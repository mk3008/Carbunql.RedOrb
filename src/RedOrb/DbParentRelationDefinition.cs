namespace RedOrb;

public class DbParentRelationDefinition : IDbColumnContainer
{
	public required string Identifer { get; set; }

	public required Type IdentiferType { get; set; }

	public bool IsNullable { get; set; } = false;

	public List<DbParentRelationColumnDefinition> Relations { get; set; } = new();

	public IEnumerable<DbColumnDefinition> GetDbColumnDefinitions()
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
