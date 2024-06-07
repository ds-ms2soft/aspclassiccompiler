﻿using System.Collections.Generic;
using System.Diagnostics;

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
/// A parse tree for a possibly generic block declaration.
/// </summary>
	public abstract class GenericBlockDeclaration : BlockDeclaration
	{

		private readonly TypeParameterCollection _TypeParameters;

		/// <summary>
    /// The type parameters of the type, if any.
    /// </summary>
		public TypeParameterCollection TypeParameters
		{
			get
			{
				return _TypeParameters;
			}
		}

		protected GenericBlockDeclaration(TreeType @type, AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, SimpleName name, TypeParameterCollection typeParameters, DeclarationCollection declarations, EndBlockDeclaration endStatement, Span span, IList<CommentType> comments) : base(type, attributes, modifiers, keywordLocation, name, declarations, endStatement, span, comments)
		{

			Debug.Assert(type == TreeType.ClassDeclaration || type == TreeType.InterfaceDeclaration || type == TreeType.StructureDeclaration);

			SetParent(typeParameters);
			_TypeParameters = typeParameters;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);

			AddChild(childList, TypeParameters);
		}
	}
}