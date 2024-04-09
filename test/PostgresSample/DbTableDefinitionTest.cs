using Microsoft.Extensions.Logging;
using RedOrb;
using RedOrb.Attributes;
using Xunit.Abstractions;

namespace PostgresSample;

public class DbTableDefinitionTest
{
	public DbTableDefinitionTest(ITestOutputHelper output)
	{
		Logger = new UnitTestLogger() { Output = output };
	}

	private readonly UnitTestLogger Logger;

	[Fact]
	public void OmitSchemaName()
	{
		var table = new DbTableDefinition { TableName = "test" };
		Assert.Equal("test", table.TableName);
		Assert.Equal("test", table.TableFullName);
	}

	[Fact]
	public void HasSchemaName()
	{
		var table = new DbTableDefinition { SchemaName = "public", TableName = "test" };
		Assert.Equal("test", table.TableName);
		Assert.Equal("public.test", table.TableFullName);
	}

	private DbTableDefinition GetDefinition()
	{
		var def = new DbTableDefinition
		{
			TableName = "test",
			PKeyIdentifiers = { "test_id" },
			ColumnContainers = new()
			{
				new DbColumnDefinition()
				{
					Identifier = "test_id",
					ColumnName = "test_id",
					ColumnType = "serial8",
				},
				new DbColumnDefinition()
				{
					Identifier = "price",
					ColumnName = "price",
					ColumnType = "int8"
				},
				new DbColumnDefinition()
				{
					Identifier = "remarks",
					ColumnName = "remarks",
					ColumnType = "text",
					IsNullable = true,
				}
			}
		};
		return def;
	}

	[Fact]
	public void DDL()
	{
		var def = GetDefinition();

		var expect = """
CREATE TABLE IF NOT EXISTS test (
    test_id serial8 NOT NULL,
    price int8 NOT NULL,
    remarks text,
    CONSTRAINT pk_test PRIMARY KEY (test_id)
)
;
""";
		var actual = def.ToDefinitionQuerySet().ToText();
		Logger.LogInformation(actual);

		Assert.Equal(expect.ToValidateText(), actual.ToValidateText());
	}



}
