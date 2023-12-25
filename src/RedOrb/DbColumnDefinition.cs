using Carbunql.Analysis.Parser;

namespace RedOrb;

public class DbColumnDefinition : IDbColumnContainer
{
	public string Identifer { get; set; } = string.Empty;

	public required string ColumnName { get; set; }

	public required string ColumnType { get; set; }

	public string RelationColumnType { get; set; } = string.Empty;

	public bool IsNullable { get; set; } = false;

	public bool IsPrimaryKey { get; set; } = false;

	public bool IsUniqueKey { get; set; } = false;

	public bool IsAutoNumber { get; set; } = false;

	public string DefaultValue { get; set; } = string.Empty;

	public string Comment { get; set; } = string.Empty;

	public SpecialColumn SpecialColumn { get; set; } = SpecialColumn.None;

	public IEnumerable<DbColumnDefinition> GetDbColumnDefinitions()
	{
		yield return this;
	}

	public IEnumerable<string> GetCreateTableCommandTexts()
	{
		var name = ColumnName;
		var type = ColumnType;
		var sql = $"{name} {type}";

		if (!IsNullable) { sql += " not null"; }
		if (!string.IsNullOrEmpty(DefaultValue)) { sql += " default " + ValueParser.Parse(DefaultValue).ToText(); }

		yield return sql;
	}
}
