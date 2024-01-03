namespace RedOrb;

public class ValueMap
{
	public required string Identifer { get; set; }
	public required string ColumnName { get; set; }
	public object? Value { get; set; }

	public bool IsEmpty()
	{
		if (Value == null) return true;
		var v = Value.ToString();
		if (string.IsNullOrEmpty(v)) return true;
		if (long.TryParse(v, out var id) && id == 0) return true;
		return false;
	}
}