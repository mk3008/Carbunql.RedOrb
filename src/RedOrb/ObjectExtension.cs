namespace RedOrb;

internal static class ObjectExtension
{
	public static bool IsEmptyId(this object? id)
	{
		if (id is null)
		{
			return true;
		}
		else if (int.TryParse(id.ToString(), out var intid) && intid == 0)
		{
			return true;
		}
		else if (long.TryParse(id.ToString(), out var longid) && longid == 0)
		{
			return true;
		}
		return false;
	}
}
