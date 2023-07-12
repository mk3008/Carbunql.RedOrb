using Carbunql;
using Carbunql.Dapper;
using RedOrb.Extensions;
using RedOrb.Mapping;
using System.Collections;
using System.Data;

namespace RedOrb;

public class DbAccessor
{
	public static void Save<T>(IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();

		var seq = def.GetSequenceOrDefault() ?? throw new NotSupportedException("AutoNumber column not found.");
		var id = seq.Identifer.ToPropertyInfo<T>().GetValue(instance);
		if (id == null)
		{
			Insert(connection, instance);
		}
		else
		{
			Update(connection, instance);
		}
	}

	public static void Insert<T>(IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.Insert(def, instance, ObjectRelationMapper.PlaceholderIdentifer, ObjectRelationMapper.Logger, ObjectRelationMapper.Timeout);

		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetChildren(instance, idnetifer);
			foreach (var child in children.Items)
			{
				var insertMethod = typeof(DbAccessor).GetMethod(nameof(Insert))!.MakeGenericMethod(children.GenericType);
				insertMethod.Invoke(null, new[] { connection, child });
			}
		}
	}

	public static void Update<T>(IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.Update(def, instance, ObjectRelationMapper.PlaceholderIdentifer, ObjectRelationMapper.Logger, ObjectRelationMapper.Timeout);

		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetChildren(instance, idnetifer);
			foreach (var child in children.Items)
			{
				var saveMethod = typeof(DbAccessor).GetMethod(nameof(Save))!.MakeGenericMethod(children.GenericType);
				saveMethod.Invoke(null, new[] { connection, child });
			}
		}
	}




	public static List<T> Load<T>(IDbConnection cn, Action<SelectQuery>? injector = null, ICascadeRule? rule = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		var val = def.ToSelectQueryMap<T>();
		var sq = val.Query;
		var typeMaps = val.Maps;

		if (injector != null) injector(sq);

		var lst = new List<T>();
		using var r = cn.ExecuteReader(sq);

		while (r.Read())
		{
			var mapper = CreateMapper(typeMaps);
			var root = mapper.Execute(r);
			if (root == null) continue;
			lst.Add((T)root);
		}

		return lst;
	}

	private static Mapper CreateMapper(List<TypeMap> typeMaps)
	{
		var lst = new Mapper();
		foreach (var map in typeMaps)
		{
			lst.Add(new() { TypeMap = map, Item = Activator.CreateInstance(map.Type)! });
		}
		return lst;
	}

	private static (Type GenericType, IEnumerable Items) GetChildren<T>(T instance, string idnetifer)
	{
		var prop = idnetifer.ToPropertyInfo<T>();
		var collectionType = prop.PropertyType;

		if (!collectionType.IsGenericType) throw new NotSupportedException();

		Type genericType = collectionType.GenericTypeArguments[0];

		var children = (IEnumerable)prop.GetValue(instance)!;

		return (genericType, children);
	}
}
