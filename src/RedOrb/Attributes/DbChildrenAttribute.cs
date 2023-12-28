namespace RedOrb;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class DbChildrenAttribute : Attribute
{
    public DbChildrenAttribute(string deletedItemsIdentifer)
	{
		RemovedItemsIdentifer = deletedItemsIdentifer;
	}

	public string RemovedItemsIdentifer { get; set; }
}
