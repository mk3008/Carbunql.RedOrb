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

		Assert.Equal(0, newBlog.BlogId);
		Assert.Equal(0, newPost.Blog.BlogId);
		Assert.Equal(0, newPost.PostId);
		cn.Save(newBlog);
		Assert.NotEqual(0, newBlog.BlogId);
		Assert.NotEqual(0, newPost.Blog.BlogId);
		Assert.NotEqual(0, newPost.PostId);
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
			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newPost.Blog.BlogId));
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

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newPost.Blog.BlogId));
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

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newPost.Blog.BlogId));
		}).FirstOrDefault();

		Assert.Null(loadedPost);
	}

	[Fact]
	public void RemoveTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/DeleteTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		//reload
		var blog = cn.Load(newBlog);
		cn.Fetch(blog, nameof(Blog.Posts));

		Assert.NotEqual(blog, newBlog);
		Assert.Single(blog.Posts);

		//remove and save
		blog.Posts.RemoveAt(0);// .Clear();
		cn.Save(blog);

		//reload
		blog = cn.Load(blog);
		cn.Fetch(blog, nameof(Blog.Posts));

		Assert.Empty(blog.Posts);
	}

	[Fact]
	public void ClearTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeTest/DeleteTest" };
		newBlog.Posts.Add(new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" });
		newBlog.Posts.Add(new Post { Title = "Hello Carbunql!", Content = "I wrote an app using RedOrb!" });
		cn.Save(newBlog);

		//reload
		var blog = cn.Load(newBlog);
		cn.Fetch(blog, nameof(Blog.Posts));

		Assert.NotEqual(blog, newBlog);
		Assert.Equal(2, blog.Posts.Count);

		//clear and save
		blog.Posts.Clear();
		cn.Save(blog);

		//reload
		blog = cn.Load(blog);
		cn.Fetch(blog, nameof(Blog.Posts));

		Assert.Empty(blog.Posts);
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

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newBlog.BlogId));
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

			x.Where(x.FromClause!, column).Equal(x.AddParameter(":id", newBlog.BlogId));
		}).First();
		cn.Fetch(loadedBlog, nameof(loadedBlog.Posts));

		Assert.Equal(2, loadedBlog.Posts.Count);
		Assert.Equal(loadedBlog.Posts[0].Blog, loadedBlog.Posts[1].Blog);
	}
}
