using Microsoft.Extensions.Logging;
using RedOrb;
using RedOrb.Attributes;
using Xunit.Abstractions;

namespace PostgresSample;

public class DefinitionBuilderTest
{
	public DefinitionBuilderTest(ITestOutputHelper output)
	{
		Logger = new UnitTestLogger() { Output = output };
	}

	private readonly UnitTestLogger Logger;

	[Fact]
	public void CreateTable()
	{
		var def = DefinitionBuilder.Create<Blog>();
		var sql = def.ToCreateTableCommandText();

		var expect = """
create table if not exists blogs (
    blog_id serial8 not null, 
    url text not null, 
    created_at timestamp not null default clock_timestamp(), 
	updated_at timestamp not null default clock_timestamp(), 
	version numeric not null, 
    primary key(blog_id)
)
""";

		Logger.LogInformation(sql);

		Assert.Equal(expect.ToValidateText(), sql.ToValidateText());
	}

	[Fact]
	public void CreateIndex()
	{
		var def = DefinitionBuilder.Create<Blog>();
		var sql = def.ToCreateIndexCommandTexts().First();

		var createTableCommand = """
create unique index if not exists i1_blogs on blogs (url)
""";

		Logger.LogInformation(sql);

		Assert.Equal(createTableCommand.ToValidateText(), sql.ToValidateText());
	}

	[Fact]
	public void CreateTable_HasParentRelation()
	{
		var def = DefinitionBuilder.Create<Post>();
		var sql = def.ToCreateTableCommandText();

		var expect = """
create table if not exists posts (
	post_id serial8 not null, 
	blog_id bigint not null, 
	title text not null, 
	content text not null, 
	primary key(post_id)
)
""";

		Logger.LogInformation(sql);

		Assert.Equal(expect.ToValidateText(), sql.ToValidateText());
	}
}
