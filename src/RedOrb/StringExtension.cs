using System.Reflection;
using System.Text;

namespace RedOrb;

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

	public static string ToSnakeCase(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		var sb = new StringBuilder();
		sb.Append(char.ToLower(input[0]));

		for (int i = 1; i < input.Length; i++)
		{
			if (char.IsUpper(input[i]))
			{
				sb.Append('_');
				sb.Append(char.ToLower(input[i]));
			}
			else
			{
				sb.Append(input[i]);
			}
		}

		return sb.ToString();
	}
}
