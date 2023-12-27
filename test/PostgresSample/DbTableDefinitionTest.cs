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
			ColumnContainers = new()
			{
				new DbColumnDefinition()
				{
					ColumnName = "test_id",
					IsPrimaryKey = true,
					ColumnType = "serial8",
				},
				new DbColumnDefinition()
				{
					Identifer = "price",
					ColumnName = "price",
					ColumnType = "int8"
				},
				new DbColumnDefinition()
				{
					Identifer = "remarks",
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
create table if not exists test (
    test_id serial8 not null, 
    price int8 not null, 
    remarks text, 
    primary key(test_id)
)
""";
		var actual = def.ToCreateTableCommandText();
		Logger.LogInformation(actual);

		Assert.Equal(expect.ToValidateText(), actual.ToValidateText());
	}

	[Fact]
	public void DDL_PrimaryKeys()
	{
		var def = GetDefinition();
		var col = def.ColumnDefinitions.Where(x => x.ColumnName == "price").First();
		col.IsPrimaryKey = true;

		var expect = """
create table if not exists test (
    test_id serial8 not null, 
    price int8 not null, 
    remarks text, 
    primary key(test_id, price)
)
""";
		var actual = def.ToCreateTableCommandText();
		Logger.LogInformation(actual);

		Assert.Equal(expect.ToValidateText(), actual.ToValidateText());
	}

	[Fact]
	public void DDL_UniqueKey()
	{
		var def = GetDefinition();
		var col = def.ColumnDefinitions.Where(x => x.ColumnName == "price").First();
		col.IsUniqueKey = true;

		var expect = """
create table if not exists test (
    test_id serial8 not null, 
    price int8 not null, 
    remarks text, 
    primary key(test_id), 
    unique(price)
)
""";
		var actual = def.ToCreateTableCommandText();
		Logger.LogInformation(actual);

		Assert.Equal(expect.ToValidateText(), actual.ToValidateText());
	}

	[Fact]
	public void DDL_UniqueKeys()
	{
		var def = GetDefinition();
		def.ColumnDefinitions.Where(x => x.ColumnName == "price").First().IsUniqueKey = true;
		def.ColumnDefinitions.Where(x => x.ColumnName == "remarks").First().IsUniqueKey = true;

		var expect = """
create table if not exists test (
    test_id serial8 not null, 
    price int8 not null, 
    remarks text, 
    primary key(test_id), 
    unique(price, remarks)
)
""";
		var actual = def.ToCreateTableCommandText();
		Logger.LogInformation(actual);

		Assert.Equal(expect.ToValidateText(), actual.ToValidateText());
	}

	[Fact]
	public void DDL_Index()
	{
		var def = GetDefinition();
		def.Indexes.Add(new DbIndexDefinition()
		{
			Identifers = { "price" }
		});

		var expect = """
create index if not exists i1_test on test (price)
""";
		var actuals = def.ToCreateIndexCommandTexts().ToList();
		Assert.Single(actuals);

		Logger.LogInformation(actuals[0]);

		Assert.Equal(expect.ToValidateText(), actuals[0].ToValidateText());
	}

	[Fact]
	public void DDL_Indexes()
	{
		var def = GetDefinition();
		def.Indexes.Add(new DbIndexDefinition()
		{
			Identifers = { "price" }
		});
		def.Indexes.Add(new DbIndexDefinition()
		{
			Identifers = { "price", "remarks" },
			IsUnique = true
		});

		var expect0 = """
create index if not exists i1_test on test (price)
""";
		var expect1 = """
create unique index if not exists i2_test on test (price, remarks)
""";

		var actuals = def.ToCreateIndexCommandTexts().ToList();
		Assert.Equal(2, actuals.Count);

		Logger.LogInformation(actuals[0]);
		Logger.LogInformation(actuals[1]);

		Assert.Equal(expect0.ToValidateText(), actuals[0].ToValidateText());
		Assert.Equal(expect1.ToValidateText(), actuals[1].ToValidateText());
	}
}
