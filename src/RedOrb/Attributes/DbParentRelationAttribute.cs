using System.Reflection;

namespace RedOrb.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class DbParentRelationAttribute : Attribute
{
	public bool IsPrimaryKey { get; set; } = false;

	public bool IsUniqueKey { get; set; } = false;

	public DbParentRelationDefinition ToDefinition(PropertyInfo prop)
	{
		var def = new DbParentRelationDefinition()
		{
			Identifer = prop.Name,
			IdentiferType = prop.PropertyType,
			IsNullable = prop.IsNullable(),
		};

		var columns = prop.GetCustomAttributes<DbParentRelationColumnAttribute>().ToList();
		if (columns.Count == 0) throw new InvalidProgramException();

		foreach (var column in columns)
		{
			def.Relations.Add(column.ToDefinition(prop, this));
		}

		return def;
	}

	public static DbParentRelationAttribute CreateDefault()
	{
		var attr = new DbParentRelationAttribute()
		{
			IsPrimaryKey = false,
			IsUniqueKey = false,
		};
		return attr;
	}
}
