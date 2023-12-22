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

		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);

		cn.CreateTableOrDefault<Blog>();
	}

	private readonly PostgresDB PostgresDB;

	private readonly UnitTestLogger Logger;

	[Fact]
	public void CreateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/DefaultTest/CreateTest" };

		Assert.Null(newBlog.BlogId);
		cn.Save(newBlog);
		Assert.NotNull(newBlog.BlogId);
	}

	[Fact]
	public void CreateTest_ZeroId()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { BlogId = 0, Url = "http://blogs.msdn.com/adonet/DefaultTest/CreateTest" };

		Assert.Equal(0, newBlog.BlogId);
		cn.Save(newBlog);
		Assert.NotEqual(0, newBlog.BlogId);
	}

	[Fact]
	public void CreateTest_ValueSpecifiedError()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { BlogId = 1, Url = "http://blogs.msdn.com/adonet/DefaultTest/CreateTest" };

		// update error
		Assert.NotNull(newBlog.BlogId);
		Assert.NotEqual(0, newBlog.BlogId);

		var exception = Assert.Throws<InvalidOperationException>(() =>
		{
			cn.Save(newBlog);
		});
		Assert.Equal("There is no update target. The primary key value is incorrect or there is an update conflict.(type:PostgresSample.Blog, key:BlogId=1)", exception.Message);
	}

	[Fact]
	public void ReadTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/DefaultTest/ReadTest" };
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).First();

		Assert.Equal(newBlog.BlogId, loadedBlog.BlogId);
		Assert.Equal(newBlog.Url, loadedBlog.Url);
	}

	[Fact]
	public void UpdateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/DefaultTest/UpdateTest" };
		cn.Save(newBlog);

		newBlog.Url = "https://devblogs.microsoft.com/dotnet/DefaultTest/UpdateTest";
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).First();

		Assert.Equal(newBlog.BlogId, loadedBlog.BlogId);
		Assert.Equal(newBlog.Url, loadedBlog.Url);
	}

	[Fact]
	public void DeleteTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/DefaultTest/DeleteTest" };
		cn.Save(newBlog);

		cn.Delete(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).FirstOrDefault();

		Assert.Null(loadedBlog);
	}
}
