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
	/// A parse tree for a literal expression.
	/// </summary>
	public abstract class LiteralExpression : Expression
	{

		public sealed override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		// LC add property to get value
		public abstract object Value { get; }

		protected LiteralExpression(TreeType @type, Span span) : base(type, span)
		{

			Debug.Assert(type >= TreeType.StringLiteralExpression && type <= TreeType.BooleanLiteralExpression);
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}