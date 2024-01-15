using Carbunql;
using Carbunql.Building;
using Carbunql.Extensions;
using Carbunql.Tables;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using RedOrb.Mapping;
using System.Collections;
using System.Data;
using System.Linq.Expressions;

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
			Logger = (connection is ILogger lg) ? lg : null
		};

		executor.Execute(tabledef.ToCreateTableCommandText());
		foreach (var item in tabledef.ToCreateIndexCommandTexts()) executor.Execute(item);
	}

	[Obsolete("Functions that retrieve multiple rows will be deprecated in favor of single-row operations.")]
	public static List<T> Load<T>(this IDbConnection connection, Action<SelectQuery>? injector = null, ICascadeReadRule? rule = null)
	{
		return connection.Select<T>(injector, rule);
	}

	public static List<T> Select<T>(this IDbConnection connection, Action<SelectQuery>? injector = null, ICascadeReadRule? rule = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		var val = def.ToSelectQueryMap<T>(rule);
		var sq = val.Query;
		var typeMaps = val.Maps;

		if (injector != null) injector(sq);

		var executor = new QueryExecutor()
		{
			Connection = connection,
			Logger = (connection is ILogger lg) ? lg : null,
			Timeout = ObjectRelationMapper.Timeout
		};

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
		if (id.IsEmptyId())
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
		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetRemovedChildren(instance, idnetifer);
			var childrendef = ObjectRelationMapper.FindFirst(children.GenericType);
			foreach (var child in children.Items)
			{
				var deleteMethod = typeof(IDbConnectionExtension).GetMethod(nameof(DeleteByDefinition))!.MakeGenericMethod(children.GenericType);
				deleteMethod.Invoke(null, new[] { connection, child, childrendef });
			}
		}
	}

	public static void InsertByDefinition<T>(this IDbConnection connection, T instance, IDbTableDefinition def)
	{
		var iq = def.ToInsertQuery(instance, ObjectRelationMapper.PlaceholderIdentifer);

		var executor = new QueryExecutor()
		{
			Connection = connection,
			Logger = (connection is ILogger lg) ? lg : null,
			Timeout = ObjectRelationMapper.Timeout
		};

		if (iq.Sequence == null)
		{
			executor.Execute(iq.Query);
			return;
		}

		var newId = executor.ExecuteScalar<long>(iq.Query);
		var prop = iq.Sequence.Identifer.ToPropertyInfo<T>();

		prop.Write(instance, newId);

		//initialize version
		foreach (var item in def.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
		{
			var p = item.Identifer.ToPropertyInfo<T>();
			p.Write(instance, 1);
		}
	}

	public static void UpdateByDefinition<T>(this IDbConnection connection, T instance, IDbTableDefinition def)
	{
		var q = def.ToUpdateQuery(instance, ObjectRelationMapper.PlaceholderIdentifer);
		var keys = def.GetPrimaryKeys();

		if (!keys.Any())
		{
			throw new InvalidProgramException($"primary key not defined.(type:{def.Type.FullName})");
		}

		var executor = new QueryExecutor()
		{
			Connection = connection,
			Logger = (connection is ILogger lg) ? lg : null,
			Timeout = ObjectRelationMapper.Timeout
		};
		var val = executor.Execute(q, instance);
		if (val == 0)
		{
			var sb = ZString.CreateStringBuilder();
			foreach (var key in keys)
			{
				if (sb.Length > 0) sb.Append(" and ");

				var prop = key.Identifer.ToPropertyInfo<T>();
				var v = prop.GetValue(instance, null);
				sb.Append(prop.Name);
				sb.Append("=");
				sb.Append(v);
			}

			throw new InvalidOperationException($"There is no update target. The primary key value is incorrect or there is an update conflict.(type:{def.Type.FullName}, key:{sb})");
		}
		else if (val != 1)
		{
			throw new InvalidProgramException($"You are trying to update multiple items. The primary key definition is incorrect.(type:{def.Type.FullName})");
		}

		//increment version
		foreach (var item in def.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
		{
			var p = item.Identifer.ToPropertyInfo<T>();
			var v = p.GetValue(instance, null);
			var version = long.Parse(v!.ToString()!) + 1;
			p.Write(instance, version);
		}
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
		var keys = def.GetPrimaryKeys().First();
		var id = keys.Identifer.ToPropertyInfo<T>().GetValue(instance);
		if (id.IsEmptyId()) return;

		var q = def.ToDeleteQuery(instance, ObjectRelationMapper.PlaceholderIdentifer);

		var executor = new QueryExecutor()
		{
			Connection = connection,
			Logger = (connection is ILogger lg) ? lg : null,
			Timeout = ObjectRelationMapper.Timeout
		};
		executor.Execute(q);

		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetChildren(instance, idnetifer);
			var childrendef = ObjectRelationMapper.FindFirst(children.GenericType);
			foreach (var child in children.Items)
			{
				var deleteMethod = typeof(IDbConnectionExtension).GetMethod(nameof(DeleteByDefinition))!.MakeGenericMethod(children.GenericType);
				deleteMethod.Invoke(null, new[] { connection, child, childrendef });
			}
		}
		foreach (var idnetifer in def.ChildIdentifers)
		{
			var children = GetRemovedChildren(instance, idnetifer);
			var childrendef = ObjectRelationMapper.FindFirst(children.GenericType);
			foreach (var child in children.Items)
			{
				var deleteMethod = typeof(IDbConnectionExtension).GetMethod(nameof(DeleteByDefinition))!.MakeGenericMethod(children.GenericType);
				deleteMethod.Invoke(null, new[] { connection, child, childrendef });
			}
		}
	}

	public static T Load<T>(this IDbConnection connection, Expression<Func<T, bool>> predicate, ICascadeReadRule? rule = null)
	{
		var c = predicate.ToCondition();
		var instance = Activator.CreateInstance<T>();
		c.SetValue(instance!);

		return ReLoad(connection, instance, rule);
	}

	public static T ReLoad<T>(this IDbConnection connection, T instance, ICascadeReadRule? rule = null)
	{
		var pkeymaps = GetPrimaryKeyValueMaps(instance);

		if (!pkeymaps.Where(x => x.IsEmpty()).Any())
		{
			return connection.Fetch<T>(pkeymaps, rule);
		}

		var uqmaps = GetUniqueKeyValueMaps(instance);

		if (!uqmaps.Where(x => x.Value == null).Any())
		{
			return connection.Fetch<T>(uqmaps, rule);
		}

		throw new NullReferenceException("No conditions found.");
	}

	[Obsolete("Changed function name. Please use the Reload method.")]
	public static T Load<T>(this IDbConnection connection, T instance, ICascadeReadRule? rule = null)
	{
		return ReLoad(connection, instance, rule);
	}

	[Obsolete("Changed function name. Please use the Reload method.")]
	public static T LoadByKey<T>(this IDbConnection connection, T instance, ICascadeReadRule? rule = null)
	{
		return ReLoad(connection, instance, rule);
	}

	[Obsolete("Changed function name. Please use the Load method.")]
	public static T LoadByKey<T>(this IDbConnection connection, Expression<Func<T, bool>> predicate, ICascadeReadRule? rule = null)
	{
		var c = predicate.ToCondition();
		var instance = Activator.CreateInstance<T>();
		c.SetValue(instance!);

		return LoadByKey(connection, instance, rule);
	}

	private static T Fetch<T>(this IDbConnection connection, List<ValueMap> condition, ICascadeReadRule? rule = null)
	{
		var injectorOfCondition = (SelectQuery x) =>
		{
			var t = x.FromClause!.Root;
			foreach (var item in condition)
			{
				var index = condition.IndexOf(item);
				x.Where(t, item.ColumnName).Equal(x.AddParameter($"{ObjectRelationMapper.PlaceholderIdentifer}key{index}", item.Value));
			}
		};

		var val = connection.Select<T>(injectorOfCondition, rule).FirstOrDefault();

		if (val == null)
		{
			var sb = ZString.CreateStringBuilder();
			var isFirst = true;
			foreach (var item in condition)
			{
				if (!isFirst) sb.Append(", ");
				sb.Append(item.Identifer + "=" + item.Value!.ToString());
				isFirst = false;
			}
			throw new ArgumentException($"No records found.({sb})");
		}

		return val;
	}

	public static void Fetch<T>(this IDbConnection connection, T instance, string collectionProperty)
	{
		var keyvalues = GetPrimaryKeyValues(instance);

		var children = GetChildren(instance, collectionProperty);
		var relation = GetParentRelation<T>(children);
		var parent = ObjectRelationMapper.FindFirst<T>();

		var rule = new CascadeReadRule();
		rule.CascadeRelationRules.Add(new() { FromType = children.GenericType, ToType = typeof(T) });
		rule.IsNegative = true;

		var injector = (SelectQuery x) =>
		{
			x.AddComment($"inject pkey filter(type:{typeof(T).Name})");
			var def = ObjectRelationMapper.FindFirst<T>();
			var table = x.GetSelectableTables().Where(y => y.Table is PhysicalTable pt && pt.Table.IsEqualNoCase(def.TableFullName)).First();
			var i = 0;
			foreach (var item in keyvalues)
			{
				var parameter = x.AddParameter($"{ObjectRelationMapper.PlaceholderIdentifer}key{i}", item.Value);
				x.Where(table, item.Key.ColumnName).Equal(parameter);
				i++;
			}
		};

		var method = typeof(IDbConnectionExtension).GetMethods().Where(x => x.Name == nameof(Load)).First();
		var loadMethod = method.MakeGenericMethod(children.GenericType);
		var items = (IEnumerable?)loadMethod.Invoke(null, new object[] { connection, injector, rule });
		if (items == null) throw new NullReferenceException("Load method return value is NULL");
		foreach (var item in items)
		{
			children.Items.Add(item);
		}
	}

	private static Dictionary<DbColumnDefinition, object?> GetPrimaryKeyValues<T>(T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		return def.GetPrimaryKeys().ToDictionary(x => x, x => x.Identifer.ToPropertyInfo<T>().GetValue(instance));
	}

	private static List<ValueMap> GetPrimaryKeyValueMaps<T>(T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		var maps = def.GetPrimaryKeys().Select(x => new ValueMap() { Identifer = x.Identifer, ColumnName = x.ColumnName, Value = x.Identifer.ToPropertyInfo<T>().GetValue(instance) }).ToList();
		if (!maps.Any()) throw new NullReferenceException("Could not find primary key definition.");
		return maps;
	}

	private static List<ValueMap> GetUniqueKeyValueMaps<T>(T instance)
	{
		var def = ObjectRelationMapper.FindFirst<T>();

		var indexes = def.GetUniqueKeyIndexes();
		if (!indexes.Any()) throw new NullReferenceException("Could not find unique index definition.");
		if (indexes.Count != 1) throw new NullReferenceException("More than one unique index defined.");

		var columns = def.ColumnDefinitions.Where(x => indexes.First().Identifers.Contains(x.Identifer)).ToList();
		var maps = columns.Select(x => new ValueMap() { Identifer = x.Identifer, ColumnName = x.ColumnName, Value = x.Identifer.ToPropertyInfo<T>().GetValue(instance) }).ToList();
		if (!maps.Any()) throw new NullReferenceException("Could not find unique key definition.");
		return maps;
	}

	private static DbParentRelationDefinition GetParentRelation<ParentT>(Children children)
	{
		var def = ObjectRelationMapper.FindFirst(children.GenericType);
		return def.ParentRelationDefinitions.Where(x => x.IdentiferType == typeof(ParentT)).First();
	}

	private static Children GetChildren<T>(T instance, string idnetifer)
	{
		var prop = idnetifer.ToPropertyInfo<T>();
		var collectionType = prop.PropertyType;

		if (!collectionType.IsGenericType) throw new NotSupportedException();

		Type genericType = collectionType.GenericTypeArguments[0];

		var children = (IList)prop.GetValue(instance)!;

		var targetType = typeof(IDirtyCheckableCollection<>).MakeGenericType(collectionType);
		if (targetType.IsAssignableFrom(children.GetType()))
		{

		}

		return new Children() { GenericType = genericType, Items = children };
	}

	private static Children GetRemovedChildren<T>(T instance, string idnetifer)
	{
		var prop = idnetifer.ToPropertyInfo<T>();
		var collectionType = prop.PropertyType;

		if (!collectionType.IsGenericType) throw new NotSupportedException();

		Type genericType = collectionType.GenericTypeArguments[0];

		var children = (IDirtyCheckableCollection)prop.GetValue(instance)!;

		return new Children() { GenericType = genericType, Items = children.RemovedCollection };
	}
}
