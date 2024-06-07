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
/// A parse tree for a constructor declaration.
/// </summary>
	public sealed class ConstructorDeclaration : MethodDeclaration
	{

		/// <summary>
    /// Creates a new parse tree for a constructor declaration.
    /// </summary>
    /// <param name="attributes">The attributes for the parse tree.</param>
    /// <param name="modifiers">The modifiers for the parse tree.</param>
    /// <param name="keywordLocation">The location of the keyword.</param>
    /// <param name="name">The name of the declaration.</param>
    /// <param name="parameters">The parameters of the declaration.</param>
    /// <param name="statements">The statements in the declaration.</param>
    /// <param name="endDeclaration">The end block declaration, if any.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ConstructorDeclaration(AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, SimpleName name, ParameterCollection parameters, StatementCollection statements, EndBlockDeclaration endDeclaration, Span span, IList<CommentType> comments) : base(TreeType.ConstructorDeclaration, attributes, modifiers, keywordLocation, name, null, parameters, default, null, null, null, null, statements, endDeclaration, span, comments)
		{
		}
	}
}