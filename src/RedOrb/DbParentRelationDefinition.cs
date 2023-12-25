namespace RedOrb;

public class DbParentRelationDefinition : IDbColumnContainer
{
	public required string Identifer { get; set; }

	public required Type IdentiferType { get; set; }

	public bool IsNullable { get; set; } = false;

	private List<DbParentRelationColumnDefinition>? _relations;
	public List<DbParentRelationColumnDefinition> Relations
	{
		get
		{
			if (_relations != null) return _relations;
			return GetDbParentRelationColumnDefinitionAsDefault();
		}
		set
		{
			_relations = value;
		}
	}

	public IEnumerable<DbColumnDefinition> GetDbColumnDefinitions()
	{
		if (Relations.Any())
		{
			foreach (var relation in Relations)
			{
				foreach (var item in relation.GetDbColumnDefinitions())
				{
					item.SpecialColumn = SpecialColumn.ParentRelation;
					yield return item;
				}
			}
		}
		else
		{
			foreach (var item in GetDbColumnDefinitionsAsDefault()) yield return item;
		}
	}

	private List<DbParentRelationColumnDefinition> GetDbParentRelationColumnDefinitionAsDefault()
	{
		var lst = new List<DbParentRelationColumnDefinition>();
		var mapper = ObjectRelationMapper.FindFirst(IdentiferType);

		foreach (var column in mapper.GetPrimaryKeys())
		{
			var r = new DbParentRelationColumnDefinition
			{
				ParentIdentifer = column.Identifer,
				ColumnName = column.ColumnName,
				IsNullable = IsNullable,
				IsPrimaryKey = false,
				ColumnType = column.RelationColumnType,
				RelationColumnType = column.RelationColumnType,
				Comment = column.Comment,
				IsAutoNumber = false,
				IsUniqueKey = false,
				SpecialColumn = SpecialColumn.ParentRelation,
			};
			lst.Add(r);
		}

		return lst;
	}

	private List<DbColumnDefinition> GetDbColumnDefinitionsAsDefault()
	{
		var lst = new List<DbColumnDefinition>();
		var mapper = ObjectRelationMapper.FindFirst(IdentiferType);
		foreach (var key in mapper.GetPrimaryKeys())
		{
			if (string.IsNullOrEmpty(key.RelationColumnType))
			{
				throw new InvalidProgramException();
			}

			lst.Add(new DbColumnDefinition()
			{
				ColumnName = key.ColumnName,
				IsNullable = IsNullable,
				ColumnType = key.RelationColumnType,
				SpecialColumn = SpecialColumn.ParentRelation
			});
		}

		return lst;
	}

	public IEnumerable<string> GetCreateTableCommandTexts()
	{
		foreach (var def in GetDbColumnDefinitions())
		{
			foreach (var item in def.GetCreateTableCommandTexts())
			{
				yield return item;
			}
		}
	}
}
