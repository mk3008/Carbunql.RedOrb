using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace RedOrb.Extensions;

public static class IDbConnectionExtension
{
	public static void CreateTableOrDefault<T>(this IDbConnection connection)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.CreateTableOrDefault(def);
	}

	public static void CreateTableOrDefault(this IDbConnection connection, IDbTableDefinition tabledef)
	{
		connection.Execute(tabledef.ToCreateTableCommandText());
		foreach (var item in tabledef.ToCreateIndexCommandTexts()) connection.Execute(item);
	}

	public static void Insert<T>(this IDbConnection connection, T instance, string placeholderIdentifer, ILogger? Logger = null, int? timeout = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.Insert(def, instance, placeholderIdentifer, Logger, timeout);
	}

	public static void Insert<T>(this IDbConnection connection, IDbTableDefinition tabledef, T instance, string placeholderIdentifer, ILogger? Logger = null, int? timeout = null)
	{
		var iq = tabledef.ToInsertQuery(instance, placeholderIdentifer);

		var executor = new QueryExecutor() { Connection = connection, Logger = Logger, Timeout = timeout };

		if (iq.Sequence == null)
		{
			executor.Execute(iq.Query);
			return;
		}

		var newId = executor.ExecuteScalar<long>(iq.Query);
		var prop = iq.Sequence.Identifer.ToPropertyInfo<T>();

		prop.Write(instance, newId);
	}

	public static void Update<T>(this IDbConnection connection, T instance, string placeholderIdentifer, ILogger? Logger = null, int? timeout = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.Update(def, instance, placeholderIdentifer, Logger, timeout);
	}

	public static void Update<T>(this IDbConnection connection, IDbTableDefinition tabledef, T instance, string placeholderIdentifer, ILogger? Logger = null, int? timeout = null)
	{
		var q = tabledef.ToUpdateQuery(instance, placeholderIdentifer);

		var executor = new QueryExecutor() { Connection = connection, Logger = Logger, Timeout = timeout };
		executor.Execute(q, instance);
	}

	public static void Delete<T>(this IDbConnection connection, T instance, string placeholderIdentifer, ILogger? Logger = null, int? timeout = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		connection.Delete(def, instance, placeholderIdentifer, Logger, timeout);
	}

	public static void Delete<T>(this IDbConnection connection, IDbTableDefinition tabledef, T instance, string placeholderIdentifer, ILogger? Logger = null, int? timeout = null)
	{
		var q = tabledef.ToDeleteQuery(instance, placeholderIdentifer);

		var executor = new QueryExecutor() { Connection = connection, Logger = Logger, Timeout = timeout };
		executor.Execute(q);
	}
}
