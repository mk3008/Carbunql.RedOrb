using Carbunql.Definitions;

namespace RedOrb.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DbTableAttribute : Attribute, ITable
{
	public DbTableAttribute(string[] identifiers)
	{
		Identifiers = identifiers;
	}

	public string[] Identifiers { get; init; }

	public string Schema { get; init; } = string.Empty;

	public string Table { get; internal set; } = string.Empty;

	public string ConstraintName { get; init; } = string.Empty;

	public string Comment { get; init; } = string.Empty;

	public DbTableDefinition<T> ToDefinition<T>()
	{
		var d = new DbTableDefinition<T>()
		{
			SchemaName = Schema,
			TableName = (!string.IsNullOrEmpty(Table)) ? Table : typeof(T).Name.ToSnakeCase(),
			Comment = Comment,
		};
		return d;
	}

	public static DbTableAttribute CreateDefault<T>()
	{
		var table = typeof(T).Name.ToSnakeCase();
		var identifiers = new[] { table + "id" };
		var attr = new DbTableAttribute(identifiers) { Table = table };
		return attr;
	}
}