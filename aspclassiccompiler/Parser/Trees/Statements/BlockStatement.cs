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
/// A parse tree for a block statement.
/// </summary>
	public abstract class BlockStatement : Statement
	{

		private readonly StatementCollection _Statements;

		/// <summary>
    /// The statements in the block.
    /// </summary>
		public StatementCollection Statements
		{
			get
			{
				return _Statements;
			}
		}

		protected BlockStatement(TreeType @type, StatementCollection statements, Span span, IList<CommentType> comments) : base(type, span, comments)
		{

			_Statements = statements;
			SetParent(statements);
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Statements);
		}
	}
}