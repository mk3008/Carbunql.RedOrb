using Cysharp.Text;

namespace RedOrb;

public class DbIndexDefinition
{
	public bool IsUnique { get; set; } = false;

	public List<string> Identifers { get; set; } = new();

	public string ToCommandText(IDbTableDefinition table, int id)
	{
		var indexname = $"i{id}_{table.TableName}";
		var indextype = IsUnique ? "unique index" : "index";
		var sb = ZString.CreateStringBuilder();

		Identifers.Select(x => table.GetColumnName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList().ForEach(x =>
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

public class DbParentRelationDefinition
{
	public required Type IdentiferType { get; set; }

	public List<string> ColumnNames { get; set; } = new();

	public required string Identifer { get; set; }

	public bool IsNullable { get; set; } = false;

	//public List<string> AlterIdentifers { get; set; } = new();

	public List<string> ToCommandTexts()
	{
		var lst = new List<string>();
		var def = ObjectRelationMapper.FindFirst(IdentiferType);
		var pkeys = def.GetPrimaryKeys();

		for (int i = 0; i < ColumnNames.Count; i++)
		{
			var name = ColumnNames[i];
			var type = pkeys[i].RelationColumnType;
			var sql = $"{name} {type}";
			if (!IsNullable) { sql += " not null"; }
			lst.Add(sql);
		}
		return lst;
	}
}

