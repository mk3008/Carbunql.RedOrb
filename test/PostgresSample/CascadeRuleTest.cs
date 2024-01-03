using Carbunql.Building;
using Microsoft.Extensions.Logging;
using RedOrb;
using Xunit.Abstractions;

namespace PostgresSample;

public class CascadeRuleTest : IClassFixture<PostgresDB>
{
	public CascadeRuleTest(PostgresDB postgresDB, ITestOutputHelper output)
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
	public void FullCascadeTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeRuleTest/FullCascadeTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		var newComment = new Comment { CommentText = "How are you?" };
		newPost.Comments.Add(newComment);
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedComment = cn.Load<Comment>(x =>
		{
			x.Where(x.FromClause!, "comment_id").Equal(x.AddParameter(":id", newComment.CommentId));
		}).First();

		Assert.Equal(loadedComment.CommentId, newComment.CommentId);
		Assert.Equal(loadedComment.CommentText, newComment.CommentText);
		Assert.Equal(loadedComment.Post.PostId, newComment.Post.PostId);
		Assert.Equal(loadedComment.Post.Title, newComment.Post.Title);
		Assert.Equal(loadedComment.Post.Blog.BlogId, newComment.Post.Blog.BlogId);
		Assert.Equal(loadedComment.Post.Blog.Url, newComment.Post.Blog.Url);
	}

	[Fact]
	public void NoCascadeTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeRuleTest/NoCascadeTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		var newComment = new Comment { CommentText = "How are you?" };
		newPost.Comments.Add(newComment);
		cn.Save(newBlog);

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedComment = cn.Load<Comment>(x =>
		{
			x.Where(x.FromClause!, "comment_id").Equal(x.AddParameter(":id", newComment.CommentId));
		}, rule: new NoCascadeReadRule()).First();

		Assert.Equal(loadedComment.CommentId, newComment.CommentId);
		Assert.Equal(loadedComment.CommentText, newComment.CommentText);
		Assert.Equal(loadedComment.Post.PostId, newComment.Post.PostId);

		Assert.NotEqual(loadedComment.Post.Title, newComment.Post.Title);
		Assert.Equal(loadedComment.Post.Title, string.Empty);
		Assert.Null(loadedComment.Post.Blog);
	}

	[Fact]
	public void WhiteListTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeRuleTest/WhiteListTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		var newComment = new Comment { CommentText = "How are you?" };
		newPost.Comments.Add(newComment);
		cn.Save(newBlog);

		var rule = new CascadeReadRule();
		rule.CascadeRelationRules.Add(new() { FromType = typeof(Comment), ToType = typeof(Post) });

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedComment = cn.Load<Comment>(x =>
		{
			x.Where(x.FromClause!, "comment_id").Equal(x.AddParameter(":id", newComment.CommentId));
		}, rule).First();

		Assert.Equal(loadedComment.CommentId, newComment.CommentId);
		Assert.Equal(loadedComment.CommentText, newComment.CommentText);
		Assert.Equal(loadedComment.Post.PostId, newComment.Post.PostId);
		Assert.Equal(loadedComment.Post.Title, newComment.Post.Title);
		Assert.Equal(loadedComment.Post.Blog.BlogId, newComment.Post.Blog.BlogId);

		Assert.NotEqual(loadedComment.Post.Blog.Url, newComment.Post.Blog.Url);
		Assert.Equal(loadedComment.Post.Blog.Url, string.Empty);
	}

	[Fact]
	public void BlackListTest()
	{
		using var cn = PostgresDB.ConnectionOpenAsNew(Logger);
		using var trn = cn.BeginTransaction();

		Logger.LogInformation("Inserting a new blog");
		var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet/CascadeRuleTest/BlackListTest" };
		var newPost = new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" };
		newBlog.Posts.Add(newPost);
		var newComment = new Comment { CommentText = "How are you?" };
		newPost.Comments.Add(newComment);
		cn.Save(newBlog);

		var rule = new CascadeReadRule();
		rule.CascadeRelationRules.Add(new() { FromType = typeof(Comment), ToType = typeof(Post) });
		rule.IsNegative = true;

		// Read
		Logger.LogInformation("Querying for a blog");
		var loadedComment = cn.Load<Comment>(x =>
		{
			x.Where(x.FromClause!, "comment_id").Equal(x.AddParameter(":id", newComment.CommentId));
		}, rule).First();

		Assert.Equal(loadedComment.CommentId, newComment.CommentId);
		Assert.Equal(loadedComment.CommentText, newComment.CommentText);
		Assert.Equal(loadedComment.Post.PostId, newComment.Post.PostId);

		Assert.NotEqual(loadedComment.Post.Title, newComment.Post.Title);
		Assert.Equal(loadedComment.Post.Title, string.Empty);
		Assert.Null(loadedComment.Post.Blog);
	}
}
