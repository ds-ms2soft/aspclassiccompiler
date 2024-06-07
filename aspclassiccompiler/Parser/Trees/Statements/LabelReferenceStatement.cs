using System.Collections.Generic;
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
/// A parse tree for a statement that refers to a label.
/// </summary>
	public abstract class LabelReferenceStatement : Statement
	{

		private readonly SimpleName _Name;
		private readonly bool _IsLineNumber;

		/// <summary>
    /// The name of the label being referred to.
    /// </summary>
		public SimpleName Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// Whether the label is a line number.
    /// </summary>
		public bool IsLineNumber
		{
			get
			{
				return _IsLineNumber;
			}
		}

		protected LabelReferenceStatement(TreeType @type, SimpleName name, bool isLineNumber, Span span, IList<CommentType> comments) : base(type, span, comments)
		{

			Debug.Assert(type == TreeType.GotoStatement || type == TreeType.LabelStatement || type == TreeType.OnErrorStatement || type == TreeType.ResumeStatement);

			SetParent(name);
			_Name = name;
			_IsLineNumber = isLineNumber;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Name);
		}
	}
}