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
/// A parse tree for an AddHandler statement.
/// </summary>
	public sealed class AddHandlerStatement : HandlerStatement
	{

		/// <summary>
    /// Constructs a new parse tree for an AddHandler statement.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="commaLocation">The location of the ','.</param>
    /// <param name="delegateExpression">The delegate expression.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public AddHandlerStatement(Expression name, Location commaLocation, Expression delegateExpression, Span span, IList<CommentType> comments) : base(TreeType.AddHandlerStatement, name, commaLocation, delegateExpression, span, comments)
		{
		}
	}
}