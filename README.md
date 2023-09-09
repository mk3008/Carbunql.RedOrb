# RedOrb
Simple, Intuitive, ORM

ORM for people who simply want to access a database.

# Demo
Although a configuration file is required, CRUD processing is very simple to write.
## Create
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();

var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
cn.Save(newBlog);
```
## Read
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();

var blog = cn.Load(new Blog() { BlodId = 1 });
```

## Update
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();

var blog = cn.Load(new Blog() { BlodId = 1 });

blog.Url = "https://devblogs.microsoft.com/dotnet";

cn.Save(blog);
```

## Delete
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();

cn.Delete(new Blog() { BlodId = 1 });
```

## Create Table
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();

cn.CreateTableOrDefault<Blog>();
```

## Configuration
```cs
using RedOrb;

ObjectRelationMapper.PlaceholderIdentifer = ":";

var def = new DbTableDefinition<Blog>()
{
    TableName = "blogs",
    ColumnDefinitions =
    {
        new () {Identifer = nameof(Blog.BlogId), ColumnName = "blog_id", ColumnType= "serial8", RelationColumnType = "bigint", IsPrimaryKey= true, IsAutoNumber = true},
        new () {Identifer = nameof(Blog.Url), ColumnName = "url", ColumnType= "text"},
    },
    Indexes =
    {
        new() {Identifers = { nameof(Blog.Url) }, IsUnique = true},
    }
};

ObjectRelationMapper.AddTypeHandler(def);
```

## Model
```cs
public class Blog
{
    public int? BlogId { get; set; }
    public string Url { get; set; } = string.Empty;
}
```


# Features
## General
- Connection classes are not hidden
- Supports SQL logging
- DBMS independent
- Supports sequence keys
- Supports composite keys

## When reading
- All tables with a 1:1 or 1:0..1 relationship are joined and read (default)
- You can set whether to join tables.
- You can use primary key search and unique key search.
- You can also specify any search conditions.

## When saving
- All tables with 1 to 0..N relationships are saved.

# Constraints
## General
- When using sequence keys, please make the type Nullable
- Connection class generation is out of scope
- Requires creation of table definition class

## When reading
- Column filtering is not possible.
