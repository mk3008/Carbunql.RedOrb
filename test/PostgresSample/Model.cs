using PropertyBind;
using RedOrb;
using RedOrb.Attributes;

namespace PostgresSample;

/*
 * https://learn.microsoft.com/ja-jp/ef/core/get-started/overview/first-app?tabs=netcore-cli
 */

[GeneratePropertyBind(nameof(Posts), nameof(Post.Blog))]
[DbTable("blogs")]
[DbIndex(true, nameof(Url))]
public partial class Blog
{
	[DbColumn("serial8", IsAutoNumber = true, IsPrimaryKey = true)]
	public int? BlogId { get; set; }
	[DbColumn("text")]
	public string Url { get; set; } = string.Empty;

	[DbChildren]
	public IList<Post> Posts { get; }
}

[GeneratePropertyBind(nameof(Comments), nameof(Comment.Post))]
[DbTable("posts")]
public partial class Post
{
	[DbColumn("serial8", IsAutoNumber = true, IsPrimaryKey = true)]
	public int? PostId { get; set; }
	[DbParentRelation]
	[DbParentRelationColumn("bigint", nameof(Post.Blog.BlogId))]
	public Blog Blog { get; set; } = null!;
	[DbColumn("text")]
	public string Title { get; set; } = string.Empty;
	[DbColumn("text")]
	public string Content { get; set; } = string.Empty;
	[DbChildren]
	public IList<Comment> Comments { get; }
}

[DbTable("comments")]
public class Comment
{
	[DbColumn("serial8", IsAutoNumber = true, IsPrimaryKey = true)]
	public int? CommentId { get; set; }
	[DbColumn("text")]
	public string CommentText { get; set; } = string.Empty;
	[DbParentRelation]
	[DbParentRelationColumn("owner_post_id", "bigint", nameof(Comment.Post.PostId))]
	public Post Post { get; set; } = null!;
}

public static class DbTableDefinitionRepository
{
	public static DbTableDefinition<Blog> GetBlogDefinition()
	{
		return new DbTableDefinition<Blog>()
		{
			TableName = "blogs",
			ColumnContainers =
			{
				new DbColumnDefinition  {
					Identifer = nameof(Blog.BlogId),
					ColumnName = "blog_id",
					ColumnType= "serial8",
					RelationColumnType = "bigint",
					IsPrimaryKey= true,
					IsAutoNumber = true
				},
				new DbColumnDefinition {
					Identifer = nameof(Blog.Url),
					ColumnName = "url",
					ColumnType= "text"
				},
			},
			ChildIdentifers = {
				nameof(Blog.Posts)
			},
			Indexes =
			{
				new DbIndexDefinition {
					Identifers = {
						nameof(Blog.Url)
					},
					IsUnique = true
				},
			}
		};
	}

	public static DbTableDefinition<Post> GetPostDefinition()
	{
		return new DbTableDefinition<Post>()
		{
			TableName = "posts",
			ColumnContainers =
			{
				new DbColumnDefinition {
					Identifer = nameof(Post.PostId),
					ColumnName = "post_id",
					ColumnType= "serial8",
					RelationColumnType = "bigint",
					IsPrimaryKey= true,
					IsAutoNumber = true
				},
				new DbParentRelationDefinition
				{
					Identifer = nameof(Post.Blog),
					IdentiferType= typeof(Blog),
					//Not required if the columns to be mapped have the same name.
					//Relations = new()
					//{
					//	new DbParentRelationColumnDefinition()
					//	{
					//		ColumnName = "blog_id",
					//		ColumnType = "bigint",
					//		ParentIdentifer = nameof(Post.Blog.BlogId),
					//	}
					//}
				},
				new DbColumnDefinition {
					Identifer = nameof(Post.Title),
					ColumnName = "title",
					ColumnType= "text"
				},
				new DbColumnDefinition {
					Identifer = nameof(Post.Content),
					ColumnName = "content",
					ColumnType = "text"
				},
			},
			ChildIdentifers = {
				nameof(Post.Comments)
			}
		};
	}

	public static DbTableDefinition<Comment> GetCommentDefinition()
	{
		return new DbTableDefinition<Comment>()
		{
			TableName = "post_comments",
			ColumnContainers =
			{
				new DbColumnDefinition {
					Identifer = nameof(Comment.CommentId),
					ColumnName = "comment_id",
					ColumnType= "serial8",
					RelationColumnType = "bigint",
					IsPrimaryKey= true,
					IsAutoNumber = true
				},
				new DbParentRelationDefinition {
					Identifer = nameof(Comment.Post),
					IdentiferType = typeof(Post),
					//Required if the column to be mapped is an alias.
					Relations = new()
					{
						new DbParentRelationColumnDefinition()
						{
							ColumnName = "owner_post_id",
							ColumnType = "bigint",
							ParentIdentifer = nameof(Comment.Post.PostId),
						}
					}
				},
				new DbColumnDefinition {
					Identifer = nameof(Comment.CommentText),
					ColumnName = "comment_text",
					ColumnType= "text"
				},
			}
		};
	}
}