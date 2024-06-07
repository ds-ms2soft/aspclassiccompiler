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
/// A parse tree for a case clause that compares values.
/// </summary>
	public sealed class ComparisonCaseClause : CaseClause
	{

		private readonly Location _IsLocation;
		private readonly OperatorType _ComparisonOperator;
		private readonly Location _OperatorLocation;
		private readonly Expression _Operand;

		/// <summary>
    /// The location of the 'Is', if any.
    /// </summary>
		public Location IsLocation
		{
			get
			{
				return _IsLocation;
			}
		}

		/// <summary>
    /// The comparison operator used in the case clause.
    /// </summary>
		public OperatorType ComparisonOperator
		{
			get
			{
				return _ComparisonOperator;
			}
		}

		/// <summary>
    /// The location of the comparison operator.
    /// </summary>
		public Location OperatorLocation
		{
			get
			{
				return _OperatorLocation;
			}
		}

		/// <summary>
    /// The operand of the case clause.
    /// </summary>
		public Expression Operand
		{
			get
			{
				return _Operand;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a comparison case clause.
    /// </summary>
    /// <param name="isLocation">The location of the 'Is', if any.</param>
    /// <param name="comparisonOperator">The comparison operator used.</param>
    /// <param name="operatorLocation">The location of the comparison operator.</param>
    /// <param name="operand">The operand of the comparison.</param>
    /// <param name="span">The location of the parse tree.</param>
		public ComparisonCaseClause(Location isLocation, OperatorType comparisonOperator, Location operatorLocation, Expression operand, Span span) : base(TreeType.ComparisonCaseClause, span)
		{

			if (operand is null)
			{
				throw new ArgumentNullException("operand");
			}

			if (comparisonOperator < OperatorType.Equals || comparisonOperator > OperatorType.GreaterThanEquals)
			{
				throw new ArgumentOutOfRangeException("comparisonOperator");
			}

			SetParent(operand);

			_IsLocation = isLocation;
			_ComparisonOperator = comparisonOperator;
			_OperatorLocation = operatorLocation;
			_Operand = operand;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Operand);
		}
	}
}