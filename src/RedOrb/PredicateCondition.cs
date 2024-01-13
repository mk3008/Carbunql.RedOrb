namespace RedOrb;

public class PredicateCondition
{
	public PredicateCondition? Next { get; set; }

	public required string VariableName { get; set; }

	public required Type ObjectType { get; set; }

	public required string PropertyName { get; set; }

	public required Type PropertyType { get; set; }

	public object? Value { get; set; }

	public void Add(PredicateCondition condition)
	{
		if (Next == null)
		{
			Next = condition;
		}
		else
		{
			Next.Add(condition);
		}
	}

	public void SetValue(object instance)
	{
		ObjectType.GetProperty(PropertyName)!.SetValue(instance, Value);
		if (Next != null)
		{
			Next.SetValue(instance);
		}
	}
}
