using Carbunql.Building;
using Microsoft.Extensions.Logging;
using RedOrb;
using System.Collections.ObjectModel;
using System.Data;
using Xunit.Abstractions;

namespace PostgresSample;

public class CascadeTest : IClassFixture<PostgresDB>
{
	public CascadeTest(PostgresDB postgresDB, ITestOutputHelper output)
	{
		PostgresDB = postgresDB;
		Logger = new UnitTestLogger() { Output = output };
		ObjectRelationMapper.Logger = Logger;

		using var cn = PostgresDB.ConnectionOpenAsNew();

		cn.CreateTableOrDefault<Blog>();
		cn.CreateTableOrDefault<Post>();
		cn.CreateTableOrDefault<Comment>();
	}

	private readonly PostgresDB PostgresDB;

	private readonly UnitTestLogger Logger;

	[Fact]
	public void CreateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
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

		trn.Commit();
	}

	[Fact]
	public void ReadTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedPost = cn.Load<Post>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newPost.Blog.BlogId!.Value));
		}).First();

		Assert.Equal(newPost.PostId, loadedPost.PostId);
		Assert.Equal(newPost.Title, loadedPost.Title);
		Assert.Equal(newPost.Content, loadedPost.Content);
		Assert.Equal(newPost.Blog.BlogId, loadedPost.Blog.BlogId);
		Assert.Equal(newPost.Blog.Url, loadedPost.Blog.Url);

		trn.Commit();
	}

	[Fact]
	public void UpdateTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		newPost.Title = "Hello RedOrb";
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedPost = cn.Load<Post>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newPost.Blog.BlogId!.Value));
		}).First();

		Assert.Equal(newPost.PostId, loadedPost.PostId);
		Assert.Equal(newPost.Title, loadedPost.Title);
		Assert.Equal(newPost.Content, loadedPost.Content);
		Assert.Equal(newPost.Blog.BlogId, loadedPost.Blog.BlogId);
		Assert.Equal(newPost.Blog.Url, loadedPost.Blog.Url);

		trn.Commit();
	}

	[Fact]
	public void DeleteTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		cn.Save(newBlog);

		cn.Delete(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedPost = cn.Load<Post>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newPost.Blog.BlogId!.Value));
		}).FirstOrDefault();

		Assert.Null(loadedPost);

		trn.Commit();
	}

	//[Fact]
	//public void FetchTest()
	//{
	//	using var cn = PostgresDB.ConnectionOpenAsNew();
	//	using var trn = cn.BeginTransaction();

	//	Logger.LogInformation("Inserting a new blog");
	//	var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
	//	var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
	//	newBlog.Posts.Add(newPost);
	//	cn.Save(newBlog);

	//	// Read
	//	Logger.LogInformation("Querying for a blog");
	//	var loadedBlog = cn.Load<Blog>(x =>
	//	{
	//		x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
	//	}).First();

	//	Assert.Empty(loadedBlog.Posts);

	//	Fetch(cn, loadedBlog, nameof(loadedBlog.Posts));

	//	trn.Commit();
	//}

	//private void Fetch<T>(IDbConnection cn, T instance, string collectionProperty)
	//{
	//	var def = ObjectRelationMapper.FindFirst<T>();
	//	var identifer = def.ChildIdentifers.Where(x => x == collectionProperty).FirstOrDefault();
	//	var 

	//	var loadedBlog = cn.Load<T>(x =>
	//	{
	//		x.Where(x.FromClause!, "blog_id").Equal(x.AddParameter(":id", newBlog.BlogId!.Value));
	//	}).First();
	//}


}
