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
/// A parse tree for a compound assignment statement.
/// </summary>
	public sealed class CompoundAssignmentStatement : Statement
	{

		private readonly Expression _TargetExpression;
		private readonly OperatorType _CompoundOperator;
		private readonly Location _OperatorLocation;
		private readonly Expression _SourceExpression;

		/// <summary>
    /// The target of the assignment.
    /// </summary>
		public Expression TargetExpression
		{
			get
			{
				return _TargetExpression;
			}
		}

		/// <summary>
    /// The compound operator.
    /// </summary>
		public OperatorType CompoundOperator
		{
			get
			{
				return _CompoundOperator;
			}
		}

		/// <summary>
    /// The location of the operator.
    /// </summary>
		public Location OperatorLocation
		{
			get
			{
				return _OperatorLocation;
			}
		}

		/// <summary>
    /// The source of the assignment.
    /// </summary>
		public Expression SourceExpression
		{
			get
			{
				return _SourceExpression;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a compound assignment statement.
    /// </summary>
    /// <param name="compoundOperator">The compound operator.</param>
    /// <param name="targetExpression">The target of the assignment.</param>
    /// <param name="operatorLocation">The location of the operator.</param>
    /// <param name="sourceExpression">The source of the assignment.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public CompoundAssignmentStatement(OperatorType compoundOperator, Expression targetExpression, Location operatorLocation, Expression sourceExpression, Span span, IList<CommentType> comments) : base(TreeType.CompoundAssignmentStatement, span, comments)
		{

			if (compoundOperator < OperatorType.Plus || compoundOperator > OperatorType.Power)
			{
				throw new ArgumentOutOfRangeException("compoundOperator");
			}

			if (targetExpression is null)
			{
				throw new ArgumentNullException("targetExpression");
			}

			if (sourceExpression is null)
			{
				throw new ArgumentNullException("sourceExpression");
			}

			SetParent(targetExpression);
			SetParent(sourceExpression);

			_CompoundOperator = compoundOperator;
			_TargetExpression = targetExpression;
			_OperatorLocation = operatorLocation;
			_SourceExpression = sourceExpression;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, TargetExpression);
			AddChild(childList, SourceExpression);
		}
	}
}