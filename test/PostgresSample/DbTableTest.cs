using RedOrb;
using Xunit.Abstractions;

namespace PostgresSample;

public class DbTableTest
{
	public DbTableTest(ITestOutputHelper output)
	{
		Logger = new UnitTestLogger() { Output = output };
	}

	private readonly UnitTestLogger Logger;

	[Fact]
	public void OmitSchemaName()
	{
		var table = new DbTable { TableName = "test" };
		Assert.Equal("test", table.TableName);
		Assert.Equal("test", table.TableFullName);
	}

	[Fact]
	public void HasSchemaName()
	{
		var table = new DbTable { SchemaName = "public", TableName = "test" };
		Assert.Equal("test", table.TableName);
		Assert.Equal("public.test", table.TableFullName);
	}
}
