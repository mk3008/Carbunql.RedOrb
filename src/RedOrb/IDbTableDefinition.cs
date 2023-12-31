using Carbunql;
using Carbunql.Analysis.Parser;
using Carbunql.Building;
using Carbunql.Values;
using Cysharp.Text;
using RedOrb;
using RedOrb.Mapping;

namespace RedOrb;

public interface IDbTableDefinition : IDbTable
{
	IEnumerable<DbColumnDefinition> ColumnDefinitions { get; }

	List<DbIndexDefinition> Indexes { get; }

	Type Type { get; }

	IEnumerable<DbParentRelationDefinition> ParentRelationDefinitions { get; }

	List<string> ChildIdentifers { get; }
}

public static class IDbTableDefinitionExtention
{
	public static string ToCreateTableCommandText(this IDbTableDefinition source)
	{
		var table = ValueParser.Parse(source.GetTableFullName()).ToText();

		var sb = ZString.CreateStringBuilder();

		foreach (var column in source.ColumnDefinitions)
		{
			foreach (var item in column.GetCreateTableCommandTexts())
			{
				if (sb.Length > 0) sb.AppendLine(", ");
				sb.Append("    " + item);
			}
		}

		var pkeys = source.ColumnDefinitions.Where(x => x.IsPrimaryKey).ToList();
		if (pkeys.Any())
		{
			var columnText = string.Join(", ", pkeys.Select(x => ValueParser.Parse(x.ColumnName).ToText()));
			if (sb.Length > 0) sb.AppendLine(", ");
			sb.Append("    primary key(" + string.Join(", ", columnText) + ")");
		}

		var ukeys = source.ColumnDefinitions.Where(x => x.IsUniqueKey).ToList();
		if (ukeys.Any())
		{
			var columnText = string.Join(", ", ukeys.Select(x => ValueParser.Parse(x.ColumnName).ToText()));
			if (sb.Length > 0) sb.AppendLine(", ");
			sb.Append("    unique(" + string.Join(", ", columnText) + ")");
		}

		var sql = @$"create table if not exists {table} (
{sb}
)";
		return sql;
	}

	public static IEnumerable<string> ToCreateIndexCommandTexts(this IDbTableDefinition source)
	{
		var id = 0;
		foreach (var index in source.Indexes)
		{
			id++;
			yield return index.ToCommandText(source, id);
		}
	}

	public static string GetColumnName(this IDbTableDefinition source, string identifer)
	{
		return source.ColumnDefinitions.Where(x => x.Identifer == identifer).Select(x => x.ColumnName).First();
	}

	public static DbColumnDefinition? GetSequenceOrDefault(this IDbTableDefinition source)
	{
		return source.ColumnDefinitions.Where(x => x.IsAutoNumber).FirstOrDefault();
	}

	public static DbColumnDefinition GetSequence(this IDbTableDefinition source)
	{
		var seq = source.GetSequenceOrDefault();
		if (seq == null) throw new NotSupportedException($"Sequence column not defined in {source.GetTableFullName()}");
		return seq;
	}

	public static List<DbColumnDefinition> GetPrimaryKeys(this IDbTableDefinition source)
	{
		var lst = source.ColumnDefinitions.Where(x => x.IsPrimaryKey).ToList();
		if (!lst.Any()) throw new NotSupportedException($"Primary key column not defined in {source.GetTableFullName()}");
		return lst;
	}

	public static List<DbIndexDefinition> GetUniqueKeyIndexes(this IDbTableDefinition source)
	{
		var lst = source.Indexes.Where(x => x.IsUnique).ToList();
		if (!lst.Any()) throw new NotSupportedException($"Unique key column not defined in {source.GetTableFullName()}");
		return lst;
	}

	private static (SelectQuery, TypeMap) CreateSelectQuery<T>(this DbTableDefinition def)
	{
		var map = new TypeMap()
		{
			TableAlias = "t0",
			Type = typeof(T),
			ColumnMaps = new()
		};

		var sq = new SelectQuery();
		var table = ValueParser.Parse(def.GetTableFullName()).ToText();
		sq.From(table).As(map.TableAlias);

		sq.AddSelectPrimarykeyColumns(def, map);
		sq.AddSelectColumnsWithoutPrimaryKeys(def, map);

		return (sq, map);
	}

	public static (SelectQuery Query, List<TypeMap> Maps) ToSelectQueryMap<T>(this DbTableDefinition source, ICascadeReadRule? rule = null)
	{
		var (sq, fromMap) = source.CreateSelectQuery<T>();
		var maps = new List<TypeMap>() { fromMap };
		var from = sq.FromClause!.Root;

		rule ??= new FullCascadeReadRule();

		source.ParentRelationDefinitions.ToList().ForEach(relation =>
		{
			var doCascade = rule.DoCascade(source.Type!, relation.IdentiferType);
			maps.AddRange(sq.AddJoin(relation, fromMap, rule, doSelectPKeyOnly: !doCascade));
		});

		return (sq, maps);
	}

	public static (InsertQuery Query, DbColumnDefinition? Sequence) ToInsertQuery<T>(this IDbTableDefinition source, T instance, string placeholderIdentifer)
	{
		var row = new ValueCollection();
		var cols = new List<string>();

		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.None))
		{
			if (string.IsNullOrEmpty(item.Identifer)) continue;
			if (item.IsAutoNumber) continue;

			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);

			row.Add(pv);
			cols.Add(item.ColumnName);
		}

		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.CreateTimestamp || x.SpecialColumn == SpecialColumn.UpdateTimestamp))
		{
			if (string.IsNullOrEmpty(item.Identifer)) continue;
			if (item.IsAutoNumber) continue;

			var command = !string.IsNullOrEmpty(item.DefaultValue) ? item.DefaultValue : "current_timestamp";

			row.Add(command);
			cols.Add(item.ColumnName);
		}

		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
		{
			row.Add("1");
			cols.Add(item.ColumnName);
		}

		foreach (var parent in source.ParentRelationDefinitions)
		{
			var parentProp = parent.Identifer.ToPropertyInfo<T>();
			var parentType = parentProp.PropertyType;
			var parentInstance = parentProp.GetValue(instance);

			var def = ObjectRelationMapper.FindFirst(parent.IdentiferType);
			var pkeys = def.GetPrimaryKeys();

			foreach (var relation in parent.Relations)
			{
				var prop = relation.ParentIdentifer.ToPropertyInfo(parentType);
				if (parentInstance != null)
				{
					var pv = prop.ToParameterValue(parentInstance, placeholderIdentifer);
					row.Add(pv);
				}
				else
				{
					var pv = prop.ToParameterNullValue(placeholderIdentifer);
					row.Add(pv);
				}
				cols.Add(relation.ColumnName);
			}
		}

		var vq = new ValuesQuery(new List<ValueCollection>() { row });
		var query = vq.ToSelectQuery(cols).ToInsertQuery(source.GetTableFullName());

		var seq = source.GetSequenceOrDefault();
		if (seq != null) query.Returning(new ColumnValue(seq.ColumnName));

		return (query, seq);
	}

	public static UpdateQuery ToUpdateQuery<T>(this IDbTableDefinition source, T instance, string placeholderIdentifer)
	{
		var pkeys = source.GetPrimaryKeys();

		var row = new ValueCollection();
		var cols = new List<string>();

		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.None || x.SpecialColumn == SpecialColumn.ParentRelation))
		{
			if (string.IsNullOrEmpty(item.Identifer)) continue;

			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);

			row.Add(pv);
			cols.Add(item.ColumnName);
		}

		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.UpdateTimestamp))
		{
			var command = !string.IsNullOrEmpty(item.DefaultValue) ? item.DefaultValue : "current_timestamp";

			row.Add(command);
			cols.Add(item.ColumnName);
		}

		// version increment
		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
		{
			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);
			pv.AddOperatableValue("+", "1");
			row.Add(pv);
			cols.Add(item.ColumnName);
		}

		var vq = new ValuesQuery(new List<ValueCollection>() { row });
		var uq = vq.ToSelectQuery(cols).ToUpdateQuery(source.GetTableFullName(), pkeys.Select(x => x.ColumnName));

		// version check
		foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
		{
			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);
			uq.WhereClause!.Condition.And(uq.UpdateClause!.Table.Alias, item.ColumnName).Equal(pv);
		}

		return uq;
	}

	public static DeleteQuery ToDeleteQuery<T>(this IDbTableDefinition source, T instance, string placeholderIdentifer)
	{
		var pkeys = source.GetPrimaryKeys();

		var row = new ValueCollection();
		var cols = new List<string>();

		foreach (var item in pkeys)
		{
			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);

			row.Add(pv);
			cols.Add(item.ColumnName);
		}

		var vq = new ValuesQuery(new List<ValueCollection>() { row });
		return vq.ToSelectQuery(cols).ToDeleteQuery(source.GetTableFullName());
	}
}

