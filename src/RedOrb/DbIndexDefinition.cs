using Cysharp.Text;

namespace RedOrb;

public class DbIndexDefinition
{
	public bool IsUnique { get; set; } = false;

	public string ConstraintName { get; set; } = string.Empty;

	public List<string> Identifiers { get; set; } = new();

	public string ToCommandText(IDbTableDefinition table, int id)
	{
		var indexname = !string.IsNullOrEmpty(ConstraintName) ? ConstraintName : $"i{id}_{table.TableName}";
		var indextype = IsUnique ? "unique index" : "index";

		var sb = ZString.CreateStringBuilder();
		Identifiers.Select(x => table.GetColumnName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList().ForEach(x =>
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}
			sb.Append(x);
		});

		var sql = @$"create {indextype} if not exists {indexname} on {table.GetTableFullName()} ({sb})";
		return sql;
	}
}
public class DbKeyDefinition
{
	public string ConstraintName { get; set; } = string.Empty;

	public List<string> Identifiers { get; set; } = new();

	public string ToCommandText(IDbTableDefinition table, int id)
	{
		var name = !string.IsNullOrEmpty(ConstraintName) ? ConstraintName : $"i{id}_{table.TableName}";

		var sb = ZString.CreateStringBuilder();
		Identifiers.Select(x => table.GetColumnName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList().ForEach(x =>
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}
			sb.Append(x);
		});

		var sql = @$"alter table {table.GetTableFullName()} add constraint {name} primary key ({sb})";
		return sql;
	}
}
