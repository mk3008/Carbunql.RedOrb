using Carbunql;
using Carbunql.Building;
using Carbunql.Clauses;
using Carbunql.Values;
using RedOrb.Mapping;

namespace RedOrb;

internal static class SelectQueryExtension
{
	public static void AddSelectPrimarykeyColumns(this SelectQuery sq, DbTableDefinition def, TypeMap map)
	{
		var t = sq.FromClause!.GetSelectableTables().Where(x => x.Alias == map.TableAlias).First();

		var pkeys = def.GetPrimaryKeys();
		pkeys.ForEach(column =>
		{
			var name = map.TableAlias + column.Identifer;
			sq.Select(t, column.ColumnName).As(name);
			map.ColumnMaps.Add(new() { ColumnName = name, PropertyName = column.Identifer });
		});
	}

	public static void AddSelectColumnsWithoutPrimaryKeys(this SelectQuery sq, DbTableDefinition def, TypeMap map)
	{
		var t = sq.FromClause!.GetSelectableTables().Where(x => x.Alias == map.TableAlias).First();

		var pkeys = def.GetPrimaryKeys();
		def.ColumnDefinitions.Where(x => !string.IsNullOrEmpty(x.Identifer) && !pkeys.Contains(x)).ToList().ForEach(column =>
		{
			var name = map.TableAlias + column.Identifer;
			sq.Select(t, column.ColumnName).As(name);
			map.ColumnMaps.Add(new() { ColumnName = name, PropertyName = column.Identifer });
		});
	}

	public static List<TypeMap> AddJoin(this SelectQuery sq, DbParentRelationDefinition relation, TypeMap fromMap, ICascadeReadRule rule, bool doSelectPKeyOnly)
	{
		var destination = ObjectRelationMapper.FindFirst(relation.IdentiferType);
		bool isNullable = Nullable.GetUnderlyingType(relation.IdentiferType) != null;

		var fromKeys = relation.ColumnNames;
		var toKeys = destination.GetPrimaryKeys();
		var joinType = isNullable ? "left join" : "inner join";

		var index = sq.FromClause!.GetSelectableTables().Count();

		var map = new TypeMap()
		{
			TableAlias = "t" + index,
			Type = relation.IdentiferType,
			ColumnMaps = new(),
			RelationMap = new() { OwnerTableAlias = fromMap.TableAlias, OwnerPropertyName = relation.Identifer },
		};
		var maps = new List<TypeMap>() { map };

		var t = sq.FromClause!.Join(destination.SchemaName, destination.TableName, joinType).As("t" + index).On(x =>
		{
			ValueBase? condition = null;
			for (int i = 0; i < fromKeys.Count(); i++)
			{
				if (condition == null)
				{
					condition = new ColumnValue(fromMap.TableAlias, fromKeys[i]);
				}
				else
				{
					condition.And(fromMap.TableAlias, fromKeys[i]);
				}
				condition.Equal(x.Table.Alias, toKeys[i].ColumnName);
			}
			if (condition == null) throw new InvalidOperationException();
			return condition;
		});

		sq.AddSelectPrimarykeyColumns(destination, map);

		if (doSelectPKeyOnly) return maps;

		sq.AddSelectColumnsWithoutPrimaryKeys(destination, map);
		destination.ParentRelations.ForEach(relation =>
		{
			var doCascade = rule.DoCascade(destination.Type!, relation.IdentiferType);
			maps.AddRange(sq.AddJoin(relation, map, rule, doSelectPKeyOnly: !doCascade));
		});

		return maps;
	}
}
