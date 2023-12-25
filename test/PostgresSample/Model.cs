﻿using RedOrb;
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