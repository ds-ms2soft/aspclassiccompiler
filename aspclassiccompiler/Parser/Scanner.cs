using System;
using System.Collections.Generic;
using System.Diagnostics;
// 
// Visual Basic .NET Parser
// 
// Copyright (C) 2005, Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// 

using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Dlrsoft.VBScript.Parser
{

	/// <summary>
	/// A lexical analyzer for Visual Basic .NET. It produces a stream of lexical tokens.
	/// </summary>
	public sealed class Scanner : IDisposable
	{

		// The text to be read. We use a TextReader here so that lexical analysis
		// can be done on strings as well as streams.
		private TextReader _Source;

		// For performance reasons, we cache the character when we peek ahead.
		private char _PeekCache;
		private bool _PeekCacheHasValue = false;

		// There are a few places where we're going to need to peek one character
		// ahead
		private char _PeekAheadCache;
		private bool _PeekAheadCacheHasValue = false;

		// Since we're only using a TextReader which has no position information,
		// we have to keep track of line/column information ourselves.
		private int _Index = 0;
		private int _Line = 1;
		private int _Column = 1;

		// A buffer of all the tokens we've returned so far
		private List<Token> _Tokens = new List<Token>();

		// Our current position in the buffer. -1 means before the beginning.
		private int _Position = -1;

		// Determine whether we have been disposed already or not
		private bool _Disposed = false;

		// How many columns a tab character should be treated as
		private int _TabSpaces = 4;

		// Version of the language to parse
		private LanguageVersion _Version = LanguageVersion.VisualBasic80;

		/// <summary>
    /// How many columns a tab character should be considered.
    /// </summary>
		public int TabSpaces
		{
			get
			{
				return _TabSpaces;
			}

			set
			{
				if (value < 1)
				{
					throw new ArgumentException("Tabs cannot represent less than one space.");
				}

				_TabSpaces = value;
			}
		}

		/// <summary>
    /// The version of Visual Basic this scanner operates on.
    /// </summary>
		public LanguageVersion Version
		{
			get
			{
				return _Version;
			}
		}

		/// <summary>
    /// Constructs a scanner for a string.
    /// </summary>
    /// <param name="source">The string to scan.</param>
		public Scanner(string source)
		{
			if (source is null)
				throw new ArgumentNullException("Source");
			_Source = new StringReader(source);
		}

		/// <summary>
    /// Constructs a scanner for a string.
    /// </summary>
    /// <param name="source">The string to scan.</param>
    /// <param name="version">The language version to parse.</param>
		public Scanner(string source, LanguageVersion version)
		{
			if (source is null)
				throw new ArgumentNullException("Source");
			if (version != LanguageVersion.VisualBasic71 && version != LanguageVersion.VisualBasic80)
			{
				throw new ArgumentOutOfRangeException("Version");
			}
			_Source = new StringReader(source);
			_Version = version;
		}

		/// <summary>
    /// Constructs a scanner for a stream.
    /// </summary>
    /// <param name="source">The stream to scan.</param>
		public Scanner(Stream source)
		{
			if (source is null)
				throw new ArgumentNullException("Source");
			_Source = new StreamReader(source);
		}

		/// <summary>
    /// Constructs a scanner for a stream.
    /// </summary>
    /// <param name="source">The stream to scan.</param>
    /// <param name="version">The language version to parse.</param>
		public Scanner(Stream source, LanguageVersion version)
		{
			if (source is null)
				throw new ArgumentNullException("Source");
			if (version != LanguageVersion.VisualBasic71 && version != LanguageVersion.VisualBasic80)
			{
				throw new ArgumentOutOfRangeException("Version");
			}
			_Source = new StreamReader(source);
			_Version = version;
		}

		/// <summary>
    /// Constructs a canner for a general TextReader.
    /// </summary>
    /// <param name="source">The TextReader to scan.</param>
		public Scanner(TextReader source)
		{
			if (source is null)
				throw new ArgumentNullException("Source");
			_Source = source;
		}

		/// <summary>
    /// Constructs a canner for a general TextReader.
    /// </summary>
    /// <param name="source">The TextReader to scan.</param>
    /// <param name="version">The language version to parse.</param>
		public Scanner(TextReader source, LanguageVersion version)
		{
			if (source is null)
				throw new ArgumentNullException("Source");
			if (version != LanguageVersion.VisualBasic71 && version != LanguageVersion.VisualBasic80)
			{
				throw new ArgumentOutOfRangeException("Version");
			}
			_Source = source;
			_Version = version;
		}

		/// <summary>
    /// Closes/disposes the scanner.
    /// </summary>
		public void Close()
		{
			if (!_Disposed)
			{
				_Disposed = true;
				_Source.Close();
			}
		}

		void IDisposable.Dispose() => Close();

		// Read a character
		private char ReadChar()
		{
			char c;

			if (_PeekCacheHasValue)
			{
				c = _PeekCache;
				_PeekCacheHasValue = false;

				if (_PeekAheadCacheHasValue)
				{
					_PeekCache = _PeekAheadCache;
					_PeekCacheHasValue = true;
					_PeekAheadCacheHasValue = false;
				}
			}
			else
			{
				Debug.Assert(!_PeekAheadCacheHasValue, "Cache incorrect!");
				c = Strings.ChrW(_Source.Read());
			}

			_Index += 1;
			if (Strings.AscW(c) == 0x9)
			{
				_Column += _TabSpaces;
			}
			else
			{
				_Column += 1;
			}

			return c;
		}

		// Peek ahead at the next character
		private char PeekChar()
		{
			if (!_PeekCacheHasValue)
			{
				_PeekCache = Strings.ChrW(_Source.Read());
				_PeekCacheHasValue = true;
			}

			return _PeekCache;
		}

		// Peek at the character past the next character
		private char PeekAheadChar()
		{
			if (!_PeekAheadCacheHasValue)
			{
				if (!_PeekCacheHasValue)
				{
					PeekChar();
				}

				_PeekAheadCache = Strings.ChrW(_Source.Read());
				_PeekAheadCacheHasValue = true;
			}

			return _PeekAheadCache;
		}

		// The current line/column position
		private Location CurrentLocation
		{
			get
			{
				return new Location(_Index, _Line, _Column);
			}
		}

		// Creates a span from the start location to the current location.
		private Span SpanFrom(Location start)
		{
			return new Span(start, CurrentLocation);
		}

		private static bool IsAlphaClass(UnicodeCategory c)
		{
			return c == UnicodeCategory.UppercaseLetter || c == UnicodeCategory.LowercaseLetter || c == UnicodeCategory.TitlecaseLetter || c == UnicodeCategory.OtherLetter || c == UnicodeCategory.ModifierLetter || c == UnicodeCategory.LetterNumber;




		}

		private static bool IsNumericClass(UnicodeCategory c)
		{
			return c == UnicodeCategory.DecimalDigitNumber;
		}

		private static bool IsUnderscoreClass(UnicodeCategory c)
		{
			return c == UnicodeCategory.ConnectorPunctuation;
		}

		private static bool IsSingleQuote(char c)
		{
			return c == '\'' || c == Strings.ChrW(0xFF07) || c == Strings.ChrW(0x2018) || c == Strings.ChrW(0x2019);
		}

		private static bool IsDoubleQuote(char c)
		{
			return c == '"' || c == Strings.ChrW(0xFF02) || c == Strings.ChrW(0x201C) || c == Strings.ChrW(0x201D);
		}

		private static bool IsDigit(char c)
		{
			return c >= '0' && c <= '9' || c >= Strings.ChrW(0xFF10) && c <= Strings.ChrW(0xFF19);
		}

		private static bool IsOctalDigit(char c)
		{
			return c >= '0' && c <= '7' || c >= Strings.ChrW(0xFF10) && c <= Strings.ChrW(0xFF17);
		}

		private static bool IsHexDigit(char c)
		{
			return IsDigit(c) || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F' || c >= Strings.ChrW(0xFF41) && c <= Strings.ChrW(0xFF46) || c >= Strings.ChrW(0xFF21) && c <= Strings.ChrW(0xFF26);

		}

		private static bool IsEquals(char c)
		{
			return c == '=' || c == Strings.ChrW(0xFF1D);
		}

		private static bool IsLessThan(char c)
		{
			return c == '<' || c == Strings.ChrW(0xFF1C);
		}

		private static bool IsGreaterThan(char c)
		{
			return c == '>' || c == Strings.ChrW(0xFF1E);
		}

		private static bool IsAmpersand(char c)
		{
			return c == '&' || c == Strings.ChrW(0xFF06);
		}

		private static bool IsUnderscore(char c)
		{
			return IsUnderscoreClass(char.GetUnicodeCategory(c));
		}

		private static bool IsHexDesignator(char c)
		{
			return c == 'H' || c == 'h' || c == Strings.ChrW(0xFF48) || c == Strings.ChrW(0xFF28);
		}

		private static bool IsOctalDesignator(char c)
		{
			return c == 'O' || c == 'o' || c == Strings.ChrW(0xFF2F) || c == Strings.ChrW(0xFF4F);
		}

		private static bool IsPeriod(char c)
		{
			return c == '.' || c == Strings.ChrW(0xFF0E);
		}

		private static bool IsExponentDesignator(char c)
		{
			return c == 'e' || c == 'E' || c == Strings.ChrW(0xFF45) || c == Strings.ChrW(0xFF25);
		}

		private static bool IsPlus(char c)
		{
			return c == '+' || c == Strings.ChrW(0xFF0B);
		}

		private static bool IsMinus(char c)
		{
			return c == '-' || c == Strings.ChrW(0xFF0D);
		}

		private static bool IsForwardSlash(char c)
		{
			return c == '/' || c == Strings.ChrW(0xFF0F);
		}

		private static bool IsColon(char c)
		{
			return c == ':' || c == Strings.ChrW(0xFF1A);
		}

		private static bool IsPound(char c)
		{
			return c == '#' || c == Strings.ChrW(0xFF03);
		}

		private static bool IsA(char c)
		{
			return c == 'a' || c == Strings.ChrW(0xFF41) || c == 'A' || c == Strings.ChrW(0xFF21);
		}

		private static bool IsP(char c)
		{
			return c == 'p' || c == Strings.ChrW(0xFF50) || c == 'P' || c == Strings.ChrW(0xFF30);
		}

		private static bool IsM(char c)
		{
			return c == 'm' || c == Strings.ChrW(0xFF4D) || c == 'M' || c == Strings.ChrW(0xFF2D);
		}

		private static bool IsCharDesignator(char c)
		{
			return c == 'c' || c == 'C' || c == Strings.ChrW(0xFF43) || c == Strings.ChrW(0xFF23);
		}

		private static bool IsLeftBracket(char c)
		{
			return c == '[' || c == Strings.ChrW(0xFF3B);
		}

		private static bool IsRightBracket(char c)
		{
			return c == ']' || c == Strings.ChrW(0xFF3D);
		}

		private static bool IsUnsignedTypeChar(char c)
		{
			return c == 'u' || c == 'U' || c == Strings.ChrW(0xFF35) || c == Strings.ChrW(0xFF55);
		}

		private static bool IsIdentifier(char c)
		{
			var CharClass = char.GetUnicodeCategory(c);

			return IsAlphaClass(CharClass) || IsNumericClass(CharClass) || CharClass == UnicodeCategory.SpacingCombiningMark || CharClass == UnicodeCategory.NonSpacingMark || CharClass == UnicodeCategory.Format || IsUnderscoreClass(CharClass);





		}

		internal static char MakeHalfWidth(char c)
		{
			if (c < Strings.ChrW(0xFF01) || c > Strings.ChrW(0xFF5E))
			{
				return c;
			}
			else
			{
				return Strings.ChrW(Strings.AscW(c) - 0xFF00 + 0x20);
			}
		}

		internal static char MakeFullWidth(char c)
		{
			if (c < '!' || c > '~')
			{
				return c;
			}
			else
			{
				return Strings.ChrW(Strings.AscW(c) + 0xFF00 - 0x20);
			}
		}

		internal static string MakeFullWidth(string s)
		{
			var Builder = new StringBuilder(s);

			for (int Index = 0, loopTo = Builder.Length - 1; Index <= loopTo; Index++)
				Builder[Index] = MakeFullWidth(Builder[Index]);

			return Builder.ToString();
		}

		private Dictionary<string, TypeCharacter> _ScanPossibleTypeCharacter_TypeCharacterTable = default;

		// 
		// Scan functions
		// 
		// Each function assumes that the reader is positioned at the beginning of
		// the token. At the end, the function will have read through the entire
		// token. If an error occurs, the function may attempt to do error recovery.
		// 

		private TypeCharacter ScanPossibleTypeCharacter(TypeCharacter ValidTypeCharacters)
		{
			char TypeChar = PeekChar();
			string TypeString;

			if (_ScanPossibleTypeCharacter_TypeCharacterTable is null)
			{
				var Table = new Dictionary<string, TypeCharacter>(StringComparer.InvariantCultureIgnoreCase);
				// NOTE: These have to be in the same order as the enum!
				string[] TypeCharacters = new string[] { "$", "%", "&", "S", "I", "L", "!", "#", "@", "F", "R", "D", "US", "UI", "UL" };
				var TypeCharacter = VBScript.Parser.TypeCharacter.StringSymbol;

				for (int Index = 0, loopTo = TypeCharacters.Length - 1; Index <= loopTo; Index++)
				{
					Table.Add(TypeCharacters[Index], TypeCharacter);
					Table.Add(MakeFullWidth(TypeCharacters[Index]), TypeCharacter);

					TypeCharacter = (TypeCharacter)((int)TypeCharacter << 1);
				}

				_ScanPossibleTypeCharacter_TypeCharacterTable = Table;
			}

			if (IsUnsignedTypeChar(TypeChar) && _Version > LanguageVersion.VisualBasic71)
			{
				// At the point at which we've seen a "U", we don't know if it's going to
				// be a valid type character or just something invalid.
				TypeString = Conversions.ToString(TypeChar) + PeekAheadChar();
			}
			else
			{
				TypeString = Conversions.ToString(TypeChar);
			}

			if (_ScanPossibleTypeCharacter_TypeCharacterTable.ContainsKey(TypeString))
			{
				var TypeCharacter = _ScanPossibleTypeCharacter_TypeCharacterTable[TypeString];

				if ((TypeCharacter & ValidTypeCharacters) != 0)
				{
					// A bang (!) is a type character unless it is followed by a legal identifier start.
					if (TypeCharacter == VBScript.Parser.TypeCharacter.SingleSymbol && CanStartIdentifier(PeekAheadChar()))
					{
						return VBScript.Parser.TypeCharacter.None;
					}

					ReadChar();

					if (IsUnsignedTypeChar(TypeChar))
					{
						ReadChar();
					}

					return TypeCharacter;
				}
			}

			return TypeCharacter.None;
		}

		private PunctuatorToken ScanPossibleMultiCharacterPunctuator(char leadingCharacter, Location start)
		{
			char NextChar = PeekChar();
			TokenType Punctuator;
			string PunctuatorString = Conversions.ToString(leadingCharacter);

			Debug.Assert(PunctuatorToken.TokenTypeFromString(Conversions.ToString(leadingCharacter)) != TokenType.None);

			if (IsEquals(NextChar) || IsLessThan(NextChar) || IsGreaterThan(NextChar))
			{
				PunctuatorString += Conversions.ToString(NextChar);
				Punctuator = PunctuatorToken.TokenTypeFromString(PunctuatorString);

				if (Punctuator != TokenType.None)
				{
					ReadChar();

					if ((Punctuator == TokenType.LessThanLessThan || Punctuator == TokenType.GreaterThanGreaterThan) && IsEquals(PeekChar()))

					{
						PunctuatorString += Conversions.ToString(ReadChar());
						Punctuator = PunctuatorToken.TokenTypeFromString(PunctuatorString);
					}

					return new PunctuatorToken(Punctuator, SpanFrom(start));
				}
			}

			Punctuator = PunctuatorToken.TokenTypeFromString(Conversions.ToString(leadingCharacter));
			return new PunctuatorToken(Punctuator, SpanFrom(start));
		}

		private Token ScanNumericLiteral()
		{
			var Start = CurrentLocation;
			var Literal = new StringBuilder();
			var Base = IntegerBase.Decimal;
			var TypeCharacter = VBScript.Parser.TypeCharacter.None;

			Debug.Assert(CanStartNumericLiteral());

			if (IsAmpersand(PeekChar()))
			{
				Literal.Append(MakeHalfWidth(ReadChar()));

				if (IsHexDesignator(PeekChar()))
				{
					Literal.Append(MakeHalfWidth(ReadChar()));
					Base = IntegerBase.Hexadecimal;

					while (IsHexDigit(PeekChar()))
						Literal.Append(MakeHalfWidth(ReadChar()));
				}
				else if (IsOctalDesignator(PeekChar()))
				{
					Literal.Append(MakeHalfWidth(ReadChar()));
					Base = IntegerBase.Octal;

					while (IsOctalDigit(PeekChar()))
						Literal.Append(MakeHalfWidth(ReadChar()));
				}
				else if (IsOctalDigit(PeekChar())) // VbScript Octal is like &123456&
				{
					Base = IntegerBase.Octal;
					Literal.Append('O'); // VB.net Octal starts with &O

					while (IsOctalDigit(PeekChar()))
						Literal.Append(MakeHalfWidth(ReadChar()));

					if (IsAmpersand(PeekChar()))
					{
						ReadChar(); // Ignored the last '&'
					}
				}
				else
				{
					return ScanPossibleMultiCharacterPunctuator('&', Start);
				}

				if (Literal.Length > 2)
				{
					const TypeCharacter ValidTypeChars = VBScript.Parser.TypeCharacter.ShortChar | VBScript.Parser.TypeCharacter.UnsignedShortChar | VBScript.Parser.TypeCharacter.IntegerSymbol | VBScript.Parser.TypeCharacter.IntegerChar | VBScript.Parser.TypeCharacter.UnsignedIntegerChar | VBScript.Parser.TypeCharacter.LongSymbol | VBScript.Parser.TypeCharacter.LongChar | VBScript.Parser.TypeCharacter.UnsignedLongChar;



					TypeCharacter = ScanPossibleTypeCharacter(ValidTypeChars);

					try
					{
						switch (TypeCharacter)
						{
							case VBScript.Parser.TypeCharacter.ShortChar:
								{
									long Value = Conversions.ToLong(Literal.ToString());

									if (Value <= 0xFFFFL)
									{
										if (Value > 0x7FFFL)
										{
											Value = -(0x10000L - Value);
										}

										if (Value >= short.MinValue && Value <= short.MaxValue)
										{
											return new IntegerLiteralToken((short)Value, Base, TypeCharacter, SpanFrom(Start));
										}
									}

									break;
								}
							// Fall through

							case VBScript.Parser.TypeCharacter.UnsignedShortChar:
								{
									ulong Value = Conversions.ToULong(Literal.ToString());

									if (Value <= 65535m)
									{
										if (Value >= ushort.MinValue && Value <= ushort.MaxValue)
										{
											return new UnsignedIntegerLiteralToken((ushort)Value, Base, TypeCharacter, SpanFrom(Start));
										}
									}

									break;
								}
							// Fall through

							case VBScript.Parser.TypeCharacter.IntegerSymbol:
							case VBScript.Parser.TypeCharacter.IntegerChar:
								{
									long Value = Conversions.ToLong(Literal.ToString());

									if (Value <= 0xFFFFFFFFL)
									{
										if (Value > 0x7FFFFFFFL)
										{
											Value = -(0x100000000L - Value);
										}

										if (Value >= int.MinValue && Value <= int.MaxValue)
										{
											return new IntegerLiteralToken((int)Value, Base, TypeCharacter, SpanFrom(Start));
										}
									}

									break;
								}
							// Fall through

							case VBScript.Parser.TypeCharacter.UnsignedIntegerChar:
								{
									ulong Value = Conversions.ToULong(Literal.ToString());

									if (Value <= 4294967295m)
									{
										if (Value >= uint.MinValue && Value <= uint.MaxValue)
										{
											return new UnsignedIntegerLiteralToken((uint)Value, Base, TypeCharacter, SpanFrom(Start));
										}
									}

									break;
								}
							// Fall through

							case VBScript.Parser.TypeCharacter.LongSymbol:
							case VBScript.Parser.TypeCharacter.LongChar:
								{
									return new IntegerLiteralToken(ParseInt(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
								}

							case VBScript.Parser.TypeCharacter.UnsignedLongChar:
								{
									return new UnsignedIntegerLiteralToken(Conversions.ToULong(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
								}

							default:
								{
									TypeCharacter = VBScript.Parser.TypeCharacter.None;
									return new IntegerLiteralToken(ParseInt(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
								}
						}
					}
					catch (OverflowException ex)
					{
						return new ErrorToken(SyntaxErrorType.InvalidIntegerLiteral, SpanFrom(Start));
					}
					catch (InvalidCastException ex)
					{
						return new ErrorToken(SyntaxErrorType.InvalidIntegerLiteral, SpanFrom(Start));
					}
				}

				return new ErrorToken(SyntaxErrorType.InvalidIntegerLiteral, SpanFrom(Start));
			}

			while (IsDigit(PeekChar()))
				Literal.Append(MakeHalfWidth(ReadChar()));

			if (IsPeriod(PeekChar()) || IsExponentDesignator(PeekChar()))
			{
				var ErrorType = SyntaxErrorType.None;
				const TypeCharacter ValidTypeChars = VBScript.Parser.TypeCharacter.DecimalChar | VBScript.Parser.TypeCharacter.DecimalSymbol | VBScript.Parser.TypeCharacter.SingleChar | VBScript.Parser.TypeCharacter.SingleSymbol | VBScript.Parser.TypeCharacter.DoubleChar | VBScript.Parser.TypeCharacter.DoubleSymbol;



				if (IsPeriod(PeekChar()))
				{
					Literal.Append(MakeHalfWidth(ReadChar()));

					if (!IsDigit(PeekChar()) & Literal.Length == 1)
					{
						return new PunctuatorToken(TokenType.Period, SpanFrom(Start));
					}

					while (IsDigit(PeekChar()))
						Literal.Append(MakeHalfWidth(ReadChar()));
				}

				if (IsExponentDesignator(PeekChar()))
				{
					Literal.Append(MakeHalfWidth(ReadChar()));

					if (IsPlus(PeekChar()) || IsMinus(PeekChar()))
					{
						Literal.Append(MakeHalfWidth(ReadChar()));
					}

					if (!IsDigit(PeekChar()))
					{
						return new ErrorToken(SyntaxErrorType.InvalidFloatingPointLiteral, SpanFrom(Start));
					}

					while (IsDigit(PeekChar()))
						Literal.Append(MakeHalfWidth(ReadChar()));
				}

				TypeCharacter = ScanPossibleTypeCharacter(ValidTypeChars);

				try
				{
					switch (TypeCharacter)
					{
						case VBScript.Parser.TypeCharacter.DecimalChar:
						case VBScript.Parser.TypeCharacter.DecimalSymbol:
							{
								ErrorType = SyntaxErrorType.InvalidDecimalLiteral;
								return new DecimalLiteralToken(Conversions.ToDecimal(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.SingleSymbol:
						case VBScript.Parser.TypeCharacter.SingleChar:
							{
								ErrorType = SyntaxErrorType.InvalidFloatingPointLiteral;
								return new FloatingPointLiteralToken((double)Conversions.ToSingle(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.DoubleSymbol:
						case VBScript.Parser.TypeCharacter.DoubleChar:
							{
								ErrorType = SyntaxErrorType.InvalidFloatingPointLiteral;
								return new FloatingPointLiteralToken(Conversions.ToDouble(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}

						default:
							{
								ErrorType = SyntaxErrorType.InvalidFloatingPointLiteral;
								TypeCharacter = VBScript.Parser.TypeCharacter.None;
								return new FloatingPointLiteralToken(Conversions.ToDouble(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}
					}
				}
				catch (OverflowException ex)
				{
					return new ErrorToken(ErrorType, SpanFrom(Start));
				}
				catch (InvalidCastException ex)
				{
					return new ErrorToken(ErrorType, SpanFrom(Start));
				}
			}
			else
			{
				var ErrorType = SyntaxErrorType.None;
				const TypeCharacter ValidTypeChars = VBScript.Parser.TypeCharacter.ShortChar | VBScript.Parser.TypeCharacter.IntegerSymbol | VBScript.Parser.TypeCharacter.IntegerChar | VBScript.Parser.TypeCharacter.LongSymbol | VBScript.Parser.TypeCharacter.LongChar | VBScript.Parser.TypeCharacter.DecimalSymbol | VBScript.Parser.TypeCharacter.DecimalChar | VBScript.Parser.TypeCharacter.SingleSymbol | VBScript.Parser.TypeCharacter.SingleChar | VBScript.Parser.TypeCharacter.DoubleSymbol | VBScript.Parser.TypeCharacter.DoubleChar | VBScript.Parser.TypeCharacter.UnsignedShortChar | VBScript.Parser.TypeCharacter.UnsignedIntegerChar | VBScript.Parser.TypeCharacter.UnsignedLongChar;








				TypeCharacter = ScanPossibleTypeCharacter(ValidTypeChars);

				try
				{
					switch (TypeCharacter)
					{
						case VBScript.Parser.TypeCharacter.ShortChar:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new IntegerLiteralToken(Conversions.ToShort(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.UnsignedShortChar:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new UnsignedIntegerLiteralToken(Conversions.ToUShort(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.IntegerSymbol:
						case VBScript.Parser.TypeCharacter.IntegerChar:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new IntegerLiteralToken(Conversions.ToInteger(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.UnsignedIntegerChar:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new UnsignedIntegerLiteralToken(Conversions.ToUInteger(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.LongSymbol:
						case VBScript.Parser.TypeCharacter.LongChar:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new IntegerLiteralToken(Conversions.ToInteger(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.UnsignedLongChar:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new UnsignedIntegerLiteralToken(Conversions.ToULong(Literal.ToString()), Base, TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.DecimalChar:
						case VBScript.Parser.TypeCharacter.DecimalSymbol:
							{
								ErrorType = SyntaxErrorType.InvalidDecimalLiteral;
								return new DecimalLiteralToken(Conversions.ToDecimal(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.SingleSymbol:
						case VBScript.Parser.TypeCharacter.SingleChar:
							{
								ErrorType = SyntaxErrorType.InvalidFloatingPointLiteral;
								return new FloatingPointLiteralToken((double)Conversions.ToSingle(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}

						case VBScript.Parser.TypeCharacter.DoubleSymbol:
						case VBScript.Parser.TypeCharacter.DoubleChar:
							{
								ErrorType = SyntaxErrorType.InvalidFloatingPointLiteral;
								return new FloatingPointLiteralToken(Conversions.ToDouble(Literal.ToString()), TypeCharacter, SpanFrom(Start));
							}

						default:
							{
								ErrorType = SyntaxErrorType.InvalidIntegerLiteral;
								return new IntegerLiteralToken(Conversions.ToInteger(Literal.ToString()), Base, VBScript.Parser.TypeCharacter.None, SpanFrom(Start));
							}
					}
				}
				catch (OverflowException ex)
				{
					return new ErrorToken(ErrorType, SpanFrom(Start));
				}
				catch (InvalidCastException ex)
				{
					return new ErrorToken(ErrorType, SpanFrom(Start));
				}
			}
		}

		private bool CanStartNumericLiteral()
		{
			return IsPeriod(PeekChar()) || IsAmpersand(PeekChar()) || IsDigit(PeekChar());
		}

		private long ReadIntegerLiteral()
		{
			long Value = 0L;

			while (IsDigit(PeekChar()))
			{
				char c = MakeHalfWidth(ReadChar());
				Value *= 10L;
				Value += Strings.AscW(c) - Strings.AscW('0');
			}

			return Value;
		}

		private Token ScanDateLiteral()
		{
			var Start = CurrentLocation;
			Location PossibleEnd;
			int Month = 0;
			int Day = 0;
			int Year = 0;
			int Hour = 0;
			int Minute = 0;
			int Second = 0;
			bool HaveDateValue = false;
			bool HaveTimeValue = false;
			long Value;

			Debug.Assert(CanStartDateLiteral());

			ReadChar();
			PossibleEnd = CurrentLocation;
			EatWhitespace();

			// Distinguish between date literals and the # punctuator
			if (!IsDigit(PeekChar()))
			{
				return new PunctuatorToken(TokenType.Pound, new Span(Start, PossibleEnd));
			}

			Value = ReadIntegerLiteral();

			// LC in VBScript, it is legal to have something like #08 / 27 / 97 5:11:42pm#
			EatWhitespace();

			if (IsForwardSlash(PeekChar()) || IsMinus(PeekChar()))
			{
				char Separator = ReadChar();
				Location YearStart;

				HaveDateValue = true;
				if (Value < 1L || Value > 12L)
					return Invalid();
				Month = (int)Value;

				// LC
				EatWhitespace();

				if (!IsDigit(PeekChar()))
					return Invalid();
				Value = ReadIntegerLiteral();
				if (Value < 1L || Value > 31L)
					return Invalid();
				Day = (int)Value;

				// LC
				EatWhitespace();

				if (PeekChar() != Separator)
					return Invalid();
				ReadChar();

				// LC
				EatWhitespace();

				if (!IsDigit(PeekChar()))
					return Invalid();
				YearStart = CurrentLocation;
				Value = ReadIntegerLiteral();
				if (Value < 1L || Value > 9999L)
					return Invalid();
				// Years less than 1000 have to be four digits long to avoid y2k confusion
				// If Value < 1000 And CurrentLocation.Column - YearStart.Column <> 4 Then GoTo Invalid

				Year = (int)Value;

				// LC 2 digit year conversion
				if (CurrentLocation.Column - YearStart.Column == 2)
				{
					if (Year > 30)
					{
						Year += 2000;
					}
					else
					{
						Year += 1900;
					}
				}
				else if (CurrentLocation.Column - YearStart.Column != 4)
				{
					return Invalid();
				}

				if (Day > DateTime.DaysInMonth(Year, Month))
					return Invalid();

				EatWhitespace();
				if (IsDigit(PeekChar()))
				{
					Value = ReadIntegerLiteral();

					if (!IsColon(PeekChar()))
						return Invalid();
				}
			}

			if (IsColon(PeekChar()))
			{
				ReadChar();
				HaveTimeValue = true;
				if (Value < 0L || Value > 23L)
					return Invalid();
				Hour = (int)Value;

				if (!IsDigit(PeekChar()))
					return Invalid();
				Value = ReadIntegerLiteral();
				if (Value < 0L || Value > 59L)
					return Invalid();
				Minute = (int)Value;

				if (IsColon(PeekChar()))
				{
					ReadChar();
					if (!IsDigit(PeekChar()))
						return Invalid();
					Value = ReadIntegerLiteral();
					if (Value < 0L || Value > 59L)
						return Invalid();
					Second = (int)Value;
				}

				EatWhitespace();

				if (IsA(PeekChar()))
				{
					ReadChar();

					if (IsM(PeekChar()))
					{
						ReadChar();
						if (Hour < 1 || Hour > 12)
						{
							return Invalid();
						}
					}
					else
					{
						return Invalid();
					}
				}
				else if (IsP(PeekChar()))
				{
					ReadChar();

					if (IsM(PeekChar()))
					{
						ReadChar();
						if (Hour < 1 || Hour > 12)
						{
							return Invalid();
						}

						Hour += 12;

						if (Hour == 24)
						{
							Hour = 12;
						}
					}
					else
					{
						return Invalid();
					}
				}
			}

			if (!IsPound(PeekChar()))
			{
				return Invalid();
			}
			else
			{
				ReadChar();
			}

			if (!HaveTimeValue && !HaveDateValue)
			{
				return Invalid();
			}

			if (HaveDateValue)
			{
				if (HaveTimeValue)
				{
					return new DateLiteralToken(new DateTime(Year, Month, Day, Hour, Minute, Second), SpanFrom(Start));
				}
				else
				{
					return new DateLiteralToken(new DateTime(Year, Month, Day), SpanFrom(Start));
				}
			}
			else
			{
				return new DateLiteralToken(new DateTime(1, 1, 1, Hour, Minute, Second), SpanFrom(Start));
			}

			ErrorToken Invalid()
			{
				while (!IsPound(PeekChar()) && !CanStartLineTerminator())
					ReadChar();

				if (IsPound(PeekChar()))
				{
					ReadChar();
				}

				return new ErrorToken(SyntaxErrorType.InvalidDateLiteral, SpanFrom(Start));
			}
		}

		private bool CanStartDateLiteral()
		{
			return IsPound(PeekChar());
		}

		// Actually, this scans string and char literals
		private Token ScanStringLiteral()
		{
			var Start = CurrentLocation;
			var s = new StringBuilder();

			Debug.Assert(CanStartStringLiteral());

			ReadChar();

		ContinueScan:
			;

			while (!IsDoubleQuote(PeekChar()) && !CanStartLineTerminator())
				s.Append(ReadChar());

			if (IsDoubleQuote(PeekChar()))
			{
				ReadChar();

				if (IsDoubleQuote(PeekChar()))
				{
					ReadChar();
					// NOTE: We take what could be a full-width double quote and make it a half-width.
					s.Append('"');
					goto ContinueScan;
				}
			}
			else
			{
				return new ErrorToken(SyntaxErrorType.InvalidStringLiteral, SpanFrom(Start));
			}

			if (IsCharDesignator(PeekChar()))
			{
				ReadChar();

				if (s.Length != 1)
				{
					return new ErrorToken(SyntaxErrorType.InvalidCharacterLiteral, SpanFrom(Start));
				}
				else
				{
					return new CharacterLiteralToken(s[0], SpanFrom(Start));
				}
			}
			else
			{
				return new StringLiteralToken(s.ToString(), SpanFrom(Start));
			}
		}

		private bool CanStartStringLiteral()
		{
			return IsDoubleQuote(PeekChar());
		}

		private Token ScanIdentifier()
		{
			var Start = CurrentLocation;
			bool Escaped = false;
			var TypeCharacter = VBScript.Parser.TypeCharacter.None;
			string Identifier;
			var s = new StringBuilder();
			var Type = TokenType.Identifier;
			var UnreservedType = TokenType.Identifier;

			Debug.Assert(CanStartIdentifier());

			if (IsLeftBracket(PeekChar()))
			{
				Escaped = true;
				ReadChar();

				if (!CanStartNonEscapedIdentifier())
				{
					while (!IsRightBracket(PeekChar()) && !CanStartLineTerminator())
						ReadChar();

					if (IsRightBracket(PeekChar()))
					{
						ReadChar();
					}

					return new ErrorToken(SyntaxErrorType.InvalidEscapedIdentifier, SpanFrom(Start));
				}
			}

			s.Append(ReadChar());

			if (IsUnderscore(s[0]) && !IsIdentifier(PeekChar()))
			{
				var End = CurrentLocation;

				EatWhitespace();

				// This is a line continuation
				if (CanStartLineTerminator())
				{
					ScanLineTerminator(false);
					return null;
				}
				else
				{
					return new ErrorToken(SyntaxErrorType.InvalidIdentifier, new Span(Start, End));
				}
			}

			while (IsIdentifier(PeekChar()))
				// NOTE: We do not convert full-width to half-width here!
				s.Append(ReadChar());

			Identifier = s.ToString();

			if (Escaped)
			{
				if (IsRightBracket(PeekChar()))
				{
					ReadChar();
				}
				else
				{
					while (!IsRightBracket(PeekChar()) && !CanStartLineTerminator())
						ReadChar();

					if (IsRightBracket(PeekChar()))
					{
						ReadChar();
					}

					return new ErrorToken(SyntaxErrorType.InvalidEscapedIdentifier, SpanFrom(Start));
				}
			}
			else
			{
				// Const ValidTypeChars As TypeCharacter = _
				// TypeCharacter.DecimalSymbol Or TypeCharacter.DoubleSymbol Or _
				// TypeCharacter.IntegerSymbol Or TypeCharacter.LongSymbol Or _
				// TypeCharacter.SingleSymbol Or TypeCharacter.StringSymbol
				Type = IdentifierToken.TokenTypeFromString(Identifier, _Version, false);

				if (Type == TokenType.REM)
				{
					return ScanComment(Start);
				}

				UnreservedType = IdentifierToken.TokenTypeFromString(Identifier, _Version, true);
				// LC In VBScript, we do not allow type character after the identifier
				// TypeCharacter = ScanPossibleTypeCharacter(ValidTypeChars)

				if (Type != TokenType.Identifier && TypeCharacter != VBScript.Parser.TypeCharacter.None)
				{
					// In VB 8.0, keywords with a type character are considered identifiers.
					if (_Version > LanguageVersion.VisualBasic71)
					{
						Type = TokenType.Identifier;
					}
					else
					{
						return new ErrorToken(SyntaxErrorType.InvalidTypeCharacterOnKeyword, SpanFrom(Start));
					}
				}
			}

			return new IdentifierToken(Type, UnreservedType, Identifier, Escaped, TypeCharacter, SpanFrom(Start));
		}

		private bool CanStartNonEscapedIdentifier()
		{
			return CanStartNonEscapedIdentifier(PeekChar());
		}

		private static bool CanStartNonEscapedIdentifier(char c)
		{
			var CharClass = char.GetUnicodeCategory(c);

			return IsAlphaClass(CharClass) || IsUnderscoreClass(CharClass);
		}

		private bool CanStartIdentifier()
		{
			return CanStartIdentifier(PeekChar());
		}

		private static bool CanStartIdentifier(char c)
		{
			return IsLeftBracket(c) || CanStartNonEscapedIdentifier(c);
		}

		// Scan a comment that begins with a tick mark
		private CommentToken ScanComment()
		{
			var s = new StringBuilder();
			var Start = CurrentLocation;

			Debug.Assert(CanStartComment());
			ReadChar();

			while (!CanStartLineTerminator())
				// NOTE: We don't convert full-width to half-width here.
				s.Append(ReadChar());

			return new CommentToken(s.ToString(), false, SpanFrom(Start));
		}

		// Scan a comment that begins with REM.
		private CommentToken ScanComment(Location start)
		{
			var s = new StringBuilder();

			while (!CanStartLineTerminator())
				// NOTE: We don't convert full-width to half-width here.
				s.Append(ReadChar());

			return new CommentToken(s.ToString(), true, SpanFrom(start));
		}

		// We only check for tick mark here.
		private bool CanStartComment()
		{
			return IsSingleQuote(PeekChar());
		}

		private Token ScanLineTerminator(bool produceToken = true)
		{
			var Start = CurrentLocation;
			Token Token = null;

			Debug.Assert(CanStartLineTerminator());

			if (PeekChar() == Strings.ChrW(0xFFFF))
			{
				Token = new EndOfStreamToken(SpanFrom(Start));
			}
			else
			{
				if (ReadChar() == '\r')
				{
					// A CR/LF pair is a legal line terminator
					if (PeekChar() == '\n')
					{
						ReadChar();
					}
				}

				if (produceToken)
				{
					Token = new LineTerminatorToken(SpanFrom(Start));
				}
				_Line += 1;
				_Column = 1;
			}

			return Token;
		}

		private bool CanStartLineTerminator()
		{
			char c = PeekChar();

			return c == '\r' || c == '\n' || c == Strings.ChrW(0x2028) || c == Strings.ChrW(0x2029) || c == Strings.ChrW(0xFFFF);

		}

		private bool EatWhitespace()
		{
			bool EatWhitespaceRet = default;
			char c = PeekChar();
			EatWhitespaceRet = false;
			while (c == '\t' || char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator)
			{
				ReadChar();
				EatWhitespaceRet = true;
				c = PeekChar();
			}

			return EatWhitespaceRet;
		}

		private Token Read(bool advance)
		{
			Token TokenRead;
			Token CurrentToken;

			if (_Position > -1)
			{
				CurrentToken = _Tokens[_Position];
			}
			else
			{
				CurrentToken = null;
			}

			// If we've reached the end of the stream, just return the end of stream token again
			if (CurrentToken is not null && CurrentToken.Type == TokenType.EndOfStream)
			{
				return CurrentToken;
			}

			// If we haven't read a token yet, or if we've reached the end of the tokens that we've read
			// so far, then we need to read a fresh token in.
			if (_Position == _Tokens.Count - 1)
			{
			ContinueLine:
				;

				EatWhitespace();

				if (CanStartLineTerminator())
				{
					TokenRead = ScanLineTerminator();
				}
				else if (CanStartComment())
				{
					TokenRead = ScanComment();
				}
				else if (CanStartIdentifier())
				{
					var Token = ScanIdentifier();

					if (Token is null)
					{
						// This was a line continuation, so skip and keep going
						goto ContinueLine;
					}
					else
					{
						TokenRead = Token;
					}
				}
				else if (CanStartStringLiteral())
				{
					TokenRead = ScanStringLiteral();
				}
				else if (CanStartDateLiteral())
				{
					TokenRead = ScanDateLiteral();
				}
				else if (CanStartNumericLiteral())
				{
					TokenRead = ScanNumericLiteral();
				}
				else
				{
					var Start = CurrentLocation;
					var Punctuator = PunctuatorToken.TokenTypeFromString(Conversions.ToString(PeekChar()));

					if (Punctuator != TokenType.None)
					{
						// CONSIDER: Only call this if we know it can start a two-character punctuator
						TokenRead = ScanPossibleMultiCharacterPunctuator(ReadChar(), Start);
					}
					else
					{
						ReadChar();
						TokenRead = new ErrorToken(SyntaxErrorType.InvalidCharacter, SpanFrom(Start));
					}
				}

				_Tokens.Add(TokenRead);
			}

			// Advance to the next token if we need to
			if (advance)
			{
				_Position += 1;
				return _Tokens[_Position];
			}
			else
			{
				return _Tokens[_Position + 1];
			}
		}

		/// <summary>
    /// Seeks backwards in the stream position to a particular token.
    /// </summary>
    /// <param name="token">The token to seek back to.</param>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
    /// <exception cref="ArgumentException">Thrown when token was not produced by this scanner.</exception>
		public void Seek(Token token)
		{
			int CurrentPosition;
			int StartPosition = _Position;
			bool TokenFound = false;

			if (_Disposed)
				throw new ObjectDisposedException("Scanner");

			if (StartPosition == _Tokens.Count - 1)
			{
				StartPosition -= 1;
			}

			for (CurrentPosition = StartPosition; CurrentPosition >= -1; CurrentPosition -= 1)
			{
				if (ReferenceEquals(_Tokens[CurrentPosition + 1], token))
				{
					TokenFound = true;
					break;
				}
			}

			if (!TokenFound)
			{
				throw new ArgumentException("Token not created by this scanner.");
			}
			else
			{
				_Position = CurrentPosition;
			}
		}

		/// <summary>
    /// Whether the stream is positioned on the first token.
    /// </summary>
		public bool IsOnFirstToken
		{
			get
			{
				return _Position == -1;
			}
		}

		/// <summary>
    /// Fetches the previous token in the stream.
    /// </summary>
    /// <returns>The previous token.</returns>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when the scanner is positioned on the first token.</exception>
		public Token Previous()
		{
			if (_Disposed)
				throw new ObjectDisposedException("Scanner");

			if (_Position == -1)
			{
				throw new InvalidOperationException("Scanner is positioned on the first token.");
			}
			else
			{
				return _Tokens[_Position];
			}
		}

		/// <summary>
    /// Fetches the current token without advancing the stream position.
    /// </summary>
    /// <returns>The current token.</returns>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
		public Token Peek()
		{
			if (_Disposed)
				throw new ObjectDisposedException("Scanner");
			return Read(false);
		}

		/// <summary>
    /// Fetches the current token and advances the stream position.
    /// </summary>
    /// <returns>The current token.</returns>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
		public Token Read()
		{
			if (_Disposed)
				throw new ObjectDisposedException("Scanner");
			return Read(true);
		}

		/// <summary>
    /// Fetches more than one token at a time from the stream.
    /// </summary>
    /// <param name="buffer">The array to put the tokens into.</param>
    /// <param name="index">The location in the array to start putting the tokens into.</param>
    /// <param name="count">The number of tokens to read.</param>
    /// <returns>The number of tokens read.</returns>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
    /// <exception cref="System.NullReferenceException">Thrown when the buffer is Nothing.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the index or count is invalid, or when there isn't enough room in the buffer.</exception>
		public int ReadBlock(Token[] buffer, int index, int count)
		{
			int FinalCount = 0;

			if (buffer is null)
				throw new ArgumentNullException("buffer");
			if (index < 0 || count < 0)
				throw new ArgumentException("Index or count cannot be negative.");
			if (buffer.Length - index < count)
				throw new ArgumentException("Not enough room in buffer.");
			if (_Disposed)
				throw new ObjectDisposedException("Scanner");

			while (count > 0)
			{
				var CurrentToken = Read();

				if (CurrentToken.Type == TokenType.EndOfStream)
				{
					return FinalCount;
				}

				buffer[FinalCount + index] = CurrentToken;
				count -= 1;
				FinalCount += 1;
			}

			return FinalCount;
		}

		/// <summary>
    /// Reads all of the tokens between the current position and the end of the line (or the end of the stream).
    /// </summary>
    /// <returns>The tokens read.</returns>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
		public Token[] ReadLine()
		{
			var TokenArray = new List<Token>();

			if (_Disposed)
				throw new ObjectDisposedException("Scanner");

			while (Peek().Type != TokenType.EndOfStream & Peek().Type != TokenType.LineTerminator)
				TokenArray.Add(Read());

			return TokenArray.ToArray();
		}

		/// <summary>
    /// Reads all the tokens between the current position and the end of the stream.
    /// </summary>
    /// <returns>The tokens read.</returns>
    /// <exception cref="System.ObjectDisposedException">Thrown when the scanner has been closed.</exception>
		public Token[] ReadToEnd()
		{
			var TokenArray = new List<Token>();

			if (_Disposed)
				throw new ObjectDisposedException("Scanner");

			while (Peek().Type != TokenType.EndOfStream)
				TokenArray.Add(Read());

			return TokenArray.ToArray();
		}

		private int ParseInt(string literal)
		{
			int @base;
			if (literal.StartsWith("&"))
			{
				if (literal[1] == 'H' || literal[1] == 'h')
				{
					@base = 16;
				}
				else
				{
					@base = 8;
				} // Assume oct here
				literal = literal.Substring(2);
			}
			else
			{
				@base = 10;
			}
			return Convert.ToInt32(literal, @base);
		}

	}
}