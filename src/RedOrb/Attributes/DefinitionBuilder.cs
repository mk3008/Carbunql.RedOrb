using System.Reflection;

namespace RedOrb.Attributes;

public static class DefinitionBuilder
{
	public static DbTableDefinition<T> Create<T>() where T : class
	{
		var table = typeof(T).GetCustomAttribute<DbTableAttribute>();
		if (table == null) table = DbTableAttribute.CreateDefault<T>();

		var def = table.ToDefinition<T>();

		//add Indexes
		foreach (var item in typeof(T).GetCustomAttributes<DbIndexAttribute>())
		{
			def.Indexes.Add(item.ToDefinition());
		}

		//add ColumnContainers
		foreach (var prop in typeof(T).GetProperties())
		{
			var column = prop.GetCustomAttribute<DbColumnAttribute>();
			if (column != null)
			{
				def.ColumnContainers.Add(column.ToDefinition(prop));
				continue;
			}
			var relation = prop.GetCustomAttribute<DbParentRelationAttribute>();
			if (relation != null)
			{
				def.ColumnContainers.Add(relation.ToDefinition(prop));
				continue;
			}
		}

		//add Child
		foreach (var prop in typeof(T).GetProperties())
		{
			var children = prop.GetCustomAttribute<DbChildrenAttribute>();
			if (children == null) continue;
			def.ChildIdentifers.Add(prop.Name);
		}

		return def;
	}
}
