using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// A parse tree for a statement.
/// </summary>
	public abstract class Statement : Tree
	{

		private readonly ReadOnlyCollection<CommentType> _Comments;

		/// <summary>
    /// The comments for the tree.
    /// </summary>
		public ReadOnlyCollection<CommentType> Comments
		{
			get
			{
				return _Comments;
			}
		}

		protected Statement(TreeType @type, Span span, IList<CommentType> comments) : base(type, span)
		{

			// LC Allow declarations to be craeted as statement
			Debug.Assert(type >= TreeType.EmptyStatement && type <= TreeType.EndBlockStatement || type >= TreeType.EmptyDeclaration && type <= TreeType.DelegateFunctionDeclaration);

			if (comments is not null)
			{
				_Comments = new ReadOnlyCollection<CommentType>(comments);
			}
		}
	}
}