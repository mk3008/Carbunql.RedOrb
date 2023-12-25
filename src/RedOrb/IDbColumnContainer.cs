namespace RedOrb;

public interface IDbColumnContainer
{
	public IEnumerable<DbColumnDefinition> GetDbColumnDefinitions();

	public IEnumerable<string> GetCreateTableCommandTexts();
}