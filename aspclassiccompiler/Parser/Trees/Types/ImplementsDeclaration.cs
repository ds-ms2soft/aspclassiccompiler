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
/// A parse tree for an Implements declaration.
/// </summary>
	public sealed class ImplementsDeclaration : Declaration
	{

		private readonly TypeNameCollection _ImplementedTypes;

		/// <summary>
    /// The list of types.
    /// </summary>
		public TypeNameCollection ImplementedTypes
		{
			get
			{
				return _ImplementedTypes;
			}
		}

		/// <summary>
    /// Constructs a parse tree for an Implements declaration.
    /// </summary>
    /// <param name="implementedTypes">The types inherited or implemented.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ImplementsDeclaration(TypeNameCollection implementedTypes, Span span, IList<CommentType> comments) : base(TreeType.ImplementsDeclaration, span, comments)
		{

			if (implementedTypes is null)
			{
				throw new ArgumentNullException("implementedTypes");
			}

			SetParent(implementedTypes);

			_ImplementedTypes = implementedTypes;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);

			AddChild(childList, ImplementedTypes);
		}
	}
}