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
/// A parse tree for a date literal expression.
/// </summary>
	public sealed class DateLiteralExpression : LiteralExpression
	{

		private readonly DateTime _Literal;

		/// <summary>
    /// The literal value.
    /// </summary>
		public DateTime Literal
		{
			get
			{
				return _Literal;
			}
		}

		// LC
		public override object Value
		{
			get
			{
				return _Literal;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a date literal.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="span">The location of the parse tree.</param>
		public DateLiteralExpression(DateTime literal, Span span) : base(TreeType.DateLiteralExpression, span)
		{
			_Literal = literal;
		}
	}
}