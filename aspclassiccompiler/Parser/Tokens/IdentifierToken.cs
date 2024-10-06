using System;
using System.Collections.Generic;

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
/// An identifier.
/// </summary>
	public sealed class IdentifierToken : Token
	{

		private struct Keyword
		{
			public readonly LanguageVersion Versions;
			public readonly LanguageVersion ReservedVersions;
			public readonly TokenType TokenType;

			public Keyword(LanguageVersion Versions, LanguageVersion ReservedVersions, TokenType TokenType)
			{
				this.Versions = Versions;
				this.ReservedVersions = ReservedVersions;
				this.TokenType = TokenType;
			}
		}

		private static Dictionary<string, Keyword> KeywordTable;

		private static void AddKeyword(Dictionary<string, Keyword> table, string name, Keyword keyword)
		{
			table.Add(name, keyword);
			table.Add(Scanner.MakeFullWidth(name), keyword);
		}

		// Returns the token type of the string.
		internal static TokenType TokenTypeFromString(string s, LanguageVersion Version, bool IncludeUnreserved)
		{
			if (KeywordTable is null)
			{
				var Table = new Dictionary<string, Keyword>(StringComparer.InvariantCultureIgnoreCase);

				// NOTE: These have to be in the same order as the enum!
				AddKeyword(Table, "AddHandler", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.AddHandler));
				AddKeyword(Table, "AddressOf", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.AddressOf));
				AddKeyword(Table, "Alias", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Alias));
				AddKeyword(Table, "And", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.And));
				AddKeyword(Table, "AndAlso", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.AndAlso));
				AddKeyword(Table, "Ansi", new Keyword(LanguageVersion.All, LanguageVersion.VisualBasic71, TokenType.Ansi));
				AddKeyword(Table, "As", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.As));
				AddKeyword(Table, "Assembly", new Keyword(LanguageVersion.All, LanguageVersion.VisualBasic71, TokenType.Assembly));
				AddKeyword(Table, "Auto", new Keyword(LanguageVersion.All, LanguageVersion.VisualBasic71, TokenType.Auto));
				AddKeyword(Table, "Binary", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Binary));
				AddKeyword(Table, "Boolean", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Boolean));
				AddKeyword(Table, "ByRef", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.ByRef));
				AddKeyword(Table, "Byte", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Byte));
				AddKeyword(Table, "ByVal", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.ByVal));
				AddKeyword(Table, "Call", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Call));
				AddKeyword(Table, "Case", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Case));
				AddKeyword(Table, "Catch", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Catch));
				// AddKeyword(Table, "CBool", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CBool))
				// AddKeyword(Table, "CByte", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CByte))
				// AddKeyword(Table, "CChar", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CChar))
				// AddKeyword(Table, "CDate", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CDate))
				// AddKeyword(Table, "CDbl", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CDbl))
				// AddKeyword(Table, "CDec", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CDec))
				AddKeyword(Table, "Char", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Char));
				// AddKeyword(Table, "CInt", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CInt))
				AddKeyword(Table, "Class", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Class));
				// AddKeyword(Table, "CLng", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CLng))
				// AddKeyword(Table, "CObj", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CObj))
				AddKeyword(Table, "Compare", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Compare));
				AddKeyword(Table, "Const", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Const));
				AddKeyword(Table, "Continue", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Continue));
				// AddKeyword(Table, "CSByte", New Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.CSByte))
				// AddKeyword(Table, "CShort", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CShort))
				// AddKeyword(Table, "CSng", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CSng))
				// AddKeyword(Table, "CStr", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CStr))
				// AddKeyword(Table, "CType", New Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.CType))
				// AddKeyword(Table, "CUInt", New Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.CUInt))
				// AddKeyword(Table, "CULng", New Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.CULng))
				// AddKeyword(Table, "CUShort", New Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.CUShort))
				AddKeyword(Table, "Custom", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.None, TokenType.Custom));
				AddKeyword(Table, "Date", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Date));
				AddKeyword(Table, "Decimal", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Decimal));
				AddKeyword(Table, "Declare", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Declare));
				AddKeyword(Table, "Default", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Default));
				AddKeyword(Table, "Delegate", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Delegate));
				AddKeyword(Table, "Dim", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Dim));
				AddKeyword(Table, "DirectCast", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.DirectCast));
				AddKeyword(Table, "Do", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Do));
				AddKeyword(Table, "Double", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Double));
				AddKeyword(Table, "Each", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Each));
				AddKeyword(Table, "Else", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Else));
				AddKeyword(Table, "ElseIf", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.ElseIf));
				AddKeyword(Table, "End", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.End));
				AddKeyword(Table, "EndIf", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.EndIf));
				AddKeyword(Table, "Enum", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Enum));
				AddKeyword(Table, "Erase", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Erase));
				AddKeyword(Table, "Error", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Error));
				AddKeyword(Table, "Event", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Event));
				AddKeyword(Table, "Exit", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Exit));
				AddKeyword(Table, "Explicit", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Explicit));
				AddKeyword(Table, "ExternalChecksum", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.None, TokenType.ExternalChecksum));
				AddKeyword(Table, "ExternalSource", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.ExternalSource));
				AddKeyword(Table, "False", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.False));
				AddKeyword(Table, "Finally", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Finally));
				AddKeyword(Table, "For", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.For));
				AddKeyword(Table, "Friend", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Friend));
				AddKeyword(Table, "Function", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Function));
				AddKeyword(Table, "Get", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Get));
				AddKeyword(Table, "GetType", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.GetType));
				AddKeyword(Table, "Global", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Global));
				AddKeyword(Table, "GoSub", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.GoSub));
				AddKeyword(Table, "GoTo", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.GoTo));
				AddKeyword(Table, "Handles", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Handles));
				AddKeyword(Table, "If", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.If));
				AddKeyword(Table, "Implements", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Implements));
				AddKeyword(Table, "Imports", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Imports));
				AddKeyword(Table, "In", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.In));
				AddKeyword(Table, "Inherits", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Inherits));
				AddKeyword(Table, "Integer", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Integer));
				AddKeyword(Table, "Interface", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Interface));
				AddKeyword(Table, "Is", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Is));
				AddKeyword(Table, "IsFalse", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.None, TokenType.IsFalse));
				AddKeyword(Table, "IsNot", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.IsNot));
				AddKeyword(Table, "IsTrue", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.None, TokenType.IsTrue));
				AddKeyword(Table, "Let", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Let));
				AddKeyword(Table, "Lib", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Lib));
				AddKeyword(Table, "Like", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Like));
				AddKeyword(Table, "Long", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Long));
				AddKeyword(Table, "Loop", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Loop));
				AddKeyword(Table, "Me", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Me));
				AddKeyword(Table, "Mid", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Mid));
				AddKeyword(Table, "Mod", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Mod));
				//Note  module is a keyword in VB.NET, but not in VBScript, and we use it as an identifier in VBScript everywhere
				//AddKeyword(Table, "Module", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Module));
				AddKeyword(Table, "MustInherit", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.MustInherit));
				AddKeyword(Table, "MustOverride", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.MustOverride));
				AddKeyword(Table, "MyBase", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.MyBase));
				AddKeyword(Table, "MyClass", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.MyClass));
				AddKeyword(Table, "Namespace", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Namespace));
				AddKeyword(Table, "Narrowing", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Narrowing));
				AddKeyword(Table, "New", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.New));
				AddKeyword(Table, "Next", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Next));
				AddKeyword(Table, "Not", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Not));
				AddKeyword(Table, "Nothing", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Nothing));
				AddKeyword(Table, "NotInheritable", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.NotInheritable));
				AddKeyword(Table, "NotOverridable", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.NotOverridable));
				AddKeyword(Table, "Object", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Object));
				AddKeyword(Table, "Of", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Of));
				AddKeyword(Table, "Off", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Off));
				AddKeyword(Table, "On", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.On));
				AddKeyword(Table, "Operator", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Operator));
				AddKeyword(Table, "Option", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Option));
				AddKeyword(Table, "Optional", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Optional));
				AddKeyword(Table, "Or", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Or));
				AddKeyword(Table, "OrElse", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.OrElse));
				AddKeyword(Table, "Overloads", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Overloads));
				AddKeyword(Table, "Overridable", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Overridable));
				AddKeyword(Table, "Overrides", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Overrides));
				AddKeyword(Table, "ParamArray", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.ParamArray));
				AddKeyword(Table, "Partial", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Partial));
				AddKeyword(Table, "Preserve", new Keyword(LanguageVersion.All, LanguageVersion.VisualBasic71, TokenType.Preserve));
				AddKeyword(Table, "Private", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Private));
				AddKeyword(Table, "Property", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Property));
				AddKeyword(Table, "Protected", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Protected));
				AddKeyword(Table, "Public", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Public));
				AddKeyword(Table, "RaiseEvent", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.RaiseEvent));
				AddKeyword(Table, "ReadOnly", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.ReadOnly));
				AddKeyword(Table, "ReDim", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.ReDim));
				AddKeyword(Table, "Region", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Region));
				AddKeyword(Table, "REM", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.REM));
				AddKeyword(Table, "RemoveHandler", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.RemoveHandler));
				AddKeyword(Table, "Resume", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Resume));
				AddKeyword(Table, "Return", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Return));
				AddKeyword(Table, "SByte", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.SByte));
				AddKeyword(Table, "Select", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Select));
				AddKeyword(Table, "Set", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Set));
				AddKeyword(Table, "Shadows", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Shadows));
				AddKeyword(Table, "Shared", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Shared));
				AddKeyword(Table, "Short", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Short));
				AddKeyword(Table, "Single", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Single));
				AddKeyword(Table, "Static", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Static));
				AddKeyword(Table, "Step", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Step));
				AddKeyword(Table, "Stop", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Stop));
				AddKeyword(Table, "Strict", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Strict));
				AddKeyword(Table, "String", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.String));
				AddKeyword(Table, "Structure", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Structure));
				AddKeyword(Table, "Sub", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Sub));
				AddKeyword(Table, "SyncLock", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.SyncLock));
				AddKeyword(Table, "Text", new Keyword(LanguageVersion.All, LanguageVersion.None, TokenType.Text));
				AddKeyword(Table, "Then", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Then));
				AddKeyword(Table, "Throw", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Throw));
				AddKeyword(Table, "To", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.To));
				AddKeyword(Table, "True", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.True));
				AddKeyword(Table, "Try", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Try));
				AddKeyword(Table, "TryCast", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.TryCast));
				AddKeyword(Table, "TypeOf", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.TypeOf));
				AddKeyword(Table, "UInteger", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.UInteger));
				AddKeyword(Table, "ULong", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.ULong));
				AddKeyword(Table, "UShort", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.UShort));
				AddKeyword(Table, "Using", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Using));
				AddKeyword(Table, "Unicode", new Keyword(LanguageVersion.All, LanguageVersion.VisualBasic71, TokenType.Unicode));
				AddKeyword(Table, "Until", new Keyword(LanguageVersion.All, LanguageVersion.VisualBasic71, TokenType.Until));
				AddKeyword(Table, "Variant", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Variant));
				AddKeyword(Table, "Wend", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Wend));
				AddKeyword(Table, "When", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.When));
				AddKeyword(Table, "While", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.While));
				AddKeyword(Table, "Widening", new Keyword(LanguageVersion.VisualBasic80, LanguageVersion.VisualBasic80, TokenType.Widening));
				AddKeyword(Table, "With", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.With));
				AddKeyword(Table, "WithEvents", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.WithEvents));
				AddKeyword(Table, "WriteOnly", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.WriteOnly));
				AddKeyword(Table, "Xor", new Keyword(LanguageVersion.All, LanguageVersion.All, TokenType.Xor));

				KeywordTable = Table;
			}

			if (KeywordTable.ContainsKey(s))
			{
				var Keyword = KeywordTable[s];

				if ((Keyword.Versions & Version) == Version && (IncludeUnreserved || (Keyword.ReservedVersions & Version) == Version))
				{
					return Keyword.TokenType;
				}
			}

			return TokenType.Identifier;
		}

		/// <summary>
    /// Determines if a token type is a keyword.
    /// </summary>
    /// <param name="type">The token type to check.</param>
    /// <returns>True if the token type is a keyword, False otherwise.</returns>
		public static bool IsKeyword(TokenType @type)
		{
			return type >= TokenType.AddHandler && type <= TokenType.Xor;
		}

		public override TokenType AsUnreservedKeyword()
		{
			return _UnreservedType;
		}

		private readonly string _Identifier;
		private readonly TokenType _UnreservedType;
		private readonly bool _Escaped;                  // Whether the identifier was escaped (i.e. [a])
		private readonly TypeCharacter _TypeCharacter;      // The type character that followed, if any

		/// <summary>
    /// The identifier name.
    /// </summary>
		public string Identifier
		{
			get
			{
				return _Identifier;
			}
		}

		/// <summary>
    /// Whether the identifier is escaped.
    /// </summary>
		public bool Escaped
		{
			get
			{
				return _Escaped;
			}
		}

		/// <summary>
    /// The type character of the identifier.
    /// </summary>
		public TypeCharacter TypeCharacter
		{
			get
			{
				return _TypeCharacter;
			}
		}

		/// <summary>
    /// Constructs a new identifier token.
    /// </summary>
    /// <param name="type">The token type of the identifier.</param>
    /// <param name="unreservedType">The unreserved token type of the identifier.</param>
    /// <param name="identifier">The text of the identifier</param>
    /// <param name="escaped">Whether the identifier is escaped.</param>
    /// <param name="typeCharacter">The type character of the identifier.</param>
    /// <param name="span">The location of the identifier.</param>
		public IdentifierToken(TokenType @type, TokenType unreservedType, string identifier, bool escaped, TypeCharacter typeCharacter, Span span) : base(type, span)
		{

			if (type != TokenType.Identifier && !IsKeyword(type))
			{
				throw new ArgumentOutOfRangeException("type");
			}

			if (unreservedType != TokenType.Identifier && !IsKeyword(unreservedType))
			{
				throw new ArgumentOutOfRangeException("unreservedType");
			}

			if (identifier is null || string.IsNullOrEmpty(identifier))
			{
				throw new ArgumentException("Identifier cannot be empty.", "identifier");
			}

			if (typeCharacter != TypeCharacter.None && typeCharacter != TypeCharacter.DecimalSymbol && typeCharacter != TypeCharacter.DoubleSymbol && typeCharacter != TypeCharacter.IntegerSymbol && typeCharacter != TypeCharacter.LongSymbol && typeCharacter != TypeCharacter.SingleSymbol && typeCharacter != TypeCharacter.StringSymbol)


			{
				throw new ArgumentOutOfRangeException("typeCharacter");
			}

			if (typeCharacter != TypeCharacter.None && escaped)
			{
				throw new ArgumentException("Escaped identifiers cannot have type characters.");
			}

			_UnreservedType = unreservedType;
			_Identifier = identifier;
			_Escaped = escaped;
			_TypeCharacter = typeCharacter;
		}
	}
}