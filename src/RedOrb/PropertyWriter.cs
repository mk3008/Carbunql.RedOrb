using System.Reflection;

namespace RedOrb;

public static class PropertyInfoExtension
{
	public static void Write(this PropertyInfo prop, object? instance, object value)
	{
		if (instance == null) return;

		var propType = prop.PropertyType;
		if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			propType = Nullable.GetUnderlyingType(propType);
		}

		if (propType == null) throw new ArgumentNullException(nameof(propType));

		try
		{
			object convertedValue = Convert.ChangeType(value, propType);
			prop.SetValue(instance, convertedValue);
		}
		catch (InvalidCastException)
		{
			throw new NotSupportedException($"Unsupported property type: {prop.PropertyType}");
		}
		catch (FormatException)
		{
			throw new NotSupportedException($"Invalid value for property type: {prop.PropertyType}");
		}
		catch (OverflowException)
		{
			throw new NotSupportedException($"Value exceeds the range of property type: {prop.PropertyType}");
		}
	}
}
