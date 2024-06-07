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
/// A parse tree for variable declarations.
/// </summary>
	public sealed class VariableListDeclaration : ModifiedDeclaration
	{

		private readonly VariableDeclaratorCollection _VariableDeclarators;

		/// <summary>
    /// The variables being declared.
    /// </summary>
		public VariableDeclaratorCollection VariableDeclarators
		{
			get
			{
				return _VariableDeclarators;
			}
		}

		/// <summary>
    /// Constructs a parse tree for variable declarations.
    /// </summary>
    /// <param name="attributes">The attributes on the declaration.</param>
    /// <param name="modifiers">The modifiers on the declaration.</param>
    /// <param name="variableDeclarators">The variables being declared.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public VariableListDeclaration(AttributeBlockCollection attributes, ModifierCollection modifiers, VariableDeclaratorCollection variableDeclarators, Span span, IList<CommentType> comments) : base(TreeType.VariableListDeclaration, attributes, modifiers, span, comments)
		{

			if (variableDeclarators is null)
			{
				throw new ArgumentNullException("variableDeclarators");
			}

			SetParent(variableDeclarators);

			_VariableDeclarators = variableDeclarators;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);
			AddChild(childList, VariableDeclarators);
		}
	}
}