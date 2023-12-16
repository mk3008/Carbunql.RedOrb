namespace RedOrb;

public class DbTable : IDbTable
{
	public string SchemaName { get; set; } = string.Empty;

	public required string TableName { get; set; }

	public List<string> ColumnNames { get; init; } = new();

	public string TableFullName => GetTableFullName();

	IEnumerable<string> IDbTable.ColumnNames => ColumnNames;

	private string GetTableFullName()
	{
		return string.IsNullOrEmpty(SchemaName) ? TableName : SchemaName + "." + TableName;
	}
}