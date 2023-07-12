using System.Reflection;

namespace RedOrb.Extensions;

internal static class StringExtension
{
	public static PropertyInfo ToPropertyInfo<T>(this string identifer)
	{
		return identifer.ToPropertyInfo(typeof(T));
	}

	public static PropertyInfo ToPropertyInfo(this string identifer, Type type)
	{
		var prop = type.GetProperty(identifer);
		if (prop == null) throw new InvalidOperationException($"Failed to get Property from Identifer. Type:{type.FullName}, Identifer:{identifer}");
		return prop;
	}
}
