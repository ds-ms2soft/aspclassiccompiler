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
/// A floating point literal.
/// </summary>
	public sealed class FloatingPointLiteralToken : Token
	{

		private readonly double _Literal;
		private readonly TypeCharacter _TypeCharacter;  // The type character after the literal, if any

		/// <summary>
    /// The value of the literal.
    /// </summary>
		public double Literal
		{
			get
			{
				return _Literal;
			}
		}

		/// <summary>
    /// The type character after the literal.
    /// </summary>
		public TypeCharacter TypeCharacter
		{
			get
			{
				return _TypeCharacter;
			}
		}

		/// <summary>
    /// Constructs a new floating point literal token.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="typeCharacter">The type character of the literal.</param>
    /// <param name="span">The location of the literal.</param>
		public FloatingPointLiteralToken(double literal, TypeCharacter typeCharacter, Span span) : base(TokenType.FloatingPointLiteral, span)
		{

			if (typeCharacter != TypeCharacter.None && typeCharacter != TypeCharacter.SingleSymbol && typeCharacter != TypeCharacter.SingleChar && typeCharacter != TypeCharacter.DoubleSymbol && typeCharacter != TypeCharacter.DoubleChar)

			{
				throw new ArgumentOutOfRangeException("typeCharacter");
			}

			_Literal = literal;
			_TypeCharacter = typeCharacter;
		}
	}
}