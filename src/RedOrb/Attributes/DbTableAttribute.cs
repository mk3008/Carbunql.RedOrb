namespace RedOrb.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DbTableAttribute : Attribute
{
	public DbTableAttribute() { }

	public DbTableAttribute(string tableName)
	{
		TableName = tableName;
	}

	public string SchemaName { get; set; } = string.Empty;

	public string TableName { get; set; } = string.Empty;

	public string Comment { get; set; } = string.Empty;

	public DbTableDefinition<T> ToDefinition<T>()
	{
		var d = new DbTableDefinition<T>()
		{
			SchemaName = SchemaName,
			TableName = (!string.IsNullOrEmpty(TableName)) ? TableName : typeof(T).Name.ToSnakeCase(),
			Comment = Comment,
		};
		return d;
	}

	public static DbTableAttribute CreateDefault<T>()
	{
		var d = new DbTableAttribute()
		{
			TableName = typeof(T).Name.ToSnakeCase(),
		};
		return d;
	}
}