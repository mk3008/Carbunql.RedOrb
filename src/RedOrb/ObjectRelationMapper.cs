using System.Collections.Concurrent;

namespace RedOrb;

public static class ObjectRelationMapper
{
	public static string PlaceholderIdentifer { get; set; } = "@";

	public static int Timeout { get; set; } = 30;

	public static Func<DbTableDefinition, DbTableDefinition>? Converter { get; set; }

	private static ConcurrentDictionary<Type, DbTableDefinition> Map { get; set; } = new();

	public static void AddTypeHandler<T>(DbTableDefinition<T> def)
	{
		Type type = typeof(T);

		if (Map.ContainsKey(type))
		{
			throw new ArgumentException($"Type '{type.FullName}' is already registered with ObjectRelationMapper.");
		}

		if (Converter != null)
		{
			Map.GetOrAdd(type, Converter(def));
		}
		else
		{
			Map.GetOrAdd(type, def);
		}
	}

	public static DbTableDefinition<T> FindFirst<T>()
	{
		return (DbTableDefinition<T>)FindFirst(typeof(T));
	}

	public static DbTableDefinition FindFirst(Type type)
	{
		if (!Map.ContainsKey(type))
		{
			throw new ArgumentException(@$"Type '{type.FullName}' is not registered with {nameof(ObjectRelationMapper)}. 
Use the '{nameof(ObjectRelationMapper)}.{nameof(AddTypeHandler)}' method to register the type and its corresponding table conversion definition.");
		}
		return Map[type];
	}
}
