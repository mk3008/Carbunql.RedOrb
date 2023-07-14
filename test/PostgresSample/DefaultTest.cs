using Carbunql.Building;
using Microsoft.Extensions.Logging;
using RedOrb;
using Xunit.Abstractions;

namespace PostgresSample;

public class DefaultTest : IClassFixture<PostgresDB>
{
	public DefaultTest(PostgresDB postgresDB, ITestOutputHelper output)
	{
		PostgresDB = postgresDB;
		Logger = new UnitTestLogger() { Output = output };
		ObjectRelationMapper.Logger = Logger;

		using var cn = PostgresDB.ConnectionOpenAsNew();

		cn.CreateTableOrDefault<Blog>();
	}

	private readonly PostgresDB PostgresDB;

	private readonly UnitTestLogger Logger;

	[Fact]
	public void CreateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };

		Assert.Null(newBlog.BlogId);
		cn.Save(newBlog);
		Assert.NotNull(newBlog.BlogId);

		trn.Commit();
	}

	[Fact]
	public void ReadTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).First();

		Assert.Equal(newBlog.BlogId, loadedBlog.BlogId);
		Assert.Equal(newBlog.Url, loadedBlog.Url);

		trn.Commit();
	}

	[Fact]
	public void UpdateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		cn.Save(newBlog);

		newBlog.Url = "https://devblogs.microsoft.com/dotnet";
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).First();

		Assert.Equal(newBlog.BlogId, loadedBlog.BlogId);
		Assert.Equal(newBlog.Url, loadedBlog.Url);

		trn.Commit();
	}

	[Fact]
	public void DeleteTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		cn.Save(newBlog);

		cn.Delete(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).FirstOrDefault();

		Assert.Null(loadedBlog);

		trn.Commit();
	}
}
