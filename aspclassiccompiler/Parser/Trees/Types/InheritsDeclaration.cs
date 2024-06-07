using System;
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
/// A parse tree for an Inherits declaration.
/// </summary>
	public sealed class InheritsDeclaration : Declaration
	{

		private readonly TypeNameCollection _InheritedTypes;

		/// <summary>
    /// The list of types.
    /// </summary>
		public TypeNameCollection InheritedTypes
		{
			get
			{
				return _InheritedTypes;
			}
		}

		/// <summary>
    /// Constructs a parse tree for an Inherits declaration.
    /// </summary>
    /// <param name="inheritedTypes">The types inherited or implemented.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public InheritsDeclaration(TypeNameCollection inheritedTypes, Span span, IList<CommentType> comments) : base(TreeType.InheritsDeclaration, span, comments)
		{

			if (inheritedTypes is null)
			{
				throw new ArgumentNullException("inheritedTypes");
			}

			SetParent(inheritedTypes);

			_InheritedTypes = inheritedTypes;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);

			AddChild(childList, InheritedTypes);
		}
	}
}