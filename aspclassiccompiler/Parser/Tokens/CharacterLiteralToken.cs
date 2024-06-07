
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
/// A character literal.
/// </summary>
	public sealed class CharacterLiteralToken : Token
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

		/// <summary>
    /// Constructs a new character literal token.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    /// <param name="span">The location of the literal.</param>
		public CharacterLiteralToken(char literal, Span span) : base(TokenType.CharacterLiteral, span)
		{
			_Literal = literal;
		}
	}
}