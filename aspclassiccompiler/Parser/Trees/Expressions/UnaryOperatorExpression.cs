using System;

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
/// A parse tree for an unary operator expression.
/// </summary>
	public sealed class UnaryOperatorExpression : UnaryExpression
	{

		private OperatorType _Operator;

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
    /// Constructs a new unary operator expression parse tree.
    /// </summary>
    /// <param name="operator">The type of the unary operator.</param>
    /// <param name="operand">The operand of the operator.</param>
    /// <param name="span">The location of the parse tree.</param>
		public UnaryOperatorExpression(OperatorType @operator, Expression operand, Span span) : base(TreeType.UnaryOperatorExpression, operand, span)
		{

			if (@operator < OperatorType.UnaryPlus || @operator > OperatorType.Not)
			{
				throw new ArgumentOutOfRangeException("operator");
			}

			_Operator = @operator;
		}
	}
}