namespace RedOrb.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DbIndexAttribute : Attribute
{
	public DbIndexAttribute(params string[] identifers)
	{
		Identifers = identifers;
	}

	public DbIndexAttribute(bool isUnique, params string[] identifers)
	{
		IsUnique = isUnique;
		Identifers = identifers;
	}

	public string[] Identifers { get; }

	public bool IsUnique { get; }

	public DbIndexDefinition ToDefinition()
	{
		var d = new DbIndexDefinition()
		{
			Identifers = Identifers.ToList(),
			IsUnique = IsUnique
		};
		return d;
	}
}
