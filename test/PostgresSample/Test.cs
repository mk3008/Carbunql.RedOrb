using Carbunql.Building;
using Microsoft.Extensions.Logging;
using RedOrb;
using Xunit.Abstractions;

namespace PostgresSample;

public class Test
{
	public Test(ITestOutputHelper output)
	{
		Logger = new UnitTestLogger() { Output = output };
	}

	private readonly UnitTestLogger Logger;

	[Fact]
	public void CreateTable()
	{
		ObjectRelationMapper.PlaceholderIdentifer = ":";
		ObjectRelationMapper.Logger = Logger;

		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetBlogDefinition());
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetPostDefinition());

		using var cn = new PostgresDB().ConnectionOpenAsNew();

		cn.CreateTableOrDefault<Blog>();
		cn.CreateTableOrDefault<Post>();
	}

	[Fact]
	public void Insert()
	{
		ObjectRelationMapper.PlaceholderIdentifer = ":";
		ObjectRelationMapper.Logger = Logger;

		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetBlogDefinition());
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetPostDefinition());

		using var cn = new PostgresDB().ConnectionOpenAsNew();
		using var trn = cn.BeginTransaction();

		cn.CreateTableOrDefault<Blog>();
		cn.CreateTableOrDefault<Post>();

		// Create
		Logger.LogInformation("Inserting a new blog");
		var blog = new Blog { Url = "http://blogs.msdn.com/adonet" };
		cn.Save(blog);
		var id = blog.BlogId!.Value;

		// Read
		Logger.LogInformation("Querying for a blog");
		blog = cn.Load<Blog>(x =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(id);
		}).First();

		// Update
		Logger.LogInformation("Updating the blog and adding a post");
		blog.Url = "https://devblogs.microsoft.com/dotnet";
		blog.Posts.Add(new Post { Title = "Hello Carbunql", Content = "I wrote an app using RedOrb!" });
		blog.Posts.Add(new Post { Title = "Hello RedOrb", Content = "I wrote an app using RedOrb!" });
		cn.Save(blog);

		// Remove Update
		Logger.LogInformation("Remove part of post");
		var cache = blog.Posts.ToList();
		blog.Posts.RemoveAt(0);
		cn.Delete(cache.Where(x => !blog.Posts.Contains(x)));

		// Delete
		Logger.LogInformation("Delete the blog");
		cn.Delete(blog);

		trn.Commit();
	}
}
