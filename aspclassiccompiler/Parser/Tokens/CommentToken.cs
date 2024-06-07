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
/// A comment token.
/// </summary>
	public sealed class CommentToken : Token
	{

		private readonly bool _IsREM;    // Was the comment preceded by a quote or by REM?
		private readonly string _Comment;   // Comment can be Nothing

		/// <summary>
    /// Whether the comment was preceded by REM.
    /// </summary>
		public bool IsREM
		{
			get
			{
				return _IsREM;
			}
		}

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
    /// Constructs a new comment token.
    /// </summary>
    /// <param name="comment">The comment value.</param>
    /// <param name="isREM">Whether the comment was preceded by REM.</param>
    /// <param name="span">The location of the comment.</param>
		public CommentToken(string comment, bool isREM, Span span) : base(TokenType.Comment, span)
		{

			if (comment is null)
			{
				throw new ArgumentNullException("comment");
			}

			_IsREM = isREM;
			_Comment = comment;
		}
	}
}