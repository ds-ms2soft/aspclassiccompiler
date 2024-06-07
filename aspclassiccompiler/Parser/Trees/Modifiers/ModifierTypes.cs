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
/// The type of a parse tree modifier.
/// </summary>
	[Flags()]
	public enum ModifierTypes
	{
		None = 0x0,
		Public = 0x1,
		Private = 0x2,
		Protected = 0x4,
		Friend = 0x8,
		AccessModifiers = Public | Private | Protected | Friend,
		Static = 0x10,
		Shared = 0x20,
		Shadows = 0x40,
		Overloads = 0x80,
		MustInherit = 0x100,
		NotInheritable = 0x200,
		Overrides = 0x400,
		NotOverridable = 0x800,
		Overridable = 0x1000,
		MustOverride = 0x2000,
		ReadOnly = 0x4000,
		WriteOnly = 0x8000,
		Dim = 0x10000,
		Const = 0x20000,
		Default = 0x40000,
		WithEvents = 0x80000,
		ByVal = 0x100000,
		ByRef = 0x200000,
		Optional = 0x400000,
		ParamArray = 0x800000,
		Partial = 0x1000000,
		Widening = 0x2000000,
		Narrowing = 0x4000000
	}
}