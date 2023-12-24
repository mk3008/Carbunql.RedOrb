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

	[Obsolete]
	public List<string> ParentIdentifers { get; set; } = new();

	public required string Identifer { get; set; }

	public bool IsNullable { get; set; } = false;

	//public List<string> AlterIdentifers { get; set; } = new();

	public List<DbColumnDefinition> GetParentRelationColumnDefinitions()
	{
		var lst = new List<DbColumnDefinition>();
		var def = ObjectRelationMapper.FindFirst(IdentiferType);
		var keys = def.GetPrimaryKeys();

		foreach (var key in keys)
		{
			var type = key.RelationColumnType;
			if (string.IsNullOrEmpty(type))
			{
				throw new InvalidProgramException($"RelationColumnType is required when used in a join expression.(type:{def.Type.FullName}, column:{key.ColumnName})");
			}
			lst.Add(key);
		}

		return lst;
	}

	public List<string> ToCommandTexts()
	{
		var lst = new List<string>();

		foreach (var relation in GetParentRelationColumnDefinitions())
		{
			var sql = $"{relation.ColumnName} {relation.RelationColumnType}";
			if (!IsNullable) { sql += " not null"; }
			lst.Add(sql);
		}

		return lst;
	}
}

