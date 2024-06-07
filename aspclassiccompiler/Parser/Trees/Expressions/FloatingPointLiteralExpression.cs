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
/// A parse tree for a floating point literal.
/// </summary>
	public sealed class FloatingPointLiteralExpression : LiteralExpression
	{

		private readonly double _Literal;
		private readonly TypeCharacter _TypeCharacter;

		/// <summary>
    /// The literal value.
    /// </summary>
		public double Literal
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
    /// The type character on the literal value.
    /// </summary>
		public TypeCharacter TypeCharacter
		{
			get
			{
				return _TypeCharacter;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a floating point literal.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="typeCharacter">The type character on the literal.</param>
    /// <param name="span">The location of the parse tree.</param>
		public FloatingPointLiteralExpression(double literal, TypeCharacter typeCharacter, Span span) : base(TreeType.FloatingPointLiteralExpression, span)
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