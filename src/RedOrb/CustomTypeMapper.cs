using Dapper;
using System.Diagnostics.CodeAnalysis;
using static Dapper.SqlMapper;

namespace RedOrb;

public static class CustomTypeMapper
{
	private static Dictionary<Type, ITypeHandler> TypeHandlers { get; set; } = new();

	public static void AddTypeHandler<T>(TypeHandler<T?> handler)
	{
		var type = typeof(T);

		if (TypeHandlers.ContainsKey(type))
		{
			throw new ArgumentException($"Type '{type.FullName}' is already registered with {nameof(CustomTypeMapper)}.");
		}

		TypeHandlers.Add(type, handler);
		SqlMapper.AddTypeHandler(handler);
	}

	public static bool TryGetValue(Type type, [MaybeNullWhen(false)] out ITypeHandler handler)
	{
		if (TypeHandlers.ContainsKey(type))
		{
			handler = TypeHandlers[type];
			return true;
		}
		handler = default;
		return false;
	}
}
