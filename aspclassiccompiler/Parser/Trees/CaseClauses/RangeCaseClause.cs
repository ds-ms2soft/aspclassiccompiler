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
/// A parse tree for a case clause that compares against a range of values.
/// </summary>
	public sealed class RangeCaseClause : CaseClause
	{

		private readonly Expression _RangeExpression;

		/// <summary>
    /// The range expression.
    /// </summary>
		public Expression RangeExpression
		{
			get
			{
				return _RangeExpression;
			}
		}

		/// <summary>
    /// Constructs a new range case clause parse tree.
    /// </summary>
    /// <param name="rangeExpression">The range expression.</param>
    /// <param name="span">The location of the parse tree.</param>
		public RangeCaseClause(Expression rangeExpression, Span span) : base(TreeType.RangeCaseClause, span)
		{

			if (rangeExpression is null)
			{
				throw new ArgumentNullException("rangeExpression");
			}

			SetParent(rangeExpression);

			_RangeExpression = rangeExpression;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, RangeExpression);
		}
	}
}