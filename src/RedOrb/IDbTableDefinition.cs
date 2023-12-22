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

	List<DbParentRelationDefinition> ParentRelations { get; }

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
			if (sb.Length > 0) sb.AppendLine(", ");
			sb.Append("    " + column.ToCommandText());
		}

		foreach (var parent in source.ParentRelations)
		{
			if (sb.Length > 0) sb.AppendLine(", ");
			foreach (var text in parent.ToCommandTexts())
			{
				sb.Append("    " + text);
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
		var lst = source.ColumnDefinitions.Where(x => x.IsPrimaryKey && !string.IsNullOrEmpty(x.Identifer)).ToList();
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

		source.ParentRelations.ForEach(relation =>
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

		foreach (var item in source.ColumnDefinitions)
		{
			if (string.IsNullOrEmpty(item.Identifer)) continue;
			if (item.IsAutoNumber) continue;

			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);

			row.Add(pv);
			cols.Add(item.ColumnName);
		}

		foreach (var parent in source.ParentRelations)
		{
			var parentProp = parent.Identifer.ToPropertyInfo<T>();
			var parentType = parentProp.PropertyType;
			var parentInstance = parentProp.GetValue(instance);

			var def = ObjectRelationMapper.FindFirst(parent.IdentiferType);
			var pkeys = def.GetPrimaryKeys();
			for (int i = 0; i < parent.ColumnNames.Count; i++)
			{
				var p = pkeys[i].Identifer.ToPropertyInfo(parentType);
				if (parentInstance != null)
				{
					var pv = p.ToParameterValue(parentInstance, placeholderIdentifer);
					row.Add(pv);
				}
				else
				{
					var pv = p.ToParameterNullValue(placeholderIdentifer);
					row.Add(pv);
				}
				cols.Add(parent.ColumnNames[i]);
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

		foreach (var item in source.ColumnDefinitions)
		{
			if (string.IsNullOrEmpty(item.Identifer)) continue;

			var prop = item.Identifer.ToPropertyInfo<T>();
			var pv = prop.ToParameterValue(instance, placeholderIdentifer);

			row.Add(pv);
			cols.Add(item.ColumnName);
		}

		var vq = new ValuesQuery(new List<ValueCollection>() { row });
		return vq.ToSelectQuery(cols).ToUpdateQuery(source.GetTableFullName(), pkeys.Select(x => x.ColumnName));
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

