using Carbunql.Building;
using Microsoft.Extensions.Logging;
using RedOrb;
using System.Data;
using Xunit.Abstractions;

namespace PostgresSample;

public class CascadeTest : IClassFixture<PostgresDB>
{
	public CascadeTest(PostgresDB postgresDB, ITestOutputHelper output)
	{
		PostgresDB = postgresDB;
		Logger = new UnitTestLogger() { Output = output };

		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);

		cn.CreateTableOrDefault<Blog>();
		cn.CreateTableOrDefault<Post>();
		cn.CreateTableOrDefault<Comment>();
	}

	private readonly PostgresDB PostgresDB;

	private readonly UnitTestLogger Logger;

	[Fact]
	public void CreateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/CreateTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);

		Assert.Null(newBlog.BlogId);
		Assert.Null(newPost.Blog.BlogId);
		Assert.Null(newPost.PostId);
		cn.Save(newBlog);
		Assert.NotNull(newBlog.BlogId);
		Assert.NotNull(newPost.Blog.BlogId);
		Assert.NotNull(newPost.PostId);
		Assert.Equal(newBlog.BlogId, newPost.Blog.BlogId);
	}

	[Fact]
	public void ReadTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/ReadTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedPost = cn.Load<Post>(x =>
		{
			var def = ObjectRelationMapper.FindFirst<Post>();
			var parent = def.ParentRelationDefinitions.Where(x => x.Identifer == nameof(Post.Blog)).First();
			var column = parent.Relations.Where(x => x.ParentIdentifer == nameof(Blog.BlogId)).First().ColumnName;
			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newPost.Blog.BlogId!.Value));
		}).First();

		Assert.Equal(newPost.PostId, loadedPost.PostId);
		Assert.Equal(newPost.Title, loadedPost.Title);
		Assert.Equal(newPost.Content, loadedPost.Content);
		Assert.Equal(newPost.Blog.BlogId, loadedPost.Blog.BlogId);
		Assert.Equal(newPost.Blog.Url, loadedPost.Blog.Url);
	}

	[Fact]
	public void UpdateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/UpdateTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		newPost.Title = "Hello RedOrb";
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedPost = cn.Load<Post>(x =>
		{
			var def = ObjectRelationMapper.FindFirst<Post>();
			var parent = def.ParentRelationDefinitions.Where(x => x.Identifer == nameof(Post.Blog)).First();
			var column = parent.Relations.Where(x => x.ParentIdentifer == nameof(Blog.BlogId)).First().ColumnName;

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newPost.Blog.BlogId!.Value));
		}).First();

		Assert.Equal(newPost.PostId, loadedPost.PostId);
		Assert.Equal(newPost.Title, loadedPost.Title);
		Assert.Equal(newPost.Content, loadedPost.Content);
		Assert.Equal(newPost.Blog.BlogId, loadedPost.Blog.BlogId);
		Assert.Equal(newPost.Blog.Url, loadedPost.Blog.Url);
	}

	[Fact]
	public void DeleteTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/DeleteTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		cn.Delete(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedPost = cn.Load<Post>(x =>
		{
			var def = ObjectRelationMapper.FindFirst<Post>();
			var parent = def.ParentRelationDefinitions.Where(x => x.Identifer == nameof(Post.Blog)).First();
			var column = parent.Relations.Where(x => x.ParentIdentifer == nameof(Blog.BlogId)).First().ColumnName;

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newPost.Blog.BlogId!.Value));
		}).FirstOrDefault();

		Assert.Null(loadedPost);
	}

	[Fact]
	public void FetchTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/FetchTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			var def = ObjectRelationMapper.FindFirst<Blog>();
			var column = def.ColumnDefinitions.Where(x => x.Identifer == nameof(Blog.BlogId)).First().ColumnName;

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).First();

		Assert.Empty(loadedBlog.Posts);

		cn.Fetch(loadedBlog, nameof(loadedBlog.Posts));

		Assert.Single(loadedBlog.Posts);
		var loadedPost = loadedBlog.Posts[0];

		Assert.Equal(newPost.PostId, loadedPost.PostId);
		Assert.Equal(newPost.Title, loadedPost.Title);
		Assert.Equal(newPost.Content, loadedPost.Content);
		Assert.Equal(newPost.Blog.BlogId, loadedPost.Blog.BlogId);
		Assert.Equal(newPost.Blog.Url, loadedPost.Blog.Url);
	}

	[Fact]
	public void CacheTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/CacheTest" };
		newBlog.Posts.Add(new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" });
		newBlog.Posts.Add(new Post { Title = "Hello RedOrb", Content = "I wrote an app using RedOrb!" });
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedBlog = cn.Load<Blog>(x =>
		{
			var def = ObjectRelationMapper.FindFirst<Blog>();
			var column = def.ColumnDefinitions.Where(x => x.Identifer == nameof(Blog.BlogId)).First().ColumnName;

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
		}).First();
		cn.Fetch(loadedBlog, nameof(loadedBlog.Posts));

		Assert.Equal(2, loadedBlog.Posts.Count);
		Assert.Equal(loadedBlog.Posts[0].Blog, loadedBlog.Posts[1].Blog);
	}
}
