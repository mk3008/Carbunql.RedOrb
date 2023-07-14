using Carbunql.Dapper;
using Carbunql;
using Dapper;
using RedOrb.Mapping;
using System.Collections;
using System.Data;
using Carbunql.Building;

namespace RedOrb;

public static class IDbConnectionExtension
{
	public static void CreateTableOrDefault<T>(this IDbConnection connection)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.CreateTableOrDefault(def);
	}

	public static void CreateTableOrDefault(this IDbConnection connection, IDbTableDefinition tabledef)
	{
		var executor = new QueryExecutor()
		{
			Connection = connection,
			Logger = ObjectRelationMapper.Logger
		};

		executor.Execute(tabledef.ToCreateTableCommandText());
		foreach (var item in tabledef.ToCreateIndexCommandTexts()) connection.Execute(item);
	}

	public static List<T> Load<T>(this IDbConnection connection, Action<SelectQuery>? injector = null, ICascadeReadRule? rule = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		var val = def.ToSelectQueryMap<T>(rule);
		var sq = val.Query;
		var typeMaps = val.Maps;

		if (injector != null) injector(sq);

		var executor = new QueryExecutor() { Connection = connection, Logger = ObjectRelationMapper.Logger, Timeout = ObjectRelationMapper.Timeout };

		var lst = new List<T>();
		using var r = executor.ExecuteReader(sq);

		var repository = new InstanceCacheRepository();
		while (r.Read())
		{
			var rowMapper = CreateRowMapper(typeMaps, repository);
			var root = rowMapper.Execute(r);
			if (root == null) continue;
			lst.Add((T)root);
		}

		return lst;
	}

	private static RowMapper CreateRowMapper(List<TypeMap> typeMaps, InstanceCacheRepository repository)
	{
		var lst = new RowMapper() { Repository = repository };
		foreach (var map in typeMaps)
		{
			lst.Add(new() { TypeMap = map, Item = Activator.CreateInstance(map.Type)! });
		}
		return lst;
	}

	public static void Save<T>(this IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();

		var seq = def.GetSequenceOrDefault() ?? throw new NotSupportedException("AutoNumber column not found.");
		var id = seq.Identifer.ToPropertyInfo<T>().GetValue(instance);
		if (id == null)
		{
			connection.Insert(instance);
		}
		else
		{
			connection.Update(instance);
		}
	}

	public static void Insert<T>(this IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.InsertByDefinition(instance, def);

		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetChildren(instance, idnetifer);
			foreach (var child in children.Items)
			{
				var insertMethod = typeof(IDbConnectionExtension).GetMethod(nameof(Insert))!.MakeGenericMethod(children.GenericType);
				insertMethod.Invoke(null, new[] { connection, child });
			}
		}
	}

	public static void Update<T>(this IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.UpdateByDefinition(instance, def);

		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetChildren(instance, idnetifer);
			foreach (var child in children.Items)
			{
				var saveMethod = typeof(IDbConnectionExtension).GetMethod(nameof(Save))!.MakeGenericMethod(children.GenericType);
				saveMethod.Invoke(null, new[] { connection, child });
			}
		}
	}

	public static void InsertByDefinition<T>(this IDbConnection connection, T instance, IDbTableDefinition def)
	{
		var iq = def.ToInsertQuery(instance, ObjectRelationMapper.PlaceholderIdentifer);

		var executor = new QueryExecutor() { Connection = connection, Logger = ObjectRelationMapper.Logger, Timeout = ObjectRelationMapper.Timeout };

		if (iq.Sequence == null)
		{
			executor.Execute(iq.Query);
			return;
		}

		var newId = executor.ExecuteScalar<long>(iq.Query);
		var prop = iq.Sequence.Identifer.ToPropertyInfo<T>();

		prop.Write(instance, newId);
	}

	public static void UpdateByDefinition<T>(this IDbConnection connection, T instance, IDbTableDefinition def)
	{
		var q = def.ToUpdateQuery(instance, ObjectRelationMapper.PlaceholderIdentifer);

		var executor = new QueryExecutor() { Connection = connection, Logger = ObjectRelationMapper.Logger, Timeout = ObjectRelationMapper.Timeout };
		executor.Execute(q, instance);
	}

	public static void Delete<T>(this IDbConnection connection, IEnumerable<T> instances)
	{
		var def = ObjectRelationMapper.FindFirst<T>();

		foreach (var instance in instances)
		{
			connection.DeleteByDefinition(instance, def);
		}
	}

	public static void Delete<T>(this IDbConnection connection, T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.DeleteByDefinition(instance, def);
	}

	public static void DeleteByDefinition<T>(this IDbConnection connection, T instance, IDbTableDefinition def)
	{
		var q = def.ToDeleteQuery(instance, ObjectRelationMapper.PlaceholderIdentifer);

		var executor = new QueryExecutor() { Connection = connection, Logger = ObjectRelationMapper.Logger, Timeout = ObjectRelationMapper.Timeout };
		executor.Execute(q);

		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetChildren(instance, idnetifer);
			var childdef = ObjectRelationMapper.FindFirst(children.GenericType);
			foreach (var child in children.Items)
			{
				var deleteMethod = typeof(IDbConnectionExtension).GetMethod(nameof(DeleteByDefinition))!.MakeGenericMethod(children.GenericType);
				deleteMethod.Invoke(null, new[] { connection, child, childdef });
			}
		}
	}

	public static void Fetch<T>(this IDbConnection connection, T instance, string collectionProperty)
	{
		var vals = GetPrimaryKeyValues(instance);

		var children = GetChildren(instance, collectionProperty);
		var relation = GetParentRelation<T>(children);

		var rule = new CascadeReadRule();
		rule.CascadeRelationRules.Add(new() { FromType = children.GenericType, ToType = typeof(T) });
		rule.IsNegative = true;

		var injector = (SelectQuery x) =>
		{
			for (int i = 0; i < relation.ColumnNames.Count(); i++)
			{
				x.Where(x.FromClause!, relation.ColumnNames[i]).Equal(x.AddParameter($"{ObjectRelationMapper.PlaceholderIdentifer}key{i}", vals[i]));
			}
		};

		var loadMethod = typeof(IDbConnectionExtension).GetMethod(nameof(Load))!.MakeGenericMethod(children.GenericType);
		var items = (IEnumerable?)loadMethod.Invoke(null, new object[] { connection, injector, rule });
		if (items == null) throw new NullReferenceException("Load method return value is NULL");
		foreach (var item in items)
		{
			children.Items.Add(item);
		}
	}

	private static List<object?> GetPrimaryKeyValues<T>(T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		return def.GetPrimaryKeys().Select(x => x.Identifer.ToPropertyInfo<T>().GetValue(instance)).ToList();
	}

	private static DbParentRelationDefinition GetParentRelation<ParentT>(Children children)
	{
		var def = ObjectRelationMapper.FindFirst(children.GenericType);
		return def.ParentRelations.Where(x => x.IdentiferType == typeof(ParentT)).First();
	}

	private static Children GetChildren<T>(T instance, string idnetifer)
	{
		var prop = idnetifer.ToPropertyInfo<T>();
		var collectionType = prop.PropertyType;

		if (!collectionType.IsGenericType) throw new NotSupportedException();

		Type genericType = collectionType.GenericTypeArguments[0];

		var children = (IList)prop.GetValue(instance)!;

		return new Children() { GenericType = genericType, Items = children };
	}

	private class Children
	{
		public required Type GenericType { get; set; }

		public required IList Items { get; set; }
	}
}
