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
/// A parse tree for a call or index expression.
/// </summary>
	public sealed class CallOrIndexExpression : Expression
	{

		private readonly Expression _TargetExpression;
		private readonly ArgumentCollection _Arguments;

		/// <summary>
    /// The target of the call or index.
    /// </summary>
		public Expression TargetExpression
		{
			get
			{
				return _TargetExpression;
			}
		}

		/// <summary>
    /// The arguments to the call or index.
    /// </summary>
		public ArgumentCollection Arguments
		{
			get
			{
				return _Arguments;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a call or index expression.
    /// </summary>
    /// <param name="targetExpression">The target of the call or index.</param>
    /// <param name="arguments">The arguments to the call or index.</param>
    /// <param name="span">The location of the parse tree.</param>
		public CallOrIndexExpression(Expression targetExpression, ArgumentCollection arguments, Span span) : base(TreeType.CallOrIndexExpression, span)
		{

			if (targetExpression is null)
			{
				throw new ArgumentNullException("targetExpression");
			}

			SetParent(targetExpression);
			SetParent(arguments);

			_TargetExpression = targetExpression;
			_Arguments = arguments;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, TargetExpression);
			AddChild(childList, Arguments);
		}
	}
}