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
/// A parse tree for an event declaration.
/// </summary>
	public sealed class EventDeclaration : SignatureDeclaration
	{

		private readonly NameCollection _ImplementsList;

		/// <summary>
    /// The list of implemented members.
    /// </summary>
		public NameCollection ImplementsList
		{
			get
			{
				return _ImplementsList;
			}
		}

		/// <summary>
    /// Constructs a parse tree for an event declaration.
    /// </summary>
    /// <param name="attributes">The attributes for the parse tree.</param>
    /// <param name="modifiers">The modifiers for the parse tree.</param>
    /// <param name="keywordLocation">The location of the keyword.</param>
    /// <param name="name">The name of the declaration.</param>
    /// <param name="parameters">The parameters of the declaration.</param>
    /// <param name="asLocation">The location of the 'As', if any.</param>
    /// <param name="resultTypeAttributes">The attributes on the result type, if any.</param>
    /// <param name="resultType">The result type, if any.</param>
    /// <param name="implementsList">The list of implemented members.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public EventDeclaration(AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, SimpleName name, ParameterCollection parameters, Location asLocation, AttributeBlockCollection resultTypeAttributes, TypeName resultType, NameCollection implementsList, Span span, IList<CommentType> comments) : base(TreeType.EventDeclaration, attributes, modifiers, keywordLocation, name, null, parameters, asLocation, resultTypeAttributes, resultType, span, comments)
		{

			SetParent(implementsList);
			_ImplementsList = implementsList;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);
			AddChild(childList, ImplementsList);
		}
	}
}