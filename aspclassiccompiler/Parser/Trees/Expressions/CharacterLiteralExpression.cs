
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
/// A parse tree for a character literal expression.
/// </summary>
	public sealed class CharacterLiteralExpression : LiteralExpression
	{

		private readonly char _Literal;

		/// <summary>
    /// The literal value.
    /// </summary>
		public char Literal
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
    /// Constructs a new parse tree for a character literal expression.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="span">The location of the parse tree.</param>
		public CharacterLiteralExpression(char literal, Span span) : base(TreeType.CharacterLiteralExpression, span)
		{
			_Literal = literal;
		}
	}
}