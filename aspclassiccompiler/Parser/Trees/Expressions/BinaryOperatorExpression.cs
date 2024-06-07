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
/// A parse tree for a binary operator expression.
/// </summary>
	public sealed class BinaryOperatorExpression : Expression
	{

		private readonly Expression _LeftOperand;
		private readonly OperatorType _Operator;
		private readonly Location _OperatorLocation;
		private readonly Expression _RightOperand;

		/// <summary>
    /// The left operand expression.
    /// </summary>
		public Expression LeftOperand
		{
			get
			{
				return _LeftOperand;
			}
		}

		/// <summary>
    /// The operator.
    /// </summary>
		public OperatorType Operator
		{
			get
			{
				return _Operator;
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
    /// The right operand expression.
    /// </summary>
		public Expression RightOperand
		{
			get
			{
				return _RightOperand;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a binary operation.
    /// </summary>
    /// <param name="leftOperand">The left operand expression.</param>
    /// <param name="operator">The operator.</param>
    /// <param name="operatorLocation">The location of the operator.</param>
    /// <param name="rightOperand">The right operand expression.</param>
    /// <param name="span">The location of the parse tree.</param>
		public BinaryOperatorExpression(Expression leftOperand, OperatorType @operator, Location operatorLocation, Expression rightOperand, Span span) : base(TreeType.BinaryOperatorExpression, span)
		{

			if (@operator < OperatorType.Plus || @operator > OperatorType.GreaterThanEquals)
			{
				throw new ArgumentOutOfRangeException("operator");
			}

			if (leftOperand is null)
			{
				throw new ArgumentNullException("leftOperand");
			}

			if (rightOperand is null)
			{
				throw new ArgumentNullException("rightOperand");
			}

			SetParent(leftOperand);
			SetParent(rightOperand);

			_LeftOperand = leftOperand;
			_Operator = @operator;
			_OperatorLocation = operatorLocation;
			_RightOperand = rightOperand;
		}

		public override bool IsConstant
		{
			get
			{
				return LeftOperand.IsConstant && RightOperand.IsConstant;
			}
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, LeftOperand);
			AddChild(childList, RightOperand);
		}
	}
}