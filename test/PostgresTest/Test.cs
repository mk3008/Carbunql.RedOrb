using Carbunql.Building;
using Carbunql.RedOrb;
using Carbunql.RedOrb.Extensions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PostgresTest;

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
		DbAccessor.Save(cn, blog);
		var id = blog.BlogId!.Value;

		// Read
		Logger.LogInformation("Querying for a blog");
		blog = DbAccessor.Load<Blog>(cn, (x) =>
		{
			x.Where(x.FromClause!, "blog_id").Equal(id);
		}).First();

		// Update
		Logger.LogInformation("Updating the blog and adding a post");
		blog.Url = "https://devblogs.microsoft.com/dotnet";
		blog.Posts.Add(new Post { Title = "Hello World", Content = "I wrote an app using Carbunql RedOrb!" });
		blog.Posts.Add(new Post { Title = "Hello Carbunql", Content = "" });
		DbAccessor.Save(cn, blog);

		//// Remove Update
		//Logger.LogInformation("Remove part of post");
		//blog.Posts.RemoveAt(0);
		//DbAccessor.Save(cn, blog);

		//// Delete
		//Logger.LogInformation("Delete the blog");
		//DbAccessor.Delete(cn, blog);

		trn.Commit();
	}
}
