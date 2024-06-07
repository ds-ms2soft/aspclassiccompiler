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
/// A character that denotes the type of something.
/// </summary>
	[Flags()]
	public enum TypeCharacter
	{
		/// <summary>No type character</summary>
		None = 0x0,

		/// <summary>The String symbol '$'.</summary>
		StringSymbol = 0x1,

		/// <summary>The Integer symbol '%'.</summary>
		IntegerSymbol = 0x2,

		/// <summary>The Long symbol '&amp;'.</summary>
		LongSymbol = 0x4,

		/// <summary>The Short character 'S'.</summary>
		ShortChar = 0x8,

		/// <summary>The Integer character 'I'.</summary>
		IntegerChar = 0x10,

		/// <summary>The Long character 'L'.</summary>
		LongChar = 0x20,

		/// <summary>The Single symbol '!'.</summary>
		SingleSymbol = 0x40,

		/// <summary>The Double symbol '#'.</summary>
		DoubleSymbol = 0x80,

		/// <summary>The Decimal symbol '@'.</summary>
		DecimalSymbol = 0x100,

		/// <summary>The Single character 'F'.</summary>
		SingleChar = 0x200,

		/// <summary>The Double character 'R'.</summary>
		DoubleChar = 0x400,

		/// <summary>The Decimal character 'D'.</summary>
		DecimalChar = 0x800,

		/// <summary>The unsigned Short characters 'US'.</summary>
    /// <remarks>New for Visual Basic 8.0.</remarks>
		UnsignedShortChar = 0x1000,

		/// <summary>The unsigned Integer characters 'UI'.</summary>
    /// <remarks>New for Visual Basic 8.0.</remarks>
		UnsignedIntegerChar = 0x2000,

		/// <summary>The unsigned Long characters 'UL'.</summary>
    /// <remarks>New for Visual Basic 8.0.</remarks>
		UnsignedLongChar = 0x4000,

		/// <summary>All type characters.</summary>
		All = 0x7FFF
	}
}