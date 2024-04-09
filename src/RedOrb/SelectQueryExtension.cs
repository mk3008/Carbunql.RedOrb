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
		var t = sq.GetSelectableTables().Where(x => x.Alias == map.TableAlias).First();

		var pkeys = def.GetPrimaryKeys();
		pkeys.ForEach(column =>
		{
			var name = map.TableAlias + column.Identifier;
			sq.Select(t, column.ColumnName).As(name);
			map.ColumnMaps.Add(new() { ColumnName = name, PropertyName = column.Identifier });
		});
	}

	public static void AddSelectColumnsWithoutPrimaryKeys(this SelectQuery sq, DbTableDefinition def, TypeMap map)
	{
		var t = sq.GetSelectableTables().Where(x => x.Alias == map.TableAlias).First();

		var pkeys = def.GetPrimaryKeys();
		def.ColumnDefinitions.Where(x => !string.IsNullOrEmpty(x.Identifier) && !pkeys.Contains(x)).ToList().ForEach(column =>
		{
			var name = map.TableAlias + column.Identifier;
			sq.Select(t, column.ColumnName).As(name);
			map.ColumnMaps.Add(new() { ColumnName = name, PropertyName = column.Identifier });
		});
	}

	public static List<TypeMap> AddJoin(this SelectQuery sq, DbParentRelationDefinition container, TypeMap fromMap, ICascadeReadRule rule, bool doSelectPKeyOnly)
	{
		var destination = ObjectRelationMapper.FindFirst(container.IdentiferType);
		bool isNullable = Nullable.GetUnderlyingType(container.IdentiferType) != null;
		var joinType = isNullable ? "left join" : "inner join";

		var index = sq.GetSelectableTables().Count();

		var map = new TypeMap()
		{
			TableAlias = "t" + index,
			Type = container.IdentiferType,
			ColumnMaps = new(),
			RelationMap = new() { OwnerTableAlias = fromMap.TableAlias, OwnerPropertyName = container.Identifer },
		};
		var maps = new List<TypeMap>() { map };

		var t = sq.FromClause!.Join(destination.SchemaName, destination.TableName, joinType).As("t" + index).On(x =>
		{
			foreach (var item in container.Relations)
			{
				var parentColumn = destination.GetPrimaryKeys().Where(x => x.Identifier == item.ParentIdentifer).First();
				x.Condition(new ColumnValue(fromMap.TableAlias, item.ColumnName).Equal(x.Table.Alias, parentColumn.ColumnName));
			}
		});

		sq.AddSelectPrimarykeyColumns(destination, map);

		if (doSelectPKeyOnly) return maps;

		sq.AddSelectColumnsWithoutPrimaryKeys(destination, map);
		destination.ParentRelationDefinitions.ToList().ForEach(relation =>
		{
			var doCascade = rule.DoCascade(destination.Type!, relation.IdentiferType);
			maps.AddRange(sq.AddJoin(relation, map, rule, doSelectPKeyOnly: !doCascade));
		});

		return maps;
	}
}
