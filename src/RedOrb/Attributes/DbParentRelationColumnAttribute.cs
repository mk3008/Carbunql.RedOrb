using System.Reflection;

namespace RedOrb.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class DbParentRelationColumnAttribute : Attribute
{
	public DbParentRelationColumnAttribute(string columnType, string parentIdentifer)
	{
		ColumnType = columnType;
		ParentIdentifer = parentIdentifer;
	}

	public DbParentRelationColumnAttribute(string columnName, string columnType, string parentIdentifer)
	{
		ColumnName = columnName;
		ColumnType = columnType;
		ParentIdentifer = parentIdentifer;
	}

	public string ColumnName { get; set; } = string.Empty;

	public string ParentIdentifer { get; set; }

	public string ColumnType { get; set; }

	public string Comment { get; set; } = string.Empty;

	public DbParentRelationColumnDefinition ToDefinition(PropertyInfo prop, DbParentRelationAttribute relation)
	{
		var d = new DbParentRelationColumnDefinition()
		{
			Identifer = string.Empty,
			ColumnName = !string.IsNullOrEmpty(ColumnName) ? ColumnName : prop.Name.ToSnakeCase() + "_id",
			ColumnType = ColumnType,
			ParentIdentifer = ParentIdentifer,
			Comment = Comment,
			IsPrimaryKey = relation.IsPrimaryKey,
			IsUniqueKey = relation.IsUniqueKey,
			IsNullable = prop.IsNullable(),
			DefaultValue = string.Empty,
			IsAutoNumber = false,
			RelationColumnType = ColumnType,
			SpecialColumn = SpecialColumn.ParentRelation
		};
		return d;
	}
}
