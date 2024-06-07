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
/// A parse tree for a local declaration statement.
/// </summary>
	public sealed class LocalDeclarationStatement : Statement
	{

		private readonly ModifierCollection _Modifiers;
		private readonly VariableDeclaratorCollection _VariableDeclarators;

		/// <summary>
    /// The statement modifiers.
    /// </summary>
		public ModifierCollection Modifiers
		{
			get
			{
				return _Modifiers;
			}
		}

		/// <summary>
    /// The variable declarators.
    /// </summary>
		public VariableDeclaratorCollection VariableDeclarators
		{
			get
			{
				return _VariableDeclarators;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a local declaration statement.
    /// </summary>
    /// <param name="modifiers">The statement modifiers.</param>
    /// <param name="variableDeclarators">The variable declarators.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public LocalDeclarationStatement(ModifierCollection modifiers, VariableDeclaratorCollection variableDeclarators, Span span, IList<CommentType> comments) : base(TreeType.LocalDeclarationStatement, span, comments)
		{

			if (modifiers is null)
			{
				throw new ArgumentNullException("modifers");
			}

			if (variableDeclarators is null)
			{
				throw new ArgumentNullException("variableDeclarators");
			}

			SetParent(modifiers);
			SetParent(variableDeclarators);

			_Modifiers = modifiers;
			_VariableDeclarators = variableDeclarators;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Modifiers);
			AddChild(childList, VariableDeclarators);
		}
	}
}