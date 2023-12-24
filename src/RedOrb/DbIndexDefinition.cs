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

	public List<string> ParentIdentifers { get; set; } = new();

	public required string Identifer { get; set; }

	public bool IsNullable { get; set; } = false;

	//public List<string> AlterIdentifers { get; set; } = new();

	public List<string> ToCommandTexts()
	{
		var lst = new List<string>();
		var def = ObjectRelationMapper.FindFirst(IdentiferType);
		var pkeys = def.GetPrimaryKeys();

		foreach (var identifer in ParentIdentifers)
		{
			var key = pkeys.Where(x => x.Identifer == identifer).FirstOrDefault();
			if (key == null)
			{
				throw new InvalidProgramException($"A column that is not a primary key is specified.(type:{def.Type.FullName}, identifer:{identifer}");
			}
			var type = key.RelationColumnType;
			if (string.IsNullOrEmpty(type))
			{
				throw new InvalidProgramException($"RelationColumnType is required when used in a join expression.(type:{def.Type.FullName}, column:{key.ColumnName})");
			}

			var sql = $"{key.ColumnName} {type}";
			if (!IsNullable) { sql += " not null"; }
			lst.Add(sql);
		}

		return lst;
	}
}

