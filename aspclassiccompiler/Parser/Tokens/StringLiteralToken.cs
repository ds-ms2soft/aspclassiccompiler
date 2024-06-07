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
/// A string literal.
/// </summary>
	public sealed class StringLiteralToken : Token
	{

		private readonly string _Literal;

		/// <summary>
    /// The value of the literal.
    /// </summary>
		public string Literal
		{
			get
			{
				return _Literal;
			}
		}

		/// <summary>
    /// Constructs a new string literal token.
    /// </summary>
    /// <param name="literal">The value of the literal.</param>
    /// <param name="span">The location of the literal.</param>
		public StringLiteralToken(string literal, Span span) : base(TokenType.StringLiteral, span)
		{

			if (literal is null)
			{
				throw new ArgumentNullException("literal");
			}

			_Literal = literal;
		}
	}
}