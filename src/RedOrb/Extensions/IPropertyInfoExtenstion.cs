using Carbunql.Values;
using System.Reflection;

namespace RedOrb.Extensions;

internal static class IPropertyInfoExtenstion
{
	public static ParameterValue ToParameterValue<T>(this PropertyInfo prop, T instance, string placeholderIdentifer)
	{
		var value = prop.GetValue(instance);
		var key = placeholderIdentifer + prop.Name;
		return new ParameterValue(key, value);
	}

	public static ParameterValue ToParameterNullValue(this PropertyInfo prop, string placeholderIdentifer)
	{
		var key = placeholderIdentifer + prop.Name;
		return new ParameterValue(key, null);
	}
}
