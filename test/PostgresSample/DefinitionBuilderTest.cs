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
		var sql = def.ToDefinitionQuerySet().ToText();

		var expect = """
CREATE TABLE IF NOT EXISTS blog (
    blog_id serial8 NOT NULL,
    url text NOT NULL,
    tags text NOT NULL,
    created_at timestamp NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    updated_at timestamp NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    version numeric NOT NULL,
    CONSTRAINT pkey_blog PRIMARY KEY (blog_id)
)
;
CREATE UNIQUE INDEX IF NOT EXISTS i0_blog ON blog (
    url
)
;
""";

		Logger.LogInformation(sql);

		Assert.Equal(expect.ToValidateText(), sql.ToValidateText());
	}

	[Fact]
	public void CreateTable_HasParentRelation()
	{
		var def = DefinitionBuilder.Create<Post>();
		var sql = def.ToDefinitionQuerySet().ToText();

		var expect = """
CREATE TABLE IF NOT EXISTS post (
    post_id serial8 NOT NULL,
    blog_id bigint NOT NULL,
    title text NOT NULL,
    content text NOT NULL,
    CONSTRAINT pk_post PRIMARY KEY (post_id)
)
;
CREATE INDEX IF NOT EXISTS r0_post ON post (
    blog_id
)
;
""";

		Logger.LogInformation(sql);

		Assert.Equal(expect.ToValidateText(), sql.ToValidateText());
	}
}
