namespace RedOrb;

public class DbTableDefinition : IDbTableDefinition
{
	public string SchemaName { get; init; } = string.Empty;

	public required string TableName { get; init; }

	public string Comment { get; set; } = string.Empty;

	public List<DbColumnDefinition> ColumnDefinitions { get; init; } = new();

	public IEnumerable<string> ColumnNames => ColumnDefinitions.Select(x => x.ColumnName);

	IEnumerable<DbColumnDefinition> IDbTableDefinition.ColumnDefinitions => ColumnDefinitions;

	public List<DbIndexDefinition> Indexes { get; init; } = new();

	public List<DbParentRelationDefinition> ParentRelations { get; init; } = new();

	public List<string> ChildIdentifers { get; init; } = new();

	public virtual Type? Type { get; } = null;

	public string TableFullName => GetTableFullName();

	private string GetTableFullName()
	{
		return string.IsNullOrEmpty(SchemaName) ? TableName : SchemaName + "." + TableName;
	}
}

public class DbTableDefinition<T> : DbTableDefinition
{
	public override Type Type { get; } = typeof(T);
}