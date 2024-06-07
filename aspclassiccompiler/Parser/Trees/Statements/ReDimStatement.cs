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
/// A parse tree for a ReDim statement.
/// </summary>
	public sealed class ReDimStatement : Statement
	{

		private readonly Location _PreserveLocation;
		private readonly ExpressionCollection _Variables;

		/// <summary>
    /// The location of the 'Preserve', if any.
    /// </summary>
		public Location PreserveLocation
		{
			get
			{
				return _PreserveLocation;
			}
		}

		/// <summary>
    /// The variables to redimension (includes bounds).
    /// </summary>
		public ExpressionCollection Variables
		{
			get
			{
				return _Variables;
			}
		}

		/// <summary>
    /// Whether the statement included a Preserve keyword.
    /// </summary>
		public bool IsPreserve
		{
			get
			{
				return PreserveLocation.IsValid;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a ReDim statement.
    /// </summary>
    /// <param name="preserveLocation">The location of the 'Preserve', if any.</param>
    /// <param name="variables">The variables to redimension (includes bounds).</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ReDimStatement(Location preserveLocation, ExpressionCollection variables, Span span, IList<CommentType> comments) : base(TreeType.ReDimStatement, span, comments)
		{

			if (variables is null)
			{
				throw new ArgumentNullException("variables");
			}

			SetParent(variables);
			_PreserveLocation = preserveLocation;
			_Variables = variables;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Variables);
		}
	}
}