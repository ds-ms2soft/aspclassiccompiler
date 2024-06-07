﻿
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
/// A token representing the end of the file.
/// </summary>
	public sealed class EndOfStreamToken : Token
	{

		/// <summary>
    /// Creates a new end-of-stream token.
    /// </summary>
    /// <param name="span">The location of the end of the stream.</param>
		public EndOfStreamToken(Span span) : base(TokenType.EndOfStream, span)
		{
		}
	}
}