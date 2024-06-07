using System;

namespace Dlrsoft.VBScript.Parser
{
	// 
	// Visual Basic .NET Parser
	// 
	// Copyright (C) 2005, Microsoft Corporation. All rights reserved.
	// 
	// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
	// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
	// MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
	// 

	/// <summary>
/// A parse tree for a comment.
/// </summary>
	public sealed class CommentType : Tree
	{

		private readonly string _Comment;
		private readonly bool _IsREM;

		/// <summary>
    /// The text of the comment.
    /// </summary>
		public string Comment
		{
			get
			{
				return _Comment;
			}
		}

		/// <summary>
    /// Whether the comment is a REM comment.
    /// </summary>
		public bool IsREM
		{
			get
			{
				return _IsREM;
			}
		}

		/// <summary>
    /// Constructs a new comment parse tree.
    /// </summary>
    /// <param name="comment">The text of the comment.</param>
    /// <param name="isREM">Whether the comment is a REM comment.</param>
    /// <param name="span">The location of the parse tree.</param>
		public CommentType(string comment, bool isREM, Span span) : base(TreeType.Comment, span)
		{

			if (comment is null)
			{
				throw new ArgumentNullException("comment");
			}

			_Comment = comment;
			_IsREM = isREM;
		}
	}
}