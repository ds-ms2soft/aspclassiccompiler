using System.Collections.Generic;

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
/// A parse tree for an expression statement.
/// </summary>
	public sealed class ReturnStatement : ExpressionStatement
	{

		/// <summary>
    /// Constructs a new parse tree for a Return statement.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ReturnStatement(Expression expression, Span span, IList<CommentType> comments) : base(TreeType.ReturnStatement, expression, span, comments)
		{
		}
	}
}