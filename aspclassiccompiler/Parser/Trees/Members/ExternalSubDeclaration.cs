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
/// A parse tree for a Declare Sub statement.
/// </summary>
	public sealed class ExternalSubDeclaration : ExternalDeclaration
	{

		/// <summary>
    /// Constructs a parse tree for a Declare Sub statement.
    /// </summary>
    /// <param name="attributes">The attributes for the parse tree.</param>
    /// <param name="modifiers">The modifiers for the parse tree.</param>
    /// <param name="keywordLocation">The location of the keyword.</param>
    /// <param name="charsetLocation">The location of the 'Ansi', 'Auto' or 'Unicode', if any.</param>
    /// <param name="charset">The charset.</param>
    /// <param name="subLocation">The location of 'Sub'.</param>
    /// <param name="name">The name of the declaration.</param>
    /// <param name="libLocation">The location of 'Lib', if any.</param>
    /// <param name="libLiteral">The library, if any.</param>
    /// <param name="aliasLocation">The location of 'Alias', if any.</param>
    /// <param name="aliasLiteral">The alias, if any.</param>
    /// <param name="parameters">The parameters of the declaration.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ExternalSubDeclaration(AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, Location charsetLocation, Charset charset, Location subLocation, SimpleName name, Location libLocation, StringLiteralExpression libLiteral, Location aliasLocation, StringLiteralExpression aliasLiteral, ParameterCollection parameters, Span span, IList<CommentType> comments) : base(TreeType.ExternalSubDeclaration, attributes, modifiers, keywordLocation, charsetLocation, charset, subLocation, name, libLocation, libLiteral, aliasLocation, aliasLiteral, parameters, default, null, null, span, comments)
		{
		}
	}
}