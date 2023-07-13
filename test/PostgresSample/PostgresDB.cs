using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace PostgresSample;

/*
https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/
 */

public class PostgresDB : IAsyncLifetime
{
	private readonly PostgreSqlContainer Container = new PostgreSqlBuilder().WithImage("postgres:15-alpine").Build();

	public Task InitializeAsync()
	{
		return Container.StartAsync();
	}

	public Task DisposeAsync()
	{
		return Container.DisposeAsync().AsTask();
	}

	public IDbConnection ConnectionOpenAsNew()
	{
		var cn = new NpgsqlConnection(Container.GetConnectionString());
		cn.Open();
		return cn;
	}
}
