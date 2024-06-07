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
/// A parse tree for an assignment statement.
/// </summary>
	public sealed class AssignmentStatement : Statement
	{

		private readonly Expression _TargetExpression;
		private readonly Location _OperatorLocation;
		private readonly Expression _SourceExpression;
		private readonly bool _IsSetStatement;

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

		public bool IsSetStatement
		{
			get
			{
				return _IsSetStatement;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for an assignment statement.
    /// </summary>
    /// <param name="targetExpression">The target of the assignment.</param>
    /// <param name="operatorLocation">The location of the operator.</param>
    /// <param name="sourceExpression">The source of the assignment.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
    /// <param name="isSetStatement">Whether is is a set statement</param>
		public AssignmentStatement(Expression targetExpression, Location operatorLocation, Expression sourceExpression, Span span, IList<CommentType> comments, bool isSetStatement) : base(TreeType.AssignmentStatement, span, comments)
		{

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

			_TargetExpression = targetExpression;
			_OperatorLocation = operatorLocation;
			_SourceExpression = sourceExpression;
			_IsSetStatement = isSetStatement;
		}

		/// <summary>
    /// Constructs a new parse tree for an assignment statement.
    /// </summary>
    /// <param name="targetExpression">The target of the assignment.</param>
    /// <param name="operatorLocation">The location of the operator.</param>
    /// <param name="sourceExpression">The source of the assignment.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public AssignmentStatement(Expression targetExpression, Location operatorLocation, Expression sourceExpression, Span span, IList<CommentType> comments) : this(targetExpression, operatorLocation, sourceExpression, span, comments, false)
		{
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, TargetExpression);
			AddChild(childList, SourceExpression);
		}
	}
}