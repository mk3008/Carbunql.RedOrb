# RedOrb
![GitHub](https://img.shields.io/github/license/mk3008/RedOrb)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/mk3008/RedOrb)
![Github Last commit](https://img.shields.io/github/last-commit/mk3008/RedOrb)  
[![SqModel](https://img.shields.io/nuget/v/RedOrb.svg)](https://www.nuget.org/packages/RedOrb/) 
[![SqModel](https://img.shields.io/nuget/dt/RedOrb.svg)](https://www.nuget.org/packages/RedOrb/) 

Simple, Intuitive, ORM

ORM for people who simply want to access a database.

# Demo
Although a configuration file is required, CRUD processing is very simple to write.

## Model
```cs
using RedOrb.Attributes;

[DbTable("blogs")]
public class Blog
{
    [DbColumn("serial8", IsAutoNumber = true, IsPrimaryKey = true)]
    public int BlogId { get; set; }
    [DbColumn("text")]
    public string Url { get; set; } = string.Empty;
}
```

## Configuration
Register the type mapping only once before using RedOrb.

```cs
using RedOrb;
using RedOrb.Attributes;

ObjectRelationMapper.PlaceholderIdentifer = ":";
ObjectRelationMapper.AddTypeHandler(DefinitionBuilder.Create<Blog>());
```

## Create Table
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
cn.CreateTableOrDefault<Blog>();
```

```sql
create table if not exists blogs (
    blog_id serial8 not null, 
    url text not null, 
    primary key(blog_id)
)
```

## Create(Insert)
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
var newBlog = new Blog { Url = "http://blogs.msdn.com/adonet" };
Assert.Equal(0, newBlog.BlodId);
cn.Save(newBlog);
Assert.NotEqual(0, newBlog.BlodId);
```

```sql
/*
  Url = '"http://blogs.msdn.com/adonet'
*/
insert into blogs (url)
SELECT
    v.url
FROM
    (
        VALUES
            (:Url)
    ) AS v (
        url
    )
returning blog_id;
```

## Read(Select)
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
var blog = cn.Load(new Blog() { BlodId = 1 });
```

```sql
/*
  key0 = 1
*/
SELECT
    t0.blog_id AS t0BlogId,
    t0.url AS t0Url
FROM
    blogs AS t0
WHERE
    t0.blog_id = :key0
```

## Update
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
var blog = cn.Load(new Blog() { BlodId = 1 });
blog.Url = "https://devblogs.microsoft.com/dotnet";
cn.Save(blog);
```

```sql
/*
  BlogId = 1
  Url = 'https://devblogs.microsoft.com/dotnet'
*/
UPDATE
    blogs AS d
SET
    url = q.url
FROM
    (
        SELECT
            v.blog_id,
            v.url
        FROM
            (
                VALUES
                    (:BlogId, :Url)
            ) AS v (
                blog_id, url
            )
    ) AS q
WHERE
    d.blog_id = q.blog_id;
```

## Delete
```cs
using RedOrb;

using IDbConnection cn = SomethingMethod();
cn.Delete(new Blog() { BlodId = 1 });
```

```sql
/*
  BlogId = 1
*/
delete from blogs as d
where
    (d.blog_id) in (select v.blog_id from (values (:BlogId)) as v (blog_id));
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
