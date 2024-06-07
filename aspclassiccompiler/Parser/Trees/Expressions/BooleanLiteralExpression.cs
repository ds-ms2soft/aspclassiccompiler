
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
/// A parse tree for a Boolean literal expression.
/// </summary>
	public sealed class BooleanLiteralExpression : LiteralExpression
	{

		private readonly bool _Literal;

		/// <summary>
    /// The literal value.
    /// </summary>
		public bool Literal
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
    /// Constructs a new parse tree for a Boolean literal expression.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="span">The location of the parse tree.</param>
		public BooleanLiteralExpression(bool literal, Span span) : base(TreeType.BooleanLiteralExpression, span)
		{
			_Literal = literal;
		}
	}
}