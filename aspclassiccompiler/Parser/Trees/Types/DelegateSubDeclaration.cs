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
/// A parse tree for a delegate Sub declaration.
/// </summary>
	public sealed class DelegateSubDeclaration : DelegateDeclaration
	{

		/// <summary>
    /// Constructs a new parse tree for a delegate Sub declaration.
    /// </summary>
    /// <param name="attributes">The attributes for the parse tree.</param>
    /// <param name="modifiers">The modifiers for the parse tree.</param>
    /// <param name="keywordLocation">The location of the keyword.</param>
    /// <param name="subLocation">The location of the 'Sub'.</param>
    /// <param name="name">The name of the declaration.</param>
    /// <param name="typeParameters">The type parameters of the declaration, if any.</param>
    /// <param name="parameters">The parameters of the declaration.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public DelegateSubDeclaration(AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, Location subLocation, SimpleName name, TypeParameterCollection typeParameters, ParameterCollection parameters, Span span, IList<CommentType> comments) : base(TreeType.DelegateSubDeclaration, attributes, modifiers, keywordLocation, subLocation, name, typeParameters, parameters, default, null, null, span, comments)
		{
		}
	}
}