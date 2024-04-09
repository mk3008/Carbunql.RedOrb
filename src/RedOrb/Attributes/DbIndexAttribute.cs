namespace RedOrb.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DbIndexAttribute : Attribute
{
	public DbIndexAttribute(params string[] identifiers)
	{
		Identifiers = identifiers;
	}

	public string[] Identifiers { get; init; }

	public string ConstraintName { get; init; } = string.Empty;

	public bool IsUnique { get; init; } = false;

	public DbIndexDefinition ToDefinition()
	{
		var d = new DbIndexDefinition()
		{
			Identifiers = Identifiers.ToList(),
			IsUnique = IsUnique,
			ConstraintName = ConstraintName
		};
		return d;
	}
}
