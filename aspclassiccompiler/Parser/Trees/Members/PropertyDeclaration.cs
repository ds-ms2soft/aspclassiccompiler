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
/// A parse tree for a property declaration.
/// </summary>
	public sealed class PropertyDeclaration : SignatureDeclaration
	{

		private readonly NameCollection _ImplementsList;
		private readonly DeclarationCollection _Accessors;
		private readonly EndBlockDeclaration _EndDeclaration;

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
    /// The property accessors.
    /// </summary>
		public DeclarationCollection Accessors
		{
			get
			{
				return _Accessors;
			}
		}

		/// <summary>
    /// The End Property declaration, if any.
    /// </summary>
		public EndBlockDeclaration EndDeclaration
		{
			get
			{
				return _EndDeclaration;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a property declaration.
    /// </summary>
    /// <param name="attributes">The attributes on the declaration.</param>
    /// <param name="modifiers">The modifiers on the declaration.</param>
    /// <param name="keywordLocation">The location of the keyword.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="parameters">The parameters of the property.</param>
    /// <param name="asLocation">The location of the 'As', if any.</param>
    /// <param name="resultTypeAttributes">The attributes on the result type.</param>
    /// <param name="resultType">The result type, if any.</param>
    /// <param name="implementsList">The implements list.</param>
    /// <param name="accessors">The property accessors.</param>
    /// <param name="endDeclaration">The End Property declaration, if any.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public PropertyDeclaration(AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, SimpleName name, ParameterCollection parameters, Location asLocation, AttributeBlockCollection resultTypeAttributes, TypeName resultType, NameCollection implementsList, DeclarationCollection accessors, EndBlockDeclaration endDeclaration, Span span, IList<CommentType> comments) : base(TreeType.PropertyDeclaration, attributes, modifiers, keywordLocation, name, null, parameters, asLocation, resultTypeAttributes, resultType, span, comments)
		{

			SetParent(accessors);
			SetParent(endDeclaration);
			SetParent(implementsList);

			_ImplementsList = implementsList;
			_Accessors = accessors;
			_EndDeclaration = endDeclaration;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);
			AddChild(childList, ImplementsList);
			AddChild(childList, Accessors);
			AddChild(childList, EndDeclaration);
		}
	}
}