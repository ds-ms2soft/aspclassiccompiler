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
/// A parse tree for an integer literal.
/// </summary>
	public sealed class IntegerLiteralExpression : LiteralExpression
	{

		private readonly int _Literal;
		private readonly TypeCharacter _TypeCharacter;
		private readonly IntegerBase _IntegerBase;

		/// <summary>
    /// The literal value.
    /// </summary>
		public int Literal
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
    /// The type character on the literal.
    /// </summary>
		public TypeCharacter TypeCharacter
		{
			get
			{
				return _TypeCharacter;
			}
		}

		/// <summary>
    /// The integer base of the literal.
    /// </summary>
		public IntegerBase IntegerBase
		{
			get
			{
				return _IntegerBase;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for an integer literal.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="integerBase">The integer base of the literal.</param>
    /// <param name="typeCharacter">The type character on the literal.</param>
    /// <param name="span">The location of the parse tree.</param>
		public IntegerLiteralExpression(int literal, IntegerBase integerBase, TypeCharacter typeCharacter, Span span) : base(TreeType.IntegerLiteralExpression, span)
		{

			if (integerBase < IntegerBase.Decimal || integerBase > IntegerBase.Hexadecimal)
			{
				throw new ArgumentOutOfRangeException("integerBase");
			}

			if (typeCharacter != TypeCharacter.None && typeCharacter != TypeCharacter.IntegerSymbol && typeCharacter != TypeCharacter.IntegerChar && typeCharacter != TypeCharacter.ShortChar && typeCharacter != TypeCharacter.LongSymbol && typeCharacter != TypeCharacter.LongChar)


			{
				throw new ArgumentOutOfRangeException("typeCharacter");
			}

			_Literal = literal;
			_IntegerBase = integerBase;
			_TypeCharacter = typeCharacter;
		}
	}
}