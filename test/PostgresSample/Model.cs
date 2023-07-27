using RedOrb;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PostgresSample;

/*
 * https://learn.microsoft.com/ja-jp/ef/core/get-started/overview/first-app?tabs=netcore-cli
 */

public class Blog
{
	public int? BlogId { get; set; }
	public string Url { get; set; } = string.Empty;
	public ObservableCollection<Post> Posts { get; } = new();

	public Blog()
	{
		Posts.CollectionChanged += Posts_CollectionChanged;
	}

	private void Posts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			if (e.NewItems == null) return;
			foreach (Post item in e.NewItems)
			{
				item.Blog = this;
			}
		}
	}
}

public class Post
{
	public int? PostId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public Blog Blog { get; set; } = null!;
	public ObservableCollection<Comment> Comments { get; } = new();

	public Post()
	{
		Comments.CollectionChanged += Posts_CollectionChanged;
	}

	private void Posts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			if (e.NewItems == null) return;
			foreach (Comment item in e.NewItems)
			{
				item.Post = this;
			}
		}
	}
}

public class Comment
{
	public int? CommentId { get; set; }
	public string CommentText { get; set; } = string.Empty;
	public Post Post { get; set; } = null!;
}

public static class DbTableDefinitionRepository
{
	public static DbTableDefinition<Blog> GetBlogDefinition()
	{
		return new DbTableDefinition<Blog>()
		{
			TableName = "blogs",
			ColumnDefinitions =
			{
				new () {Identifer = nameof(Blog.BlogId), ColumnName = "blog_id", ColumnType= "serial8", RelationColumnType = "bigint", IsPrimaryKey= true, IsAutoNumber = true},
				new () {Identifer = nameof(Blog.Url), ColumnName = "url", ColumnType= "text"},
			},
			ChildIdentifers = {
				nameof(Blog.Posts)
			},
			Indexes =
			{
				new() {Identifers = { nameof(Blog.Url) }, IsUnique = true},
			}
		};
	}

	public static DbTableDefinition<Post> GetPostDefinition()
	{
		return new DbTableDefinition<Post>()
		{
			TableName = "posts",
			ColumnDefinitions =
			{
				new () {Identifer = nameof(Post.PostId), ColumnName = "post_id", ColumnType= "serial8", RelationColumnType = "bigint", IsPrimaryKey= true, IsAutoNumber = true},
				new () {Identifer = nameof(Post.Title), ColumnName = "title", ColumnType= "text"},
				new () {Identifer = nameof(Post.Content), ColumnName = "content", ColumnType= "text"},
			},
			ParentRelations = {
				new () {Identifer = nameof(Post.Blog), ColumnNames = { "blog_id" } , IdentiferType = typeof(Blog)}
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
			ColumnDefinitions =
			{
				new () {Identifer = nameof(Comment.CommentId), ColumnName = "comment_id", ColumnType= "serial8", RelationColumnType = "bigint", IsPrimaryKey= true, IsAutoNumber = true},
				new () {Identifer = nameof(Comment.CommentText), ColumnName = "comment_text", ColumnType= "text"},
			},
			ParentRelations = {
				new () {Identifer = nameof(Comment.Post), ColumnNames = { "post_id" } , IdentiferType = typeof(Post)}
			}
		};
	}
}