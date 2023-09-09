# RedOrb
Simple, Intuitive, ORM

ORM for people who simply want to access a database.

# Demo
Although a configuration file is required, CRUD processing is very simple to write.
## Create
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
using var trn = cn.BeginTransaction();

var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
cn.Save(newBlog);
```
## Read
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
using var trn = cn.BeginTransaction();

var blog = cn.Load(new Blog() { BlodId = 1 });
```

## Update
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
using var trn = cn.BeginTransaction();

var blog = cn.Load(new Blog() { BlodId = 1 });

blog.Url = "https://devblogs.microsoft.com/dotnet";

cn.Save(blog);
```

## Delete
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
using var trn = cn.BeginTransaction();

cn.Delete(new Blog() { BlodId = 1 });
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




