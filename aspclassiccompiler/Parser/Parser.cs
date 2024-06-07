using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
	/// A parser for the Visual Basic .NET language based on the grammar
	/// documented in the Language Specification.
	/// </summary>
	public sealed class Parser : IDisposable
	{

		private enum PrecedenceLevel
		{
			None,
			Xor,
			Or,
			And,
			Not,
			Relational,
			Shift,
			Concatenate,
			Plus,
			Modulus,
			IntegralDivide,
			Multiply,
			Negate,
			Power,
			Range
		}

		private sealed class ExternalSourceContext
		{
			public Location Start;
			public string File;
			public long Line;
		}

		private sealed class RegionContext
		{
			public Location Start;
			public string Description;
		}

		private sealed class ConditionalCompilationContext
		{
			public bool BlockActive;
			public bool AnyBlocksActive;
			public bool SeenElse;
		}

		// The scanner we're going to be using.
		private Scanner Scanner;

		// The error table for the parsing
		private IList<SyntaxError> ErrorTable;

		// External line mappings
		private IList<ExternalLineMapping> ExternalLineMappings;

		// Source regions
		private IList<SourceRegion> SourceRegions;

		// External checksums
		private IList<ExternalChecksum> ExternalChecksums;

		// Conditional compilation constants
		private IDictionary<string, object> ConditionalCompilationConstants;

		// Whether there is an error in the construct
		private bool ErrorInConstruct;

		// Whether we're at the beginning of a line
		private bool AtBeginningOfLine;

		// Whether we're doing preprocessing or not
		private bool Preprocess;

		// LC Allow continue with Line Terminator
		private bool CanContinueWithoutLineTerminator;

		// The current stack of block contexts
		private Stack<TreeType> BlockContextStack = new Stack<TreeType>();

		// The current external source context
		private ExternalSourceContext CurrentExternalSourceContext;

		// The current stack of region contexts
		private Stack<RegionContext> RegionContextStack = new Stack<RegionContext>();

		// The current stack of conditional compilation states
		private Stack<ConditionalCompilationContext> ConditionalCompilationContextStack = new Stack<ConditionalCompilationContext>();

		// Determine whether we have been disposed already or not
		private bool Disposed = false;

		/// <summary>
    /// Disposes the parser.
    /// </summary>
		public void Dispose()
		{
			if (!Disposed)
			{
				Disposed = true;
				Scanner.Close();
			}
		}

		// *
		// * Token reading functions
		// *

		private Token Peek()
		{
			return Scanner.Peek();
		}

		private Token PeekAheadOne()
		{
			var Start = Read();
			var NextToken = Peek();

			Backtrack(Start);
			return NextToken;
		}

		private TokenType PeekAheadFor(params TokenType[] tokens)
		{
			var Start = Peek();
			var Current = Start;

			while (!CanEndStatement(Current))
			{
				foreach (TokenType Token in tokens)
				{
					if (Current.AsUnreservedKeyword() == Token)
					{
						Backtrack(Start);
						return Token;
					}
				}

				Current = Read();
			}

			Backtrack(Start);
			return TokenType.None;
		}

		private Token Read()
		{
			return Scanner.Read();
		}

		private Location ReadLocation()
		{
			return Read().Span.Start;
		}

		private void Backtrack(Token token)
		{
			Scanner.Seek(token);
		}

		private void ResyncAt(params TokenType[] tokenTypes)
		{
			var CurrentToken = Peek();

			while (CurrentToken.Type != TokenType.Colon && CurrentToken.Type != TokenType.EndOfStream && CurrentToken.Type != TokenType.LineTerminator && !BeginsStatement(CurrentToken))


			{
				foreach (TokenType TokenType in tokenTypes)
				{
					// CONSIDER: Need to check for non-reserved tokens?
					if (CurrentToken.Type == TokenType)
					{
						return;
					}
				}

				Read();
				CurrentToken = Peek();
			}
		}

		private List<CommentType> ParseTrailingComments()
		{
			var Comments = new List<CommentType>();

			// Link in comments that follow the statement
			while (Peek().Type == TokenType.Comment)
			{
				CommentToken CommentToken = (CommentToken)Scanner.Read();
				Comments.Add(new CommentType(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span));
			}

			if (Comments.Count > 0)
			{
				return Comments;
			}

			return null;
		}

		// *
		// * Helpers
		// *

		private void PushBlockContext(TreeType @type)
		{
			BlockContextStack.Push(type);
		}

		private void PopBlockContext()
		{
			BlockContextStack.Pop();
		}

		private TreeType CurrentBlockContextType()
		{
			if (BlockContextStack.Count == 0)
			{
				return TreeType.SyntaxError;
			}
			else
			{
				return BlockContextStack.Peek();
			}
		}

		private static OperatorType GetBinaryOperator(TokenType @type, bool allowRange = false)
		{
			switch (type)
			{
				case TokenType.Ampersand:
					{
						return OperatorType.Concatenate;
					}

				case TokenType.Star:
					{
						return OperatorType.Multiply;
					}

				case TokenType.ForwardSlash:
					{
						return OperatorType.Divide;
					}

				case TokenType.BackwardSlash:
					{
						return OperatorType.IntegralDivide;
					}

				case TokenType.Caret:
					{
						return OperatorType.Power;
					}

				case TokenType.Plus:
					{
						return OperatorType.Plus;
					}

				case TokenType.Minus:
					{
						return OperatorType.Minus;
					}

				case TokenType.LessThan:
					{
						return OperatorType.LessThan;
					}

				case TokenType.LessThanEquals:
					{
						return OperatorType.LessThanEquals;
					}

				case TokenType.Equals:
					{
						return OperatorType.Equals;
					}

				case TokenType.NotEquals:
					{
						return OperatorType.NotEquals;
					}

				case TokenType.GreaterThan:
					{
						return OperatorType.GreaterThan;
					}

				case TokenType.GreaterThanEquals:
					{
						return OperatorType.GreaterThanEquals;
					}

				case TokenType.LessThanLessThan:
					{
						return OperatorType.ShiftLeft;
					}

				case TokenType.GreaterThanGreaterThan:
					{
						return OperatorType.ShiftRight;
					}

				case TokenType.Mod:
					{
						return OperatorType.Modulus;
					}

				case TokenType.Or:
					{
						return OperatorType.Or;
					}

				case TokenType.OrElse:
					{
						return OperatorType.OrElse;
					}

				case TokenType.And:
					{
						return OperatorType.And;
					}

				case TokenType.AndAlso:
					{
						return OperatorType.AndAlso;
					}

				case TokenType.Xor:
					{
						return OperatorType.Xor;
					}

				case TokenType.Like:
					{
						return OperatorType.Like;
					}

				case TokenType.Is:
					{
						return OperatorType.Is;
					}

				case TokenType.IsNot:
					{
						return OperatorType.IsNot;
					}

				case TokenType.To:
					{
						if (allowRange)
						{
							return OperatorType.To;
						}
						else
						{
							return OperatorType.None;
						}
					}

				default:
					{
						return OperatorType.None;
					}
			}
		}

		private static PrecedenceLevel GetOperatorPrecedence(OperatorType @operator)
		{
			switch (@operator)
			{
				case OperatorType.To:
					{
						return PrecedenceLevel.Range;
					}

				case OperatorType.Power:
					{
						return PrecedenceLevel.Power;
					}

				case OperatorType.Negate:
				case OperatorType.UnaryPlus:
					{
						return PrecedenceLevel.Negate;
					}

				case OperatorType.Multiply:
				case OperatorType.Divide:
					{
						return PrecedenceLevel.Multiply;
					}

				case OperatorType.IntegralDivide:
					{
						return PrecedenceLevel.IntegralDivide;
					}

				case OperatorType.Modulus:
					{
						return PrecedenceLevel.Modulus;
					}

				case OperatorType.Plus:
				case OperatorType.Minus:
					{
						return PrecedenceLevel.Plus;
					}

				case OperatorType.Concatenate:
					{
						return PrecedenceLevel.Concatenate;
					}

				case OperatorType.ShiftLeft:
				case OperatorType.ShiftRight:
					{
						return PrecedenceLevel.Shift;
					}

				case OperatorType.Equals:
				case OperatorType.NotEquals:
				case OperatorType.LessThan:
				case OperatorType.GreaterThan:
				case OperatorType.GreaterThanEquals:
				case OperatorType.LessThanEquals:
				case OperatorType.Is:
				case OperatorType.IsNot:
				case OperatorType.Like:
					{
						return PrecedenceLevel.Relational;
					}

				case OperatorType.Not:
					{
						return PrecedenceLevel.Not;
					}

				case OperatorType.And:
				case OperatorType.AndAlso:
					{
						return PrecedenceLevel.And;
					}

				case OperatorType.Or:
				case OperatorType.OrElse:
					{
						return PrecedenceLevel.Or;
					}

				case OperatorType.Xor:
					{
						return PrecedenceLevel.Xor;
					}

				default:
					{
						return PrecedenceLevel.None;
					}
			}
		}

		private static OperatorType GetCompoundAssignmentOperatorType(TokenType tokenType)
		{
			switch (tokenType)
			{
				case TokenType.PlusEquals:
					{
						return OperatorType.Plus;
					}

				case TokenType.AmpersandEquals:
					{
						return OperatorType.Concatenate;
					}

				case TokenType.StarEquals:
					{
						return OperatorType.Multiply;
					}

				case TokenType.MinusEquals:
					{
						return OperatorType.Minus;
					}

				case TokenType.ForwardSlashEquals:
					{
						return OperatorType.Divide;
					}

				case TokenType.BackwardSlashEquals:
					{
						return OperatorType.IntegralDivide;
					}

				case TokenType.CaretEquals:
					{
						return OperatorType.Power;
					}

				case TokenType.LessThanLessThanEquals:
					{
						return OperatorType.ShiftLeft;
					}

				case TokenType.GreaterThanGreaterThanEquals:
					{
						return OperatorType.ShiftRight;
					}

				default:
					{
						return OperatorType.None;
					}
			}

			return OperatorType.None;
		}

		private static TreeType GetAssignmentOperator(TokenType tokenType)
		{

			switch (tokenType)
			{
				case TokenType.Equals:
					{
						return TreeType.AssignmentStatement;
					}

				case TokenType.PlusEquals:
				case TokenType.AmpersandEquals:
				case TokenType.StarEquals:
				case TokenType.MinusEquals:
				case TokenType.ForwardSlashEquals:
				case TokenType.BackwardSlashEquals:
				case TokenType.CaretEquals:
				case TokenType.LessThanLessThanEquals:
				case TokenType.GreaterThanGreaterThanEquals:
					{
						return TreeType.CompoundAssignmentStatement;
					}

				default:
					{
						break;
					}
			}

			return TreeType.SyntaxError;
		}

		private static bool IsRelationalOperator(TokenType @type)
		{
			return type >= TokenType.LessThan && type <= TokenType.GreaterThanEquals;
		}

		private static bool IsOverloadableOperator(Token op)
		{
			switch (op.Type)
			{
				case TokenType.Plus:
				case TokenType.Minus:
				case TokenType.Not:
				case TokenType.Star:
				case TokenType.ForwardSlash:
				case TokenType.BackwardSlash:
				case TokenType.Ampersand:
				case TokenType.Like:
				case TokenType.Mod:
				case TokenType.And:
				case TokenType.Or:
				case TokenType.Xor:
				case TokenType.Caret:
				case TokenType.LessThanLessThan:
				case TokenType.GreaterThanGreaterThan:
				case TokenType.Equals:
				case TokenType.NotEquals:
				case TokenType.LessThan:
				case TokenType.GreaterThan:
				case TokenType.LessThanEquals:
				case TokenType.GreaterThanEquals:
				case TokenType.CType:
					{
						return true;
					}
				case TokenType.Identifier:
					{
						if (op.AsUnreservedKeyword() == TokenType.IsTrue || op.AsUnreservedKeyword() == TokenType.IsFalse)
						{
							return true;
						}

						break;
					}
			}

			return false;
		}

		private static BlockType GetContinueType(TokenType tokenType)
		{
			switch (tokenType)
			{
				case TokenType.Do:
					{
						return BlockType.Do;
					}

				case TokenType.For:
					{
						return BlockType.For;
					}

				case TokenType.While:
					{
						return BlockType.While;
					}

				default:
					{
						return BlockType.None;
					}
			}
		}

		private static BlockType GetExitType(TokenType tokenType)
		{
			switch (tokenType)
			{
				case TokenType.Do:
					{
						return BlockType.Do;
					}

				case TokenType.For:
					{
						return BlockType.For;
					}

				case TokenType.While:
					{
						return BlockType.While;
					}

				case TokenType.Select:
					{
						return BlockType.Select;
					}

				case TokenType.Sub:
					{
						return BlockType.Sub;
					}

				case TokenType.Function:
					{
						return BlockType.Function;
					}

				case TokenType.Property:
					{
						return BlockType.Property;
					}

				case TokenType.Try:
					{
						return BlockType.Try;
					}

				default:
					{
						return BlockType.None;
					}
			}
		}

		private static BlockType GetBlockType(TokenType @type)
		{
			switch (type)
			{
				case TokenType.While:
					{
						return BlockType.While;
					}

				case TokenType.Select:
					{
						return BlockType.Select;
					}

				case TokenType.If:
					{
						return BlockType.If;
					}

				case TokenType.Try:
					{
						return BlockType.Try;
					}

				case TokenType.SyncLock:
					{
						return BlockType.SyncLock;
					}

				case TokenType.Using:
					{
						return BlockType.Using;
					}

				case TokenType.With:
					{
						return BlockType.With;
					}

				case TokenType.Sub:
					{
						return BlockType.Sub;
					}

				case TokenType.Function:
					{
						return BlockType.Function;
					}

				case TokenType.Operator:
					{
						return BlockType.Operator;
					}

				case TokenType.Get:
					{
						return BlockType.Get;
					}

				case TokenType.Set:
					{
						return BlockType.Set;
					}

				case TokenType.Event:
					{
						return BlockType.Event;
					}

				case TokenType.AddHandler:
					{
						return BlockType.AddHandler;
					}

				case TokenType.RemoveHandler:
					{
						return BlockType.RemoveHandler;
					}

				case TokenType.RaiseEvent:
					{
						return BlockType.RaiseEvent;
					}

				case TokenType.Property:
					{
						return BlockType.Property;
					}

				case TokenType.Class:
					{
						return BlockType.Class;
					}

				case TokenType.Structure:
					{
						return BlockType.Structure;
					}

				case TokenType.Module:
					{
						return BlockType.Module;
					}

				case TokenType.Interface:
					{
						return BlockType.Interface;
					}

				case TokenType.Enum:
					{
						return BlockType.Enum;
					}

				case TokenType.Namespace:
					{
						return BlockType.Namespace;
					}

				default:
					{
						return BlockType.None;
					}
			}
		}

		private void ReportSyntaxError(SyntaxError syntaxError)
		{
			if (ErrorInConstruct)
			{
				return;
			}

			ErrorInConstruct = true;

			if (ErrorTable is not null)
			{
				ErrorTable.Add(syntaxError);
			}
		}

		private void ReportSyntaxError(SyntaxErrorType errorType, Span span)
		{
			ReportSyntaxError(new SyntaxError(errorType, span));
		}

		private void ReportSyntaxError(SyntaxErrorType errorType, Token firstToken, Token lastToken)
		{
			// A lexical error takes precedence over the parser error
			if (firstToken.Type == TokenType.LexicalError)
			{
				ReportSyntaxError(((ErrorToken)firstToken).SyntaxError);
			}
			else
			{
				ReportSyntaxError(errorType, SpanFrom(firstToken, lastToken));
			}
		}

		private void ReportSyntaxError(SyntaxErrorType errorType, Token token)
		{
			ReportSyntaxError(errorType, token, token);
		}

		private static bool StatementEndsBlock(TreeType blockStatementType, Statement endStatement)
		{
			switch (blockStatementType)
			{
				case TreeType.DoBlockStatement:
					{
						return endStatement.Type == TreeType.LoopStatement;
					}

				case TreeType.ForBlockStatement:
				case TreeType.ForEachBlockStatement:
					{
						return endStatement.Type == TreeType.NextStatement;
					}

				case TreeType.WhileBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.While;
					}

				case TreeType.SyncLockBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.SyncLock;
					}

				case TreeType.UsingBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Using;
					}

				case TreeType.WithBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.With;
					}

				case TreeType.TryBlockStatement:
				case TreeType.CatchBlockStatement:
					{
						return endStatement.Type == TreeType.CatchStatement || endStatement.Type == TreeType.FinallyStatement || endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Try;

					}

				case TreeType.FinallyBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Try;
					}

				case TreeType.SelectBlockStatement:
				case TreeType.CaseBlockStatement:
					{
						return endStatement.Type == TreeType.CaseStatement || endStatement.Type == TreeType.CaseElseStatement || endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Select;

					}

				case TreeType.CaseElseBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Select;
					}

				case TreeType.IfBlockStatement:
				case TreeType.ElseIfBlockStatement:
					{
						return endStatement.Type == TreeType.ElseIfStatement || endStatement.Type == TreeType.ElseStatement || endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.If;

					}

				case TreeType.ElseBlockStatement:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.If;
					}

				case TreeType.LineIfBlockStatement:
					{
						// LC LineIf can end with end if
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.If;
					}
				// Return False

				case TreeType.SubDeclaration:
				case TreeType.ConstructorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Sub;
					}

				case TreeType.FunctionDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Function;
					}

				case TreeType.OperatorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Operator;
					}

				case TreeType.GetAccessorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Get;
					}

				case TreeType.SetAccessorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Set;
					}

				case TreeType.PropertyDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Property;
					}

				case TreeType.CustomEventDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Event;
					}

				case TreeType.AddHandlerAccessorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.AddHandler;
					}

				case TreeType.RemoveHandlerAccessorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.RemoveHandler;
					}

				case TreeType.RaiseEventAccessorDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.RaiseEvent;
					}

				case TreeType.ClassDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Class;
					}

				case TreeType.StructureDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Structure;
					}

				case TreeType.ModuleDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Module;
					}

				case TreeType.InterfaceDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Interface;
					}

				case TreeType.EnumDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Enum;
					}

				case TreeType.NamespaceDeclaration:
					{
						return endStatement.Type == TreeType.EndBlockStatement && ((EndBlockStatement)endStatement).EndType == BlockType.Namespace;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			return false;
		}

		private static bool DeclarationEndsBlock(TreeType blockDeclarationType, EndBlockDeclaration endDeclaration)
		{
			switch (blockDeclarationType)
			{
				case TreeType.SubDeclaration:
				case TreeType.ConstructorDeclaration:
					{
						return endDeclaration.EndType == BlockType.Sub;
					}

				case TreeType.FunctionDeclaration:
					{
						return endDeclaration.EndType == BlockType.Function;
					}

				case TreeType.OperatorDeclaration:
					{
						return endDeclaration.EndType == BlockType.Operator;
					}

				case TreeType.PropertyDeclaration:
					{
						return endDeclaration.EndType == BlockType.Property;
					}

				case TreeType.GetAccessorDeclaration:
					{
						return endDeclaration.EndType == BlockType.Get;
					}

				case TreeType.SetAccessorDeclaration:
					{
						return endDeclaration.EndType == BlockType.Set;
					}

				case TreeType.CustomEventDeclaration:
					{
						return endDeclaration.EndType == BlockType.Event;
					}

				case TreeType.AddHandlerAccessorDeclaration:
					{
						return endDeclaration.EndType == BlockType.AddHandler;
					}

				case TreeType.RemoveHandlerAccessorDeclaration:
					{
						return endDeclaration.EndType == BlockType.RemoveHandler;
					}

				case TreeType.RaiseEventAccessorDeclaration:
					{
						return endDeclaration.EndType == BlockType.RaiseEvent;
					}

				case TreeType.ClassDeclaration:
					{
						return endDeclaration.EndType == BlockType.Class;
					}

				case TreeType.StructureDeclaration:
					{
						return endDeclaration.EndType == BlockType.Structure;
					}

				case TreeType.ModuleDeclaration:
					{
						return endDeclaration.EndType == BlockType.Module;
					}

				case TreeType.InterfaceDeclaration:
					{
						return endDeclaration.EndType == BlockType.Interface;
					}

				case TreeType.EnumDeclaration:
					{
						return endDeclaration.EndType == BlockType.Enum;
					}

				case TreeType.NamespaceDeclaration:
					{
						return endDeclaration.EndType == BlockType.Namespace;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			return false;
		}

		private static bool ValidInContext(TreeType blockType, TreeType declarationType)
		{
			switch (declarationType)
			{
				case TreeType.OptionDeclaration:
				case TreeType.ImportsDeclaration:
				case TreeType.AttributeDeclaration:
					{
						return blockType == TreeType.File;
					}

				case TreeType.NamespaceDeclaration:
					{
						return blockType == TreeType.NamespaceDeclaration || blockType == TreeType.File;
					}

				case TreeType.ClassDeclaration:
				case TreeType.StructureDeclaration:
				case TreeType.InterfaceDeclaration:
				case TreeType.DelegateSubDeclaration:
				case TreeType.DelegateFunctionDeclaration:
				case TreeType.EnumDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration || blockType == TreeType.ModuleDeclaration || blockType == TreeType.InterfaceDeclaration || blockType == TreeType.NamespaceDeclaration || blockType == TreeType.File;




					}

				case TreeType.ModuleDeclaration:
					{
						return blockType == TreeType.NamespaceDeclaration || blockType == TreeType.File;
					}

				case TreeType.EventDeclaration:
				case TreeType.SubDeclaration:
				case TreeType.FunctionDeclaration:
				case TreeType.PropertyDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration || blockType == TreeType.ModuleDeclaration || blockType == TreeType.InterfaceDeclaration;


					}

				case TreeType.CustomEventDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration || blockType == TreeType.ModuleDeclaration;

					}

				case TreeType.AddHandlerAccessorDeclaration:
				case TreeType.RemoveHandlerAccessorDeclaration:
				case TreeType.RaiseEventAccessorDeclaration:
					{
						return blockType == TreeType.CustomEventDeclaration;
					}

				case TreeType.OperatorDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration;
					}

				case TreeType.VariableListDeclaration:
				case TreeType.ExternalSubDeclaration:
				case TreeType.ExternalFunctionDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration || blockType == TreeType.ModuleDeclaration;

					}

				case TreeType.ConstructorDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration;
					}

				case TreeType.GetAccessorDeclaration:
				case TreeType.SetAccessorDeclaration:
					{
						return blockType == TreeType.PropertyDeclaration;
					}

				case TreeType.InheritsDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.InterfaceDeclaration;
					}

				case TreeType.ImplementsDeclaration:
					{
						return blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration;
					}

				case TreeType.EnumValueDeclaration:
					{
						return blockType == TreeType.EnumDeclaration;
					}

				case TreeType.EmptyDeclaration:
					{
						return true;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						throw new NotSupportedException("Unknown TreeType.");
					}
			}
		}

		private static SyntaxErrorType ValidDeclaration(TreeType blockType, Declaration declaration, List<Declaration> declarations)
		{
			if (!ValidInContext(blockType, declaration.Type))
			{
				return InvalidDeclarationTypeError(blockType);
			}

			if (declaration.Type == TreeType.InheritsDeclaration)
			{
				foreach (Declaration ExistingDeclaration in declarations)
				{
					if (blockType == TreeType.ClassDeclaration || ExistingDeclaration.Type != TreeType.InheritsDeclaration)
					{
						return SyntaxErrorType.InheritsMustBeFirst;
					}
				}

				if (((InheritsDeclaration)declaration).InheritedTypes.Count > 1 && blockType != TreeType.InterfaceDeclaration)
				{
					return SyntaxErrorType.NoMultipleInheritance;
				}
			}

			if (declaration.Type == TreeType.ImplementsDeclaration)
			{
				foreach (Declaration ExistingDeclaration in declarations)
				{
					if (ExistingDeclaration.Type != TreeType.InheritsDeclaration && ExistingDeclaration.Type != TreeType.ImplementsDeclaration)
					{
						return SyntaxErrorType.ImplementsInWrongOrder;
					}
				}
			}

			if (declaration.Type == TreeType.OptionDeclaration)
			{
				foreach (Declaration ExistingDeclaration in declarations)
				{
					if (ExistingDeclaration.Type != TreeType.OptionDeclaration)
					{
						return SyntaxErrorType.OptionStatementWrongOrder;
					}
				}
			}

			if (declaration.Type == TreeType.ImportsDeclaration)
			{
				foreach (Declaration ExistingDeclaration in declarations)
				{
					if (ExistingDeclaration.Type != TreeType.OptionDeclaration && ExistingDeclaration.Type != TreeType.ImportsDeclaration)
					{
						return SyntaxErrorType.ImportsStatementWrongOrder;
					}
				}
			}

			if (declaration.Type == TreeType.AttributeDeclaration)
			{
				foreach (Declaration ExistingDeclaration in declarations)
				{
					if (ExistingDeclaration.Type != TreeType.OptionDeclaration && ExistingDeclaration.Type != TreeType.ImportsDeclaration && ExistingDeclaration.Type != TreeType.AttributeDeclaration)

					{
						return SyntaxErrorType.AttributesStatementWrongOrder;
					}
				}
			}

			return SyntaxErrorType.None;
		}

		private void ReportMismatchedEndError(TreeType blockType, Span actualEndSpan)
		{
			var ErrorType = default(SyntaxErrorType);

			switch (blockType)
			{
				case TreeType.DoBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedLoop;
						break;
					}

				case TreeType.ForBlockStatement:
				case TreeType.ForEachBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedNext;
						break;
					}

				case TreeType.WhileBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndWhile;
						break;
					}

				case TreeType.SelectBlockStatement:
				case TreeType.CaseBlockStatement:
				case TreeType.CaseElseBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndSelect;
						break;
					}

				case TreeType.SyncLockBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndSyncLock;
						break;
					}

				case TreeType.UsingBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndUsing;
						break;
					}

				case TreeType.IfBlockStatement:
				case TreeType.ElseIfBlockStatement:
				case TreeType.ElseBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndIf;
						break;
					}

				case TreeType.TryBlockStatement:
				case TreeType.CatchBlockStatement:
				case TreeType.FinallyBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndTry;
						break;
					}

				case TreeType.WithBlockStatement:
					{
						ErrorType = SyntaxErrorType.ExpectedEndWith;
						break;
					}

				case TreeType.SubDeclaration:
				case TreeType.ConstructorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndSub;
						break;
					}

				case TreeType.FunctionDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndFunction;
						break;
					}

				case TreeType.OperatorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndOperator;
						break;
					}

				case TreeType.PropertyDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndProperty;
						break;
					}

				case TreeType.GetAccessorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndGet;
						break;
					}

				case TreeType.SetAccessorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndSet;
						break;
					}

				case TreeType.CustomEventDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndEvent;
						break;
					}

				case TreeType.AddHandlerAccessorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndAddHandler;
						break;
					}

				case TreeType.RemoveHandlerAccessorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndRemoveHandler;
						break;
					}

				case TreeType.RaiseEventAccessorDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndRaiseEvent;
						break;
					}

				case TreeType.ClassDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndClass;
						break;
					}

				case TreeType.StructureDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndStructure;
						break;
					}

				case TreeType.ModuleDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndModule;
						break;
					}

				case TreeType.InterfaceDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndInterface;
						break;
					}

				case TreeType.EnumDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndEnum;
						break;
					}

				case TreeType.NamespaceDeclaration:
					{
						ErrorType = SyntaxErrorType.ExpectedEndNamespace;
						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			ReportSyntaxError(ErrorType, actualEndSpan);
		}

		private void ReportMissingBeginStatementError(TreeType blockStatementType, Statement endStatement)
		{
			var ErrorType = default(SyntaxErrorType);

			switch (endStatement.Type)
			{
				case TreeType.LoopStatement:
					{
						ErrorType = SyntaxErrorType.LoopWithoutDo;
						break;
					}

				case TreeType.NextStatement:
					{
						ErrorType = SyntaxErrorType.NextWithoutFor;
						break;
					}

				case TreeType.EndBlockStatement:
					{
						switch (((EndBlockStatement)endStatement).EndType)
						{
							case BlockType.While:
								{
									ErrorType = SyntaxErrorType.EndWhileWithoutWhile;
									break;
								}

							case BlockType.Select:
								{
									ErrorType = SyntaxErrorType.EndSelectWithoutSelect;
									break;
								}

							case BlockType.SyncLock:
								{
									ErrorType = SyntaxErrorType.EndSyncLockWithoutSyncLock;
									break;
								}

							case BlockType.Using:
								{
									ErrorType = SyntaxErrorType.EndUsingWithoutUsing;
									break;
								}

							case BlockType.If:
								{
									ErrorType = SyntaxErrorType.EndIfWithoutIf;
									break;
								}

							case BlockType.Try:
								{
									ErrorType = SyntaxErrorType.EndTryWithoutTry;
									break;
								}

							case BlockType.With:
								{
									ErrorType = SyntaxErrorType.EndWithWithoutWith;
									break;
								}

							case BlockType.Sub:
								{
									ErrorType = SyntaxErrorType.EndSubWithoutSub;
									break;
								}

							case BlockType.Function:
								{
									ErrorType = SyntaxErrorType.EndFunctionWithoutFunction;
									break;
								}

							case BlockType.Operator:
								{
									ErrorType = SyntaxErrorType.EndOperatorWithoutOperator;
									break;
								}

							case BlockType.Get:
								{
									ErrorType = SyntaxErrorType.EndGetWithoutGet;
									break;
								}

							case BlockType.Set:
								{
									ErrorType = SyntaxErrorType.EndSetWithoutSet;
									break;
								}

							case BlockType.Property:
								{
									ErrorType = SyntaxErrorType.EndPropertyWithoutProperty;
									break;
								}

							case BlockType.Event:
								{
									ErrorType = SyntaxErrorType.EndEventWithoutEvent;
									break;
								}

							case BlockType.AddHandler:
								{
									ErrorType = SyntaxErrorType.EndAddHandlerWithoutAddHandler;
									break;
								}

							case BlockType.RemoveHandler:
								{
									ErrorType = SyntaxErrorType.EndRemoveHandlerWithoutRemoveHandler;
									break;
								}

							case BlockType.RaiseEvent:
								{
									ErrorType = SyntaxErrorType.EndRaiseEventWithoutRaiseEvent;
									break;
								}

							case BlockType.Class:
								{
									ErrorType = SyntaxErrorType.EndClassWithoutClass;
									break;
								}

							case BlockType.Structure:
								{
									ErrorType = SyntaxErrorType.EndStructureWithoutStructure;
									break;
								}

							case BlockType.Module:
								{
									ErrorType = SyntaxErrorType.EndModuleWithoutModule;
									break;
								}

							case BlockType.Interface:
								{
									ErrorType = SyntaxErrorType.EndInterfaceWithoutInterface;
									break;
								}

							case BlockType.Enum:
								{
									ErrorType = SyntaxErrorType.EndEnumWithoutEnum;
									break;
								}

							case BlockType.Namespace:
								{
									ErrorType = SyntaxErrorType.EndNamespaceWithoutNamespace;
									break;
								}

							default:
								{
									Debug.Assert(false, "Unexpected.");
									break;
								}
						}

						break;
					}

				case TreeType.CatchStatement:
					{
						if (blockStatementType == TreeType.FinallyBlockStatement)
						{
							ErrorType = SyntaxErrorType.CatchAfterFinally;
						}
						else
						{
							ErrorType = SyntaxErrorType.CatchWithoutTry;
						}

						break;
					}

				case TreeType.FinallyStatement:
					{
						if (blockStatementType == TreeType.FinallyBlockStatement)
						{
							ErrorType = SyntaxErrorType.FinallyAfterFinally;
						}
						else
						{
							ErrorType = SyntaxErrorType.FinallyWithoutTry;
						}

						break;
					}

				case TreeType.CaseStatement:
					{
						if (blockStatementType == TreeType.CaseElseBlockStatement)
						{
							ErrorType = SyntaxErrorType.CaseAfterCaseElse;
						}
						else
						{
							ErrorType = SyntaxErrorType.CaseWithoutSelect;
						}

						break;
					}

				case TreeType.CaseElseStatement:
					{
						if (blockStatementType == TreeType.CaseElseBlockStatement)
						{
							ErrorType = SyntaxErrorType.CaseElseAfterCaseElse;
						}
						else
						{
							ErrorType = SyntaxErrorType.CaseElseWithoutSelect;
						}

						break;
					}

				case TreeType.ElseIfStatement:
					{
						if (blockStatementType == TreeType.ElseBlockStatement)
						{
							ErrorType = SyntaxErrorType.ElseIfAfterElse;
						}
						else
						{
							ErrorType = SyntaxErrorType.ElseIfWithoutIf;
						}

						break;
					}

				case TreeType.ElseStatement:
					{
						if (blockStatementType == TreeType.ElseBlockStatement)
						{
							ErrorType = SyntaxErrorType.ElseAfterElse;
						}
						else
						{
							ErrorType = SyntaxErrorType.ElseWithoutIf;
						}

						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			ReportSyntaxError(ErrorType, endStatement.Span);
		}

		private void ReportMissingBeginDeclarationError(EndBlockDeclaration endDeclaration)
		{
			var ErrorType = default(SyntaxErrorType);

			switch (endDeclaration.EndType)
			{
				case BlockType.Sub:
					{
						ErrorType = SyntaxErrorType.EndSubWithoutSub;
						break;
					}

				case BlockType.Function:
					{
						ErrorType = SyntaxErrorType.EndFunctionWithoutFunction;
						break;
					}

				case BlockType.Operator:
					{
						ErrorType = SyntaxErrorType.EndOperatorWithoutOperator;
						break;
					}

				case BlockType.Property:
					{
						ErrorType = SyntaxErrorType.EndPropertyWithoutProperty;
						break;
					}

				case BlockType.Get:
					{
						ErrorType = SyntaxErrorType.EndGetWithoutGet;
						break;
					}

				case BlockType.Set:
					{
						ErrorType = SyntaxErrorType.EndSetWithoutSet;
						break;
					}

				case BlockType.Event:
					{
						ErrorType = SyntaxErrorType.EndEventWithoutEvent;
						break;
					}

				case BlockType.AddHandler:
					{
						ErrorType = SyntaxErrorType.EndAddHandlerWithoutAddHandler;
						break;
					}

				case BlockType.RemoveHandler:
					{
						ErrorType = SyntaxErrorType.EndRemoveHandlerWithoutRemoveHandler;
						break;
					}

				case BlockType.RaiseEvent:
					{
						ErrorType = SyntaxErrorType.EndRaiseEventWithoutRaiseEvent;
						break;
					}

				case BlockType.Class:
					{
						ErrorType = SyntaxErrorType.EndClassWithoutClass;
						break;
					}

				case BlockType.Structure:
					{
						ErrorType = SyntaxErrorType.EndStructureWithoutStructure;
						break;
					}

				case BlockType.Module:
					{
						ErrorType = SyntaxErrorType.EndModuleWithoutModule;
						break;
					}

				case BlockType.Interface:
					{
						ErrorType = SyntaxErrorType.EndInterfaceWithoutInterface;
						break;
					}

				case BlockType.Enum:
					{
						ErrorType = SyntaxErrorType.EndEnumWithoutEnum;
						break;
					}

				case BlockType.Namespace:
					{
						ErrorType = SyntaxErrorType.EndNamespaceWithoutNamespace;
						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			ReportSyntaxError(ErrorType, endDeclaration.Span);
		}

		private static SyntaxErrorType InvalidDeclarationTypeError(TreeType blockType)
		{
			var ErrorType = default(SyntaxErrorType);

			switch (blockType)
			{
				case TreeType.PropertyDeclaration:
					{
						ErrorType = SyntaxErrorType.InvalidInsideProperty;
						break;
					}

				case TreeType.ClassDeclaration:
					{
						ErrorType = SyntaxErrorType.InvalidInsideClass;
						break;
					}

				case TreeType.StructureDeclaration:
					{
						ErrorType = SyntaxErrorType.InvalidInsideStructure;
						break;
					}

				case TreeType.ModuleDeclaration:
					{
						ErrorType = SyntaxErrorType.InvalidInsideModule;
						break;
					}

				case TreeType.InterfaceDeclaration:
					{
						ErrorType = SyntaxErrorType.InvalidInsideInterface;
						break;
					}

				case TreeType.EnumDeclaration:
					{
						ErrorType = SyntaxErrorType.InvalidInsideEnum;
						break;
					}

				case TreeType.NamespaceDeclaration:
				case TreeType.File:
					{
						ErrorType = SyntaxErrorType.InvalidInsideNamespace;
						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			return ErrorType;
		}

		private void HandleUnexpectedToken(TokenType @type)
		{
			SyntaxErrorType ErrorType;

			switch (type)
			{
				case TokenType.Comma:
					{
						ErrorType = SyntaxErrorType.ExpectedComma;
						break;
					}

				case TokenType.LeftParenthesis:
					{
						ErrorType = SyntaxErrorType.ExpectedLeftParenthesis;
						break;
					}

				case TokenType.RightParenthesis:
					{
						ErrorType = SyntaxErrorType.ExpectedRightParenthesis;
						break;
					}

				case TokenType.Equals:
					{
						ErrorType = SyntaxErrorType.ExpectedEquals;
						break;
					}

				case TokenType.As:
					{
						ErrorType = SyntaxErrorType.ExpectedAs;
						break;
					}

				case TokenType.RightCurlyBrace:
					{
						ErrorType = SyntaxErrorType.ExpectedRightCurlyBrace;
						break;
					}

				case TokenType.Period:
					{
						ErrorType = SyntaxErrorType.ExpectedPeriod;
						break;
					}

				case TokenType.Minus:
					{
						ErrorType = SyntaxErrorType.ExpectedMinus;
						break;
					}

				case TokenType.Is:
					{
						ErrorType = SyntaxErrorType.ExpectedIs;
						break;
					}

				case TokenType.GreaterThan:
					{
						ErrorType = SyntaxErrorType.ExpectedGreaterThan;
						break;
					}

				case TokenType.Of:
					{
						ErrorType = SyntaxErrorType.ExpectedOf;
						break;
					}

				default:
					{
						Debug.Assert(false, "Should give a more specific error.");
						ErrorType = SyntaxErrorType.SyntaxError;
						break;
					}
			}

			ReportSyntaxError(ErrorType, Peek());
		}

		private Location VerifyExpectedToken(TokenType @type)
		{
			var Token = Peek();

			if (Token.Type == type)
			{
				return ReadLocation();
			}
			else
			{
				HandleUnexpectedToken(type);
				return new Location();
			}
		}

		private bool CanEndStatement(Token token)
		{
			return token.Type == TokenType.Colon || token.Type == TokenType.LineTerminator || token.Type == TokenType.EndOfStream || token.Type == TokenType.Comment || BlockContextStack.Count > 0 && CurrentBlockContextType() == TreeType.LineIfBlockStatement && token.Type == TokenType.Else;



		}

		private bool BeginsStatement(Token token)
		{
			if (!CanEndStatement(token))
			{
				return false;
			}

			switch (token.Type)
			{
				case TokenType.AddHandler:
				case TokenType.Call:
				case TokenType.Case:
				case TokenType.Catch:
				case TokenType.Class:
				case TokenType.Const:
				case TokenType.Declare:
				case TokenType.Delegate:
				case TokenType.Dim:
				case TokenType.Do:
				case TokenType.Else:
				case TokenType.ElseIf:
				case TokenType.End:
				case TokenType.EndIf:
				case TokenType.Enum:
				case TokenType.Erase:
				case TokenType.Error:
				case TokenType.Event:
				case TokenType.Exit:
				case TokenType.Finally:
				case TokenType.For:
				case TokenType.Friend:
				case TokenType.Function:
				case TokenType.Get:
				case TokenType.GoTo:
				case TokenType.GoSub:
				case TokenType.If:
				case TokenType.Implements:
				case TokenType.Imports:
				case TokenType.Inherits:
				case TokenType.Interface:
				case TokenType.Loop:
				case TokenType.Module:
				case TokenType.MustInherit:
				case TokenType.MustOverride:
				case TokenType.Namespace:
				case TokenType.Narrowing:
				case TokenType.Next:
				case TokenType.NotInheritable:
				case TokenType.NotOverridable:
				case TokenType.Option:
				case TokenType.Overloads:
				case TokenType.Overridable:
				case TokenType.Overrides:
				case TokenType.Partial:
				case TokenType.Private:
				case TokenType.Property:
				case TokenType.Protected:
				case TokenType.Public:
				case TokenType.RaiseEvent:
				case TokenType.ReadOnly:
				case TokenType.ReDim:
				case TokenType.RemoveHandler:
				case TokenType.Resume:
				case TokenType.Return:
				case TokenType.Select:
				case TokenType.Shadows:
				case TokenType.Shared:
				case TokenType.Static:
				case TokenType.Stop:
				case TokenType.Structure:
				case TokenType.Sub:
				case TokenType.SyncLock:
				case TokenType.Throw:
				case TokenType.Try:
				case TokenType.Using:
				case TokenType.Wend:
				case TokenType.While:
				case TokenType.Widening:
				case TokenType.With:
				case TokenType.WithEvents:
				case TokenType.WriteOnly:
				case TokenType.Pound:
					{
						return true;
					}

				default:
					{
						return false;
					}
			}
		}

		private Token VerifyEndOfStatement()
		{
			// LC Added CanContinueWithoutLineTerminator to support Case statement on the single line
			var NextToken = Peek();

			Debug.Assert(!(NextToken.Type == TokenType.Comment), "Should have dealt with these by now!");

			if (NextToken.Type == TokenType.LineTerminator || NextToken.Type == TokenType.EndOfStream)
			{
				AtBeginningOfLine = true;
				CanContinueWithoutLineTerminator = false;
			}
			else if (NextToken.Type == TokenType.Colon)
			{
				AtBeginningOfLine = false;
			}
			else if (NextToken.Type == TokenType.Else && CurrentBlockContextType() == TreeType.LineIfBlockStatement)
			{
				// Line If statements allow Else to end the statement
				AtBeginningOfLine = false;
			}
			else if (NextToken.Type == TokenType.End && CurrentBlockContextType() == TreeType.LineIfBlockStatement)
			{
				// LC Line If statement can end with End If
				AtBeginningOfLine = false;
			}
			else if (CanContinueWithoutLineTerminator)
			{
				return NextToken;
			}
			else
			{
				// Syntax error -- a valid statement is followed by something other than
				// a colon, end-of-line, or a comment.
				ResyncAt();
				ReportSyntaxError(SyntaxErrorType.ExpectedEndOfStatement, NextToken);
				return VerifyEndOfStatement();
			}

			return Read();
		}

		private static bool MustEndStatement(Token token)
		{
			return token.Type == TokenType.Colon || token.Type == TokenType.EndOfStream || token.Type == TokenType.LineTerminator || token.Type == TokenType.Comment;


		}

		private Span SpanFrom(Location location)
		{
			Location EndLocation;

			if (Peek().Type == TokenType.EndOfStream)
			{
				EndLocation = Scanner.Previous().Span.Finish;
			}
			else
			{
				EndLocation = Peek().Span.Start;
			}

			return new Span(location, EndLocation);
		}

		private Span SpanFrom(Token token)
		{
			Location StartLocation, EndLocation;

			if (token.Type == TokenType.EndOfStream && !Scanner.IsOnFirstToken)
			{
				StartLocation = Scanner.Previous().Span.Finish;
				EndLocation = StartLocation;
			}
			else
			{
				StartLocation = token.Span.Start;

				if (Peek().Type == TokenType.EndOfStream && !Scanner.IsOnFirstToken)
				{
					EndLocation = Scanner.Previous().Span.Finish;
				}
				else
				{
					EndLocation = Peek().Span.Start;
				}
			}

			return new Span(StartLocation, EndLocation);
		}

		private Span SpanFrom(Token startToken, Token endToken)
		{
			Location StartLocation, EndLocation;

			if (startToken.Type == TokenType.EndOfStream && !Scanner.IsOnFirstToken)
			{
				StartLocation = Scanner.Previous().Span.Finish;
				EndLocation = StartLocation;
			}
			else
			{
				StartLocation = startToken.Span.Start;

				if (endToken.Type == TokenType.EndOfStream && !Scanner.IsOnFirstToken)
				{
					EndLocation = Scanner.Previous().Span.Finish;
				}
				else if (endToken.Span.Start.Index == startToken.Span.Start.Index)
				{
					EndLocation = endToken.Span.Finish;
				}
				else
				{
					EndLocation = endToken.Span.Start;
				}
			}

			return new Span(StartLocation, EndLocation);
		}

		private static Span SpanFrom(Token startToken, Tree endTree)
		{
			return new Span(startToken.Span.Start, endTree.Span.Finish);
		}

		private Span SpanFrom(Statement startStatement, Statement endStatement)
		{
			if (endStatement is null)
			{
				return SpanFrom(startStatement.Span.Start);
			}
			else
			{
				return new Span(startStatement.Span.Start, endStatement.Span.Start);
			}
		}

		// *
		// * Names
		// *

		private SimpleName ParseSimpleName(bool allowKeyword)
		{
			if (Peek().Type == TokenType.Identifier)
			{
				IdentifierToken IdentifierToken = (IdentifierToken)Read();
				return new SimpleName(IdentifierToken.Identifier, IdentifierToken.TypeCharacter, IdentifierToken.Escaped, IdentifierToken.Span);
			}
			// If the token is a keyword, assume that the user meant it to 
			// be an identifer and consume it. Otherwise, leave current token
			// as is and let caller decide what to do.
			else if (IdentifierToken.IsKeyword(Peek().Type))
			{
				IdentifierToken IdentifierToken = (IdentifierToken)Read();

				if (!allowKeyword)
				{
					ReportSyntaxError(SyntaxErrorType.InvalidUseOfKeyword, IdentifierToken);
				}

				return new SimpleName(IdentifierToken.Identifier, IdentifierToken.TypeCharacter, IdentifierToken.Escaped, IdentifierToken.Span);
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Peek());
				return SimpleName.GetBadSimpleName(SpanFrom(Peek(), Peek()));
			}
		}

		private Name ParseName(bool AllowGlobal)
		{
			var Start = Peek();
			Name Result;
			bool QualificationRequired = false;

			// Seeing the global token implies > LanguageVersion.VisualBasic71
			if (Start.Type == TokenType.Global)
			{
				if (!AllowGlobal)
				{
					ReportSyntaxError(SyntaxErrorType.InvalidUseOfGlobal, Peek());
				}

				Read();
				Result = new SpecialName(TreeType.GlobalNamespaceName, SpanFrom(Start));
				QualificationRequired = true;
			}
			else
			{
				Result = ParseSimpleName(false);
			}

			if (Peek().Type == TokenType.Period)
			{
				SimpleName Qualifier;
				Location DotLocation;

				do
				{
					DotLocation = ReadLocation();
					Qualifier = ParseSimpleName(true);
					Result = new QualifiedName(Result, DotLocation, Qualifier, SpanFrom(Start));
				}
				while (Peek().Type == TokenType.Period);
			}
			else if (QualificationRequired)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedPeriod, Peek());
			}

			return Result;
		}

		private VariableName ParseVariableName(bool allowExplicitArraySizes)
		{
			var Start = Peek();
			SimpleName Name;
			ArrayTypeName ArrayType = null;

			// CONSIDER: Often, programmers put extra decl specifiers where they are not required. 
			// Eg: Dim x as Integer, Dim y as Long
			// Check for this and give a more informative error?

			Name = ParseSimpleName(false);

			if (Peek().Type == TokenType.LeftParenthesis)
			{
				ArrayType = ParseArrayTypeName(null, null, allowExplicitArraySizes, false);
			}

			return new VariableName(Name, ArrayType, SpanFrom(Start));
		}

		// This function implements some of the special name parsing logic for names in a name
		// list such as Implements and Handles
		private Name ParseNameListName(bool allowLeadingMeOrMyBase = false)
		{
			var Start = Peek();
			Name Result;

			if (Start.Type == TokenType.MyBase && allowLeadingMeOrMyBase)
			{
				Result = new SpecialName(TreeType.MyBaseName, SpanFrom(ReadLocation()));
			}
			else if (Start.Type == TokenType.Me && allowLeadingMeOrMyBase && Scanner.Version >= LanguageVersion.VisualBasic80)
			{
				Result = new SpecialName(TreeType.MeName, SpanFrom(ReadLocation()));
			}
			else
			{
				Result = ParseSimpleName(false);
			}

			if (Peek().Type == TokenType.Period)
			{
				SimpleName Qualifier;
				Location DotLocation;

				do
				{
					DotLocation = ReadLocation();
					Qualifier = ParseSimpleName(true);
					Result = new QualifiedName(Result, DotLocation, Qualifier, SpanFrom(Start));
				}
				while (Peek().Type == TokenType.Period);
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedPeriod, Peek());
			}

			return Result;
		}

		// *
		// * Types
		// *

		private TypeName ParseTypeName(bool allowArrayType, bool allowOpenType = false)
		{
			var Start = Peek();
			TypeName Result = null;
			var Types = default(IntrinsicType);

			switch (Start.Type)
			{
				case TokenType.Boolean:
					{
						Types = IntrinsicType.Boolean;
						break;
					}

				case TokenType.SByte:
					{
						Types = IntrinsicType.SByte;
						break;
					}

				case TokenType.Byte:
					{
						Types = IntrinsicType.Byte;
						break;
					}

				case TokenType.Short:
					{
						Types = IntrinsicType.Short;
						break;
					}

				case TokenType.UShort:
					{
						Types = IntrinsicType.UShort;
						break;
					}

				case TokenType.Integer:
					{
						Types = IntrinsicType.Integer;
						break;
					}

				case TokenType.UInteger:
					{
						Types = IntrinsicType.UInteger;
						break;
					}

				case TokenType.Long:
					{
						Types = IntrinsicType.Long;
						break;
					}

				case TokenType.ULong:
					{
						Types = IntrinsicType.ULong;
						break;
					}

				case TokenType.Decimal:
					{
						Types = IntrinsicType.Decimal;
						break;
					}

				case TokenType.Single:
					{
						Types = IntrinsicType.Single;
						break;
					}

				case TokenType.Double:
					{
						Types = IntrinsicType.Double;
						break;
					}

				case TokenType.Date:
					{
						Types = IntrinsicType.Date;
						break;
					}

				case TokenType.Char:
					{
						Types = IntrinsicType.Char;
						break;
					}

				case TokenType.String:
					{
						Types = IntrinsicType.String;
						break;
					}

				case TokenType.Object:
					{
						Types = IntrinsicType.Object;
						break;
					}

				case TokenType.Identifier:
				case TokenType.Global:
					{
						Result = ParseNamedTypeName(true, allowOpenType);
						break;
					}

				default:
					{
						ReportSyntaxError(SyntaxErrorType.ExpectedType, Start);
						Result = NamedTypeName.GetBadNamedType(SpanFrom(Start));
						break;
					}
			}

			if (Result is null)
			{
				Read();
				Result = new IntrinsicTypeName(Types, Start.Span);
			}

			if (allowArrayType && Peek().Type == TokenType.LeftParenthesis)
			{
				return ParseArrayTypeName(Start, Result, false, false);
			}
			else
			{
				return Result;
			}
		}

		private NamedTypeName ParseNamedTypeName(bool allowGlobal, bool allowOpenType = false)
		{
			var Start = Peek();
			var Name = ParseName(allowGlobal);

			if (Peek().Type == TokenType.LeftParenthesis)
			{
				var LeftParenthesis = Read();

				if (Peek().Type == TokenType.Of)
				{
					return new ConstructedTypeName(Name, ParseTypeArguments(LeftParenthesis, allowOpenType), SpanFrom(Start));
				}

				Backtrack(LeftParenthesis);
			}

			return new NamedTypeName(Name, Name.Span);
		}

		private TypeArgumentCollection ParseTypeArguments(Token leftParenthesis, bool allowOpenType = false)
		{
			var Start = leftParenthesis;
			Location OfLocation;
			var TypeArguments = new List<TypeName>();
			var CommaLocations = new List<Location>();
			Location RightParenthesisLocation;
			bool OpenType = false;

			Debug.Assert(Peek().Type == TokenType.Of);
			OfLocation = ReadLocation();

			if ((Peek().Type == TokenType.Comma || Peek().Type == TokenType.RightParenthesis) && allowOpenType)
			{
				OpenType = true;
			}

			if (!OpenType || Peek().Type != TokenType.RightParenthesis)
			{
				do
				{
					TypeName TypeArgument;

					if (TypeArguments.Count > 0 || OpenType)
					{
						CommaLocations.Add(ReadLocation());
					}

					if (!OpenType)
					{
						TypeArgument = ParseTypeName(true);

						if (ErrorInConstruct)
						{
							ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
						}

						TypeArguments.Add(TypeArgument);
					}
				}
				while (ArrayBoundsContinue());
			}

			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);
			return new TypeArgumentCollection(OfLocation, TypeArguments, CommaLocations, RightParenthesisLocation, SpanFrom(Start));
		}

		private bool ArrayBoundsContinue()
		{
			var NextToken = Peek();

			if (NextToken.Type == TokenType.Comma)
			{
				return true;
			}
			else if (NextToken.Type == TokenType.RightParenthesis || MustEndStatement(NextToken))
			{
				return false;
			}

			ReportSyntaxError(SyntaxErrorType.ArgumentSyntax, NextToken);
			ResyncAt(TokenType.Comma, TokenType.RightParenthesis);

			if (Peek().Type == TokenType.Comma)
			{
				ErrorInConstruct = false;
				return true;
			}

			return false;
		}

		private ArrayTypeName ParseArrayTypeName(Token startType, TypeName elementType, bool allowExplicitSizes, bool innerArrayType)
		{
			var ArgumentsStart = Peek();
			List<Argument> Arguments;
			var CommaLocations = new List<Location>();
			Location RightParenthesisLocation;
			ArgumentCollection ArgumentCollection;

			Debug.Assert(Peek().Type == TokenType.LeftParenthesis);

			if (startType is null)
			{
				startType = Peek();
			}

			Read();

			if (Peek().Type == TokenType.RightParenthesis || Peek().Type == TokenType.Comma)
			{
				Arguments = null;

				while (Peek().Type == TokenType.Comma)
					CommaLocations.Add(ReadLocation());
			}
			else
			{
				Token SizeStart;
				Expression Size;

				if (!allowExplicitSizes)
				{
					if (innerArrayType)
					{
						ReportSyntaxError(SyntaxErrorType.NoConstituentArraySizes, Peek());
					}
					else
					{
						ReportSyntaxError(SyntaxErrorType.NoExplicitArraySizes, Peek());
					}
				}

				Arguments = new List<Argument>();

				do
				{
					if (Arguments.Count > 0)
					{
						CommaLocations.Add(ReadLocation());
					}

					SizeStart = Peek();
					Size = ParseExpression(Scanner.Version > LanguageVersion.VisualBasic71);

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.Comma, TokenType.RightParenthesis, TokenType.As);
					}

					Arguments.Add(new Argument(null, default, Size, SpanFrom(SizeStart)));
				}
				while (ArrayBoundsContinue());
			}

			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);
			ArgumentCollection = new ArgumentCollection(Arguments, CommaLocations, RightParenthesisLocation, SpanFrom(ArgumentsStart));

			if (Peek().Type == TokenType.LeftParenthesis)
			{
				elementType = ParseArrayTypeName(Peek(), elementType, false, true);
			}

			return new ArrayTypeName(elementType, CommaLocations.Count + 1, ArgumentCollection, SpanFrom(startType));
		}

		// *
		// * Initializers
		// *

		private Initializer ParseInitializer()
		{
			if (Peek().Type == TokenType.LeftCurlyBrace)
			{
				return ParseAggregateInitializer();
			}
			else
			{
				var Expression = ParseExpression();
				return new ExpressionInitializer(Expression, Expression.Span);
			}
		}

		private bool InitializersContinue()
		{
			var NextToken = Peek();

			if (NextToken.Type == TokenType.Comma)
			{
				return true;
			}
			else if (NextToken.Type == TokenType.RightCurlyBrace || MustEndStatement(NextToken))
			{
				return false;
			}

			ReportSyntaxError(SyntaxErrorType.InitializerSyntax, NextToken);
			ResyncAt(TokenType.Comma, TokenType.RightCurlyBrace);

			if (Peek().Type == TokenType.Comma)
			{
				ErrorInConstruct = false;
				return true;
			}

			return false;
		}

		private AggregateInitializer ParseAggregateInitializer()
		{
			var Start = Peek();
			var Initializers = new List<Initializer>();
			Location RightBracketLocation;
			var CommaLocations = new List<Location>();

			Debug.Assert(Start.Type == TokenType.LeftCurlyBrace);
			Read();

			if (Peek().Type != TokenType.RightCurlyBrace)
			{
				do
				{
					if (Initializers.Count > 0)
					{
						CommaLocations.Add(ReadLocation());
					}

					Initializers.Add(ParseInitializer());

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.Comma, TokenType.RightCurlyBrace);
					}
				}
				while (InitializersContinue());
			}

			RightBracketLocation = VerifyExpectedToken(TokenType.RightCurlyBrace);

			return new AggregateInitializer(new InitializerCollection(Initializers, CommaLocations, RightBracketLocation, SpanFrom(Start)), SpanFrom(Start));
		}

		// *
		// * Arguments
		// *

		private Argument ParseArgument(ref bool foundNamedArgument)
		{
			var Start = Read();
			Expression Value;
			SimpleName Name;
			var ColonEqualsLocation = default(Location);

			if (Peek().Type == TokenType.ColonEquals)
			{
				if (!foundNamedArgument)
				{
					foundNamedArgument = true;
				}
			}
			else if (foundNamedArgument)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedNamedArgument, Start);
				foundNamedArgument = false;
			}

			Backtrack(Start);

			if (foundNamedArgument)
			{
				Name = ParseSimpleName(true);
				ColonEqualsLocation = ReadLocation();
				Value = ParseExpression();
			}
			else
			{
				Name = null;
				Value = ParseExpression();
			}

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma, TokenType.RightParenthesis);

				if (Peek().Type == TokenType.Comma)
				{
					ErrorInConstruct = false;
				}
			}

			return new Argument(Name, ColonEqualsLocation, Value, SpanFrom(Start));
		}

		private bool ArgumentsContinue()
		{
			var NextToken = Peek();

			if (NextToken.Type == TokenType.Comma)
			{
				return true;
			}
			else if (NextToken.Type == TokenType.RightParenthesis || MustEndStatement(NextToken))
			{
				return false;
			}
			// LC Line If can end with "End If"
			else if (NextToken.Type == TokenType.End && CurrentBlockContextType() == TreeType.LineIfBlockStatement)
			{
				return false;
			}

			ReportSyntaxError(SyntaxErrorType.ArgumentSyntax, NextToken);
			ResyncAt(TokenType.Comma, TokenType.RightParenthesis);

			if (Peek().Type == TokenType.Comma)
			{
				ErrorInConstruct = false;
				return true;
			}

			return false;
		}
		// LC Made requireParenthesis optional
		private ArgumentCollection ParseArguments(bool requireParenthesis = true)
		{
			var Start = Peek();
			var Arguments = new List<Argument>();
			var CommaLocations = new List<Location>();
			var RightParenthesisLocation = default(Location);

			if (Start.Type != TokenType.LeftParenthesis)
			{
				if (requireParenthesis)
				{
					return null;
				}
			}
			else
			{
				requireParenthesis = true; // If found left, then right is required to balance
				Read();
			}

			if (Peek().Type != TokenType.RightParenthesis)
			{
				bool FoundNamedArgument = false;
				Token ArgumentStart;
				Argument Argument;

				do
				{
					if (Arguments.Count > 0)
					{
						CommaLocations.Add(ReadLocation());
					}

					ArgumentStart = Peek();

					if (ArgumentStart.Type == TokenType.Comma || ArgumentStart.Type == TokenType.RightParenthesis)
					{
						if (FoundNamedArgument)
						{
							ReportSyntaxError(SyntaxErrorType.ExpectedNamedArgument, Peek());
						}

						Argument = null;
					}
					else
					{
						Argument = ParseArgument(ref FoundNamedArgument);
					}

					Arguments.Add(Argument);
				}
				while (ArgumentsContinue());
			}

			if (Peek().Type == TokenType.RightParenthesis)
			{
				RightParenthesisLocation = ReadLocation();
			}
			else if (requireParenthesis)
			{
				var Current = Peek();

				// On error, peek for ")" with "(". If ")" seen before 
				// "(", then sync on that. Otherwise, assume missing ")"
				// and let caller decide.
				ResyncAt(TokenType.LeftParenthesis, TokenType.RightParenthesis);

				if (Peek().Type == TokenType.RightParenthesis)
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
					RightParenthesisLocation = ReadLocation();
				}
				else
				{
					Backtrack(Current);
					ReportSyntaxError(SyntaxErrorType.ExpectedRightParenthesis, Peek());
				}
			}
			else
			{
				RightParenthesisLocation = Peek().Span.Start;
			} // LC No ")". Just give it a dummy
			return new ArgumentCollection(Arguments, CommaLocations, RightParenthesisLocation, SpanFrom(Start));
		}

		// *
		// * Expressions
		// *

		private LiteralExpression ParseLiteralExpression()
		{
			var Start = Read();

			switch (Start.Type)
			{
				case TokenType.True:
				case TokenType.False:
					{
						return new BooleanLiteralExpression(Start.Type == TokenType.True, Start.Span);
					}

				case TokenType.IntegerLiteral:
					{
						IntegerLiteralToken Literal = (IntegerLiteralToken)Start;
						return new IntegerLiteralExpression(Literal.Literal, Literal.IntegerBase, Literal.TypeCharacter, Literal.Span);
					}

				case TokenType.FloatingPointLiteral:
					{
						FloatingPointLiteralToken Literal = (FloatingPointLiteralToken)Start;
						return new FloatingPointLiteralExpression(Literal.Literal, Literal.TypeCharacter, Literal.Span);
					}

				case TokenType.DecimalLiteral:
					{
						DecimalLiteralToken Literal = (DecimalLiteralToken)Start;
						return new DecimalLiteralExpression(Literal.Literal, Literal.TypeCharacter, Literal.Span);
					}

				case TokenType.CharacterLiteral:
					{
						CharacterLiteralToken Literal = (CharacterLiteralToken)Start;
						return new CharacterLiteralExpression(Literal.Literal, Literal.Span);
					}

				case TokenType.StringLiteral:
					{
						StringLiteralToken Literal = (StringLiteralToken)Start;
						return new StringLiteralExpression(Literal.Literal, Literal.Span);
					}

				case TokenType.DateLiteral:
					{
						DateLiteralToken Literal = (DateLiteralToken)Start;
						return new DateLiteralExpression(Literal.Literal, Literal.Span);
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			return null;
		}

		private Expression ParseCastExpression()
		{
			var Start = Read();
			IntrinsicType OperatorType;
			Expression Operand;
			Location LeftParenthesisLocation;
			Location RightParenthesisLocation;

			switch (Start.Type)
			{
				case TokenType.CBool:
					{
						OperatorType = IntrinsicType.Boolean;
						break;
					}

				case TokenType.CChar:
					{
						OperatorType = IntrinsicType.Char;
						break;
					}

				case TokenType.CDate:
					{
						OperatorType = IntrinsicType.Date;
						break;
					}

				case TokenType.CDbl:
					{
						OperatorType = IntrinsicType.Double;
						break;
					}

				case TokenType.CByte:
					{
						OperatorType = IntrinsicType.Byte;
						break;
					}

				case TokenType.CShort:
					{
						OperatorType = IntrinsicType.Short;
						break;
					}

				case TokenType.CInt:
					{
						OperatorType = IntrinsicType.Integer;
						break;
					}

				case TokenType.CLng:
					{
						OperatorType = IntrinsicType.Long;
						break;
					}

				case TokenType.CSng:
					{
						OperatorType = IntrinsicType.Single;
						break;
					}

				case TokenType.CStr:
					{
						OperatorType = IntrinsicType.String;
						break;
					}

				case TokenType.CDec:
					{
						OperatorType = IntrinsicType.Decimal;
						break;
					}

				case TokenType.CObj:
					{
						OperatorType = IntrinsicType.Object;
						break;
					}

				case TokenType.CSByte:
					{
						OperatorType = IntrinsicType.SByte;
						break;
					}

				case TokenType.CUShort:
					{
						OperatorType = IntrinsicType.UShort;
						break;
					}

				case TokenType.CUInt:
					{
						OperatorType = IntrinsicType.UInteger;
						break;
					}

				case TokenType.CULng:
					{
						OperatorType = IntrinsicType.ULong;
						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						return null;
					}
			}

			LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis);
			Operand = ParseExpression();
			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);

			return new IntrinsicCastExpression(OperatorType, LeftParenthesisLocation, Operand, RightParenthesisLocation, SpanFrom(Start));
		}

		private Expression ParseCTypeExpression()
		{
			var Start = Read();
			Expression Operand;
			TypeName Target;
			Location LeftParenthesisLocation;
			Location RightParenthesisLocation;
			Location CommaLocation;

			Debug.Assert(Start.Type == TokenType.CType || Start.Type == TokenType.DirectCast || Start.Type == TokenType.TryCast);

			LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis);
			Operand = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
			}

			CommaLocation = VerifyExpectedToken(TokenType.Comma);
			Target = ParseTypeName(true);
			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);

			if (Start.Type == TokenType.CType)
			{
				return new CTypeExpression(LeftParenthesisLocation, Operand, CommaLocation, Target, RightParenthesisLocation, SpanFrom(Start));
			}
			else if (Start.Type == TokenType.DirectCast)
			{
				return new DirectCastExpression(LeftParenthesisLocation, Operand, CommaLocation, Target, RightParenthesisLocation, SpanFrom(Start));
			}
			else
			{
				return new TryCastExpression(LeftParenthesisLocation, Operand, CommaLocation, Target, RightParenthesisLocation, SpanFrom(Start));
			}
		}

		private Expression ParseInstanceExpression()
		{
			var Start = Read();
			var InstanceType = default(InstanceType);

			switch (Start.Type)
			{
				case TokenType.Me:
					{
						InstanceType = VBScript.Parser.InstanceType.Me;
						break;
					}

				case TokenType.MyClass:
					{
						InstanceType = VBScript.Parser.InstanceType.MyClass;

						if (Peek().Type != TokenType.Period)
						{
							ReportSyntaxError(SyntaxErrorType.ExpectedPeriodAfterMyClass, Start);
						}

						break;
					}

				case TokenType.MyBase:
					{
						InstanceType = VBScript.Parser.InstanceType.MyBase;

						if (Peek().Type != TokenType.Period)
						{
							ReportSyntaxError(SyntaxErrorType.ExpectedPeriodAfterMyBase, Start);
						}

						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}

			return new InstanceExpression(InstanceType, Start.Span);
		}

		private Expression ParseParentheticalExpression()
		{
			Expression Operand;
			var Start = Read();
			Location RightParenthesisLocation;

			Debug.Assert(Start.Type == TokenType.LeftParenthesis);

			Operand = ParseExpression();
			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);

			return new ParentheticalExpression(Operand, RightParenthesisLocation, SpanFrom(Start));
		}

		private Expression ParseSimpleNameExpression()
		{
			var Start = Peek();
			return new SimpleNameExpression(ParseSimpleName(false), SpanFrom(Start));
		}

		private Expression ParseDotBangExpression(Token start, Expression terminal)
		{
			SimpleName Name;
			Token DotBang;

			Debug.Assert(Peek().Type == TokenType.Period || Peek().Type == TokenType.Exclamation);

			DotBang = Read();
			Name = ParseSimpleName(true);

			if (DotBang.Type == TokenType.Period)
			{
				return new QualifiedExpression(terminal, DotBang.Span.Start, Name, SpanFrom(start));
			}
			else
			{
				return new DictionaryLookupExpression(terminal, DotBang.Span.Start, Name, SpanFrom(start));
			}
		}

		private Expression ParseCallOrIndexExpression(Token start, Expression terminal)
		{
			ArgumentCollection Arguments;

			// Because parentheses are used for array indexing, parameter passing, and array
			// declaring (via the Redim statement), there is some ambiguity about how to handle
			// a parenthesized list that begins with an expression. The most general case is to
			// parse it as an argument list.

			Arguments = ParseArguments();
			return new CallOrIndexExpression(terminal, Arguments, SpanFrom(start));
		}

		private Expression ParseTypeOfExpression()
		{
			var Start = Peek();
			Expression Value;
			TypeName Target;
			Location IsLocation;

			Debug.Assert(Start.Type == TokenType.TypeOf);

			Read();
			Value = ParseBinaryOperatorExpression(PrecedenceLevel.Relational);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Is);
			}

			IsLocation = VerifyExpectedToken(TokenType.Is);
			Target = ParseTypeName(true);

			return new TypeOfExpression(Value, IsLocation, Target, SpanFrom(Start));
		}

		private Expression ParseGetTypeExpression()
		{
			var Start = Read();
			TypeName Target;
			Location LeftParenthesisLocation;
			Location RightParenthesisLocation;

			Debug.Assert(Start.Type == TokenType.GetType);
			LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis);
			Target = ParseTypeName(true, true);
			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);

			return new GetTypeExpression(LeftParenthesisLocation, Target, RightParenthesisLocation, SpanFrom(Start));
		}

		private Expression ParseNewExpression()
		{
			var Start = Read();
			Token TypeStart;
			TypeName Type;
			ArgumentCollection Arguments;
			Token ArgumentsStart;

			Debug.Assert(Start.Type == TokenType.New);

			TypeStart = Peek();
			Type = ParseTypeName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis);
			}

			ArgumentsStart = Peek();

			// This is an ambiguity in the grammar between
			// 
			// New <Type> ( <Arguments> )
			// New <Type> <ArrayDeclaratorList> <AggregateInitializer>
			// 
			// Try it as the first form, and if this fails, try the second.
			// (All valid instances of the second form have a beginning that is a valid
			// instance of the first form, so no spurious errors should result.)

			Arguments = ParseArguments();

			if ((Peek().Type == TokenType.LeftCurlyBrace || Peek().Type == TokenType.LeftParenthesis) && Arguments is not null)
			{
				ArrayTypeName ArrayType;

				// Treat this as the form of New expression that allocates an array.
				Backtrack(ArgumentsStart);
				ArrayType = ParseArrayTypeName(TypeStart, Type, true, false);

				if (Peek().Type == TokenType.LeftCurlyBrace)
				{
					var Initializer = ParseAggregateInitializer();
					return new NewAggregateExpression(ArrayType, Initializer, SpanFrom(Start));
				}
				else
				{
					HandleUnexpectedToken(TokenType.LeftCurlyBrace);
				}
			}

			return new NewExpression(Type, Arguments, SpanFrom(Start));
		}

		private Expression ParseTerminalExpression()
		{
			var Start = Peek();
			Expression Terminal;

			switch (Start.Type)
			{
				case TokenType.Identifier:
					{
						Terminal = ParseSimpleNameExpression();
						break;
					}

				case TokenType.IntegerLiteral:
				case TokenType.FloatingPointLiteral:
				case TokenType.DecimalLiteral:
				case TokenType.CharacterLiteral:
				case TokenType.StringLiteral:
				case TokenType.DateLiteral:
				case TokenType.True:
				case TokenType.False:
					{
						Terminal = ParseLiteralExpression();
						break;
					}

				case TokenType.CBool:
				case TokenType.CByte:
				case TokenType.CShort:
				case TokenType.CInt:
				case TokenType.CLng:
				case TokenType.CDec:
				case TokenType.CSng:
				case TokenType.CDbl:
				case TokenType.CChar:
				case TokenType.CStr:
				case TokenType.CDate:
				case TokenType.CObj:
				case TokenType.CSByte:
				case TokenType.CUShort:
				case TokenType.CUInt:
				case TokenType.CULng:
					{
						Terminal = ParseCastExpression();
						break;
					}

				case TokenType.DirectCast:
				case TokenType.CType:
				case TokenType.TryCast:
					{
						Terminal = ParseCTypeExpression();
						break;
					}

				case TokenType.Me:
				case TokenType.MyBase:
				case TokenType.MyClass:
					{
						Terminal = ParseInstanceExpression();
						break;
					}

				case TokenType.Global:
					{
						Terminal = new GlobalExpression(Read().Span);

						if (Peek().Type != TokenType.Period)
						{
							ReportSyntaxError(SyntaxErrorType.ExpectedPeriodAfterGlobal, Start);
						}

						break;
					}

				case TokenType.Nothing:
					{
						Terminal = new NothingExpression(Read().Span);
						break;
					}

				case TokenType.LeftParenthesis:
					{
						Terminal = ParseParentheticalExpression();
						break;
					}

				case TokenType.Period:
				case TokenType.Exclamation:
					{
						Terminal = ParseDotBangExpression(Start, null);
						break;
					}

				case TokenType.TypeOf:
					{
						Terminal = ParseTypeOfExpression();
						break;
					}

				case TokenType.GetType:
					{
						Terminal = ParseGetTypeExpression();
						break;
					}

				case TokenType.New:
					{
						Terminal = ParseNewExpression();
						break;
					}

				case TokenType.Short:
				case TokenType.Integer:
				case TokenType.Long:
				case TokenType.Decimal:
				case TokenType.Single:
				case TokenType.Double:
				case TokenType.Byte:
				case TokenType.Boolean:
				case TokenType.Char:
				case TokenType.Date:
				case TokenType.String:
				case TokenType.Object:
					{
						// Dim ReferencedType As TypeName = ParseTypeName(False)

						// Terminal = New TypeReferenceExpression(ReferencedType, ReferencedType.Span)
						// LC Parse the terminal as SimpleName Expression rather than TypeReferenceExpression
						Terminal = new SimpleNameExpression(ParseSimpleName(true), SpanFrom(Start));

						if (Scanner.Peek().Type == TokenType.Period)
						{
							Terminal = ParseDotBangExpression(Start, Terminal);
							// LC commented out the following to allow keyword to be used as a function
							// Else
							// HandleUnexpectedToken(TokenType.Period)
						}

						break;
					}

				default:
					{
						ReportSyntaxError(SyntaxErrorType.ExpectedExpression, Peek());
						Terminal = Expression.GetBadExpression(SpanFrom(Peek(), Peek()));
						break;
					}
			}

			// Valid suffixes are ".", "!", and "(". Everything else is considered
			// to end the term.

			while (true)
			{
				var NextToken = Peek();

				if (NextToken.Type == TokenType.Period || NextToken.Type == TokenType.Exclamation)
				{
					Terminal = ParseDotBangExpression(Start, Terminal);
				}
				else if (NextToken.Type == TokenType.LeftParenthesis)
				{
					var LeftParenthesis = Read();

					if (Peek().Type == TokenType.Of)
					{
						return new GenericQualifiedExpression(Terminal, ParseTypeArguments(LeftParenthesis, false), SpanFrom(Start));
					}
					else
					{
						Backtrack(LeftParenthesis);
						Terminal = ParseCallOrIndexExpression(Start, Terminal);
					}
				}
				else
				{
					break;
				}
			}

			return Terminal;
		}

		private Expression ParseUnaryOperatorExpression()
		{
			var Start = Peek();
			Expression Operand;
			OperatorType OperatorType;

			switch (Start.Type)
			{
				case TokenType.Minus:
					{
						OperatorType = VBScript.Parser.OperatorType.Negate;
						break;
					}

				case TokenType.Plus:
					{
						OperatorType = VBScript.Parser.OperatorType.UnaryPlus;
						break;
					}

				case TokenType.Not:
					{
						OperatorType = VBScript.Parser.OperatorType.Not;
						break;
					}

				case TokenType.AddressOf:
					{
						Read();
						Operand = ParseBinaryOperatorExpression(PrecedenceLevel.None);
						return new AddressOfExpression(Operand, SpanFrom(Start, Operand));
					}

				default:
					{
						return ParseTerminalExpression();
					}
			}

			Read();
			Operand = ParseBinaryOperatorExpression(GetOperatorPrecedence(OperatorType));
			return new UnaryOperatorExpression(OperatorType, Operand, SpanFrom(Start, Operand));
		}

		private Expression ParseBinaryOperatorExpression(PrecedenceLevel pendingPrecedence, bool allowRange = false)
		{
			Expression Expression;
			var Start = Peek();

			Expression = ParseUnaryOperatorExpression();

			// Parse operators that follow the term according to precedence.
			while (true)
			{
				var OperatorType = GetBinaryOperator(Peek().Type, allowRange);
				Expression Right;
				PrecedenceLevel Precedence;
				Location OperatorLocation;

				if (OperatorType == VBScript.Parser.OperatorType.None)
				{
					break;
				}

				Precedence = GetOperatorPrecedence(OperatorType);

				// Only continue parsing if precedence is high enough
				if (Precedence <= pendingPrecedence)
				{
					break;
				}

				OperatorLocation = ReadLocation();
				Right = ParseBinaryOperatorExpression(Precedence);
				Expression = new BinaryOperatorExpression(Expression, OperatorType, OperatorLocation, Right, SpanFrom(Start, Right));
			}

			return Expression;
		}

		private ExpressionCollection ParseExpressionList(bool requireExpression = false)
		{
			var Start = Peek();
			var Expressions = new List<Expression>();
			var CommaLocations = new List<Location>();

			if (CanEndStatement(Start) && !requireExpression)
			{
				return null;
			}

			do
			{
				if (Expressions.Count > 0)
				{
					CommaLocations.Add(ReadLocation());
				}

				Expressions.Add(ParseExpression());

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Comma);
				}
			}
			while (Peek().Type == TokenType.Comma);

			if (Expressions.Count == 0 && CommaLocations.Count == 0)
			{
				return null;
			}
			else
			{
				return new ExpressionCollection(Expressions, CommaLocations, SpanFrom(Start));
			}
		}

		private Expression ParseExpression(bool allowRange = false)
		{
			return ParseBinaryOperatorExpression(PrecedenceLevel.None, allowRange);
		}

		// *
		// * Statements
		// *

		private Statement ParseExpressionStatement(TreeType @type, bool operandOptional)
		{
			var Start = Peek();
			Expression Operand = null;

			Read();

			if (!operandOptional || !CanEndStatement(Peek()))
			{
				Operand = ParseExpression();
			}

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			switch (type)
			{
				case TreeType.ReturnStatement:
					{
						return new ReturnStatement(Operand, SpanFrom(Start), ParseTrailingComments());
					}

				case TreeType.ErrorStatement:
					{
						return new ErrorStatement(Operand, SpanFrom(Start), ParseTrailingComments());
					}

				case TreeType.ThrowStatement:
					{
						return new ThrowStatement(Operand, SpanFrom(Start), ParseTrailingComments());
					}

				default:
					{
						Debug.Assert(false, "Unexpected!");
						return null;
					}
			}
		}

		// Parse a reference to a label, which can be an identifier or a line number.
		private void ParseLabelReference(ref SimpleName name, ref bool isLineNumber)
		{
			var Start = Peek();

			if (Start.Type == TokenType.Identifier)
			{
				name = ParseSimpleName(false);
				isLineNumber = false;
			}
			else if (Start.Type == TokenType.IntegerLiteral)
			{
				IntegerLiteralToken IntegerLiteral = (IntegerLiteralToken)Start;

				if (IntegerLiteral.TypeCharacter != TypeCharacter.None)
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Start);
				}

				name = new SimpleName(IntegerLiteral.Literal.ToString(), TypeCharacter.None, false, IntegerLiteral.Span);
				isLineNumber = true;
				Read();
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Start);
				name = SimpleName.GetBadSimpleName(SpanFrom(Start));
				isLineNumber = false;
			}
		}

		private Statement ParseGotoStatement()
		{
			var Start = Peek();
			SimpleName Name = null;
			var IsLineNumber = default(bool);

			Read();
			ParseLabelReference(ref Name, ref IsLineNumber);

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			return new GotoStatement(Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseContinueStatement()
		{
			var Start = Peek();
			BlockType ContinueType;
			var ContinueArgumentLocation = default(Location);

			Read();

			ContinueType = GetContinueType(Peek().Type);

			if (ContinueType == BlockType.None)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedContinueKind, Peek());
				ResyncAt();
			}
			else
			{
				ContinueArgumentLocation = ReadLocation();
			}

			return new ContinueStatement(ContinueType, ContinueArgumentLocation, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseExitStatement()
		{
			var Start = Peek();
			BlockType ExitType;
			var ExitArgumentLocation = default(Location);

			Read();

			ExitType = GetExitType(Peek().Type);

			if (ExitType == BlockType.None)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedExitKind, Peek());
				ResyncAt();
			}
			else
			{
				ExitArgumentLocation = ReadLocation();
			}

			return new ExitStatement(ExitType, ExitArgumentLocation, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseEndStatement()
		{
			var Start = Read();
			var EndType = GetBlockType(Peek().Type);

			if (EndType == BlockType.None)
			{
				return new EndStatement(SpanFrom(Start), ParseTrailingComments());
			}

			return new EndBlockStatement(EndType, ReadLocation(), SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseWendStatement()
		{
			var Start = Read();
			return new EndBlockStatement(BlockType.While, Start.Span.Finish, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseRaiseEventStatement()
		{
			var Start = Peek();
			SimpleName Name;
			ArgumentCollection Arguments;

			Read();
			Name = ParseSimpleName(true);

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			Arguments = ParseArguments();

			return new RaiseEventStatement(Name, Arguments, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseHandlerStatement()
		{
			var Start = Peek();
			Expression EventName;
			Expression DelegateExpression;
			Location CommaLocation;

			Read();
			EventName = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma);
			}

			CommaLocation = VerifyExpectedToken(TokenType.Comma);
			DelegateExpression = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			if (Start.Type == TokenType.AddHandler)
			{
				return new AddHandlerStatement(EventName, CommaLocation, DelegateExpression, SpanFrom(Start), ParseTrailingComments());
			}
			else
			{
				return new RemoveHandlerStatement(EventName, CommaLocation, DelegateExpression, SpanFrom(Start), ParseTrailingComments());
			}
		}

		private Statement ParseOnErrorStatement()
		{
			var Start = Read();
			OnErrorType OnErrorType = default;
			Token NextToken;
			SimpleName Name = null;
			var IsLineNumber = default(bool);
			var ErrorLocation = default(Location);
			var ResumeOrGoToLocation = default(Location);
			var NextOrZeroOrMinusLocation = default(Location);
			var OneLocation = default(Location);

			if (Peek().Type == TokenType.Error)
			{
				ErrorLocation = ReadLocation();
				NextToken = Peek();

				if (NextToken.Type == TokenType.Resume)
				{
					ResumeOrGoToLocation = ReadLocation();

					if (Peek().Type == TokenType.Next)
					{
						NextOrZeroOrMinusLocation = ReadLocation();
					}
					else
					{
						ReportSyntaxError(SyntaxErrorType.ExpectedNext, Peek());
					}

					OnErrorType = VBScript.Parser.OnErrorType.Next;
				}
				else if (NextToken.Type == TokenType.GoTo)
				{
					ResumeOrGoToLocation = ReadLocation();
					NextToken = Peek();
					bool doLabel = true;
					if (NextToken.Type == TokenType.IntegerLiteral && ((IntegerLiteralToken)NextToken).Literal == 0)
					{
						NextOrZeroOrMinusLocation = ReadLocation();
						OnErrorType = VBScript.Parser.OnErrorType.Zero;
						doLabel = false;
					}
					else if (NextToken.Type == TokenType.Minus)
					{
						Token NextNextToken;

						NextOrZeroOrMinusLocation = ReadLocation();
						NextNextToken = Peek();

						if (NextNextToken.Type == TokenType.IntegerLiteral && ((IntegerLiteralToken)NextNextToken).Literal == 1)
						{
							OneLocation = ReadLocation();
							OnErrorType = VBScript.Parser.OnErrorType.MinusOne;
							doLabel = false;
						}
						else
						{
							Backtrack(NextToken);
						}
					}
					
					if (doLabel)
					{
						OnErrorType = VBScript.Parser.OnErrorType.Label;
						ParseLabelReference(ref Name, ref IsLineNumber);

						if (ErrorInConstruct)
						{
							ResyncAt();
						}
					}
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedResumeOrGoto, Peek());
					OnErrorType = VBScript.Parser.OnErrorType.Bad;
					ResyncAt();
				}
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedError, Peek());
				OnErrorType = VBScript.Parser.OnErrorType.Bad;
				ResyncAt();
			}

			return new OnErrorStatement(OnErrorType, ErrorLocation, ResumeOrGoToLocation, NextOrZeroOrMinusLocation, OneLocation, Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseResumeStatement()
		{
			var Start = Read();
			var ResumeType = VBScript.Parser.ResumeType.None;
			SimpleName Name = null;
			var IsLineNumber = default(bool);
			var NextLocation = default(Location);

			if (!CanEndStatement(Peek()))
			{
				if (Peek().Type == TokenType.Next)
				{
					NextLocation = ReadLocation();
					ResumeType = VBScript.Parser.ResumeType.Next;
				}
				else
				{
					ParseLabelReference(ref Name, ref IsLineNumber);

					if (ErrorInConstruct)
					{
						ResumeType = VBScript.Parser.ResumeType.None;
					}
					else
					{
						ResumeType = VBScript.Parser.ResumeType.Label;
					}
				}
			}

			return new ResumeStatement(ResumeType, NextLocation, Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseReDimStatement()
		{
			var Start = Read();
			var PreserveLocation = default(Location);
			ExpressionCollection Variables;

			if (Peek().AsUnreservedKeyword() == TokenType.Preserve)
			{
				PreserveLocation = ReadLocation();
			}

			Variables = ParseExpressionList(true);

			return new ReDimStatement(PreserveLocation, Variables, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseEraseStatement()
		{
			var Start = Read();
			var Variables = ParseExpressionList(true);

			return new EraseStatement(Variables, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseCallStatement(Expression target = null)
		{
			var Start = Peek();
			Location StartLocation;
			var CallLocation = default(Location);
			ArgumentCollection Arguments = null;

			if (target is null)
			{
				StartLocation = Start.Span.Start;

				if (Start.Type == TokenType.Call)
				{
					CallLocation = ReadLocation();
				}

				target = ParseExpression();

				if (ErrorInConstruct)
				{
					ResyncAt();
				}
			}
			else
			{
				StartLocation = target.Span.Start;
			}

			if (target.Type == TreeType.CallOrIndexExpression)
			{
				// Extract the operands of the call/index expression and make
				// them operands of the call statement.
				CallOrIndexExpression CallOrIndexExpression = (CallOrIndexExpression)target;

				target = CallOrIndexExpression.TargetExpression;
				Arguments = CallOrIndexExpression.Arguments;
			}
			else if (!MustEndStatement(Peek())) // LC Allow calls like response.write "hello"
			{
				Arguments = ParseArguments(false);
			}

			return new CallStatement(CallLocation, target, Arguments, SpanFrom(StartLocation), ParseTrailingComments());
		}

		private Statement ParseMidAssignmentStatement()
		{
			var Start = Read();
			IdentifierToken Identifier = (IdentifierToken)Start;
			var HasTypeCharacter = default(bool);
			Location LeftParenthesisLocation;
			Expression Target;
			Location StartCommaLocation;
			Expression StartExpression;
			Location LengthCommaLocation = default;
			Expression LengthExpression = null;
			Location RightParenthesisLocation;
			Location OperatorLocation;
			Expression Source;

			if (Identifier.TypeCharacter == TypeCharacter.StringSymbol)
			{
				HasTypeCharacter = true;
			}
			else if (Identifier.TypeCharacter != TypeCharacter.None)
			{
				goto NotMidAssignment;
			}

			if (Peek().Type == TokenType.LeftParenthesis)
			{
				LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis);
			}
			else
			{
				goto NotMidAssignment;
			}

			// This is very unfortunate: ideally, we would continue parsing to
			// make sure the entire statement matched the form of a Mid assignment
			// statement. That way something like "Mid(10) = 5", where Mid is an
			// array identifier wouldn't cause an error. Alas, it's not that simple
			// because what about something that's in error? We could fall back on
			// error, but we have no way of backtracking on errors at the moment.
			// So we're going to do what the official compiler does: if we see
			// Mid and (, you've got yourself a Mid assignment statement!
			Target = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma);
			}

			StartCommaLocation = VerifyExpectedToken(TokenType.Comma);
			StartExpression = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
			}

			if (Peek().Type == TokenType.Comma)
			{
				LengthCommaLocation = VerifyExpectedToken(TokenType.Comma);
				LengthExpression = ParseExpression();

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.RightParenthesis);
				}
			}

			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);
			OperatorLocation = VerifyExpectedToken(TokenType.Equals);

			Source = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			return new MidAssignmentStatement(HasTypeCharacter, LeftParenthesisLocation, Target, StartCommaLocation, StartExpression, LengthCommaLocation, LengthExpression, RightParenthesisLocation, OperatorLocation, Source, SpanFrom(Start), ParseTrailingComments());

		NotMidAssignment:
			;

			Backtrack(Start);
			return null;
		}

		private Statement ParseAssignmentStatement(Expression target, bool isSetStatement)
		{
			OperatorType CompoundOperator;
			Expression Source;
			Token Operator;

			Operator = Read();
			CompoundOperator = GetCompoundAssignmentOperatorType(Operator.Type);
			Source = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			if (CompoundOperator == (int)TreeType.SyntaxError)
			{
				return new AssignmentStatement(target, Operator.Span.Start, Source, SpanFrom(target.Span.Start), ParseTrailingComments(), isSetStatement);
			}
			else
			{
				return new CompoundAssignmentStatement(CompoundOperator, target, Operator.Span.Start, Source, SpanFrom(target.Span.Start), ParseTrailingComments());
			}
		}

		private Statement ParseLocalDeclarationStatement()
		{
			var Start = Peek();
			ModifierCollection Modifiers;
			const ModifierTypes ValidModifiers = ModifierTypes.Dim | ModifierTypes.Const | ModifierTypes.Static;


			Modifiers = ParseDeclarationModifierList();
			ValidateModifierList(Modifiers, ValidModifiers);

			if (Modifiers is null)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedModifier, Peek());
			}
			else if (Modifiers.Count > 1)
			{
				ReportSyntaxError(SyntaxErrorType.InvalidVariableModifiers, Modifiers.Span);
			}

			return new LocalDeclarationStatement(Modifiers, ParseVariableDeclarators(), SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseLabelStatement()
		{
			SimpleName Name = null;
			var IsLineNumber = default(bool);
			var Start = Peek();

			ParseLabelReference(ref Name, ref IsLineNumber);
			return new LabelStatement(Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseExpressionBlockStatement(TreeType blockType)
		{
			var Start = Read();
			var Expression = ParseExpression();
			StatementCollection StatementCollection;
			Statement EndStatement = null;
			List<CommentType> Comments = null;

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			StatementCollection = ParseStatementBlock(SpanFrom(Start), blockType, ref Comments, ref EndStatement);

			switch (blockType)
			{
				case TreeType.WithBlockStatement:
					{
						return new WithBlockStatement(Expression, StatementCollection, (EndBlockStatement)EndStatement, SpanFrom(Start), Comments);
					}

				case TreeType.SyncLockBlockStatement:
					{
						return new SyncLockBlockStatement(Expression, StatementCollection, (EndBlockStatement)EndStatement, SpanFrom(Start), Comments);
					}

				case TreeType.WhileBlockStatement:
					{
						return new WhileBlockStatement(Expression, StatementCollection, (EndBlockStatement)EndStatement, SpanFrom(Start), Comments);
					}

				default:
					{
						Debug.Assert(false, "Unexpected!");
						return null;
					}
			}
		}

		private Statement ParseUsingBlockStatement()
		{
			var Start = Read();
			Expression Expression = null;
			VariableDeclaratorCollection VariableDeclarators = null;
			StatementCollection StatementCollection;
			Statement EndStatement = null;
			List<CommentType> Comments = null;
			var NextToken = PeekAheadOne();

			if (NextToken.Type == TokenType.As || NextToken.Type == TokenType.Equals)
			{
				VariableDeclarators = ParseVariableDeclarators();
			}
			else
			{
				Expression = ParseExpression();
			}

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			StatementCollection = ParseStatementBlock(SpanFrom(Start), TreeType.UsingBlockStatement, ref Comments, ref EndStatement);

			if (Expression is null)
			{
				return new UsingBlockStatement(VariableDeclarators, StatementCollection, (EndBlockStatement)EndStatement, SpanFrom(Start), Comments);
			}
			else
			{
				return new UsingBlockStatement(Expression, StatementCollection, (EndBlockStatement)EndStatement, SpanFrom(Start), Comments);
			}
		}

		private Expression ParseOptionalWhileUntilClause(ref bool isWhile, ref Location whileOrUntilLocation)
		{
			Expression Expression = null;

			if (!CanEndStatement(Peek()))
			{
				var Token = Peek();

				if (Token.Type == TokenType.While || Token.AsUnreservedKeyword() == TokenType.Until)
				{
					isWhile = Token.Type == TokenType.While;
					whileOrUntilLocation = ReadLocation();
					Expression = ParseExpression();

					if (ErrorInConstruct)
					{
						ResyncAt();
					}
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
					ResyncAt();
				}
			}

			return Expression;
		}

		private Statement ParseDoBlockStatement()
		{
			var Start = Read();
			var IsWhile = default(bool);
			Expression Expression;
			var WhileOrUntilLocation = default(Location);
			StatementCollection StatementCollection;
			Statement EndStatement = null;
			LoopStatement LoopStatement;
			List<CommentType> Comments = null;

			Expression = ParseOptionalWhileUntilClause(ref IsWhile, ref WhileOrUntilLocation);

			StatementCollection = ParseStatementBlock(SpanFrom(Start), TreeType.DoBlockStatement, ref Comments, ref EndStatement);
			LoopStatement = (LoopStatement)EndStatement;

			if (Expression is not null && LoopStatement is not null && LoopStatement.Expression is not null)
			{
				ReportSyntaxError(SyntaxErrorType.LoopDoubleCondition, LoopStatement.Expression.Span);
			}

			return new DoBlockStatement(Expression, IsWhile, WhileOrUntilLocation, StatementCollection, LoopStatement, SpanFrom(Start), Comments);
		}

		private Statement ParseLoopStatement()
		{
			var Start = Read();
			var IsWhile = default(bool);
			Expression Expression;
			var WhileOrUntilLocation = default(Location);

			Expression = ParseOptionalWhileUntilClause(ref IsWhile, ref WhileOrUntilLocation);
			return new LoopStatement(Expression, IsWhile, WhileOrUntilLocation, SpanFrom(Start), ParseTrailingComments());
		}

		private Expression ParseForLoopControlVariable(ref VariableDeclarator variableDeclarator)
		{
			var Start = Peek();

			if (Start.Type == TokenType.Identifier)
			{
				var NextToken = PeekAheadOne();
				Expression Expression = null;

				// CONSIDER: Should we just always parse this as a variable declaration?
				if (NextToken.Type == TokenType.As)
				{
					variableDeclarator = ParseForLoopVariableDeclarator(ref Expression);
					return Expression;
				}
				else if (NextToken.Type == TokenType.LeftParenthesis)
				{
					// CONSIDER: Only do this if the token previous to the As is a right parenthesis
					if (PeekAheadFor(TokenType.As, TokenType.In, TokenType.Equals) == TokenType.As)
					{
						variableDeclarator = ParseForLoopVariableDeclarator(ref Expression);
						return Expression;
					}
				}
			}

			return ParseBinaryOperatorExpression(PrecedenceLevel.Relational);
		}

		private Statement ParseForBlockStatement()
		{
			var Start = Read();

			if (Peek().Type != TokenType.Each)
			{
				Expression ControlExpression;
				Expression LowerBoundExpression = null;
				Expression UpperBoundExpression = null;
				Expression StepExpression = null;
				var EqualLocation = default(Location);
				var ToLocation = default(Location);
				var StepLocation = default(Location);
				VariableDeclarator VariableDeclarator = null;
				StatementCollection Statements;
				Statement NextStatement = null;
				List<CommentType> Comments = null;

				ControlExpression = ParseForLoopControlVariable(ref VariableDeclarator);

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Equals, TokenType.To);
				}

				if (Peek().Type == TokenType.Equals)
				{
					EqualLocation = ReadLocation();
					LowerBoundExpression = ParseExpression();

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.To);
					}
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
					ResyncAt(TokenType.To);
				}

				if (Peek().Type == TokenType.To)
				{
					ToLocation = ReadLocation();
					UpperBoundExpression = ParseExpression();

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.Step);
					}
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
					ResyncAt(TokenType.Step);
				}

				if (Peek().Type == TokenType.Step)
				{
					StepLocation = ReadLocation();
					StepExpression = ParseExpression();

					if (ErrorInConstruct)
					{
						ResyncAt();
					}
				}

				Statements = ParseStatementBlock(SpanFrom(Start), TreeType.ForBlockStatement, ref Comments, ref NextStatement);

				return new ForBlockStatement(ControlExpression, VariableDeclarator, EqualLocation, LowerBoundExpression, ToLocation, UpperBoundExpression, StepLocation, StepExpression, Statements, (NextStatement)NextStatement, SpanFrom(Start), Comments);
			}
			else
			{
				Location EachLocation;
				Expression ControlExpression;
				var InLocation = default(Location);
				VariableDeclarator VariableDeclarator = null;
				Expression CollectionExpression = null;
				StatementCollection Statements;
				Statement NextStatement = null;
				List<CommentType> Comments = null;

				EachLocation = ReadLocation();
				ControlExpression = ParseForLoopControlVariable(ref VariableDeclarator);

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.In);
				}

				if (Peek().Type == TokenType.In)
				{
					InLocation = ReadLocation();
					CollectionExpression = ParseExpression();

					if (ErrorInConstruct)
					{
						ResyncAt();
					}
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
					ResyncAt();
				}

				Statements = ParseStatementBlock(SpanFrom(Start), TreeType.ForBlockStatement, ref Comments, ref NextStatement);

				return new ForEachBlockStatement(EachLocation, ControlExpression, VariableDeclarator, InLocation, CollectionExpression, Statements, (NextStatement)NextStatement, SpanFrom(Start), Comments);
			}
		}

		private Statement ParseNextStatement()
		{
			var Start = Read();

			return new NextStatement(ParseExpressionList(), SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseTryBlockStatement()
		{
			var Start = Read();
			StatementCollection TryStatementList;
			StatementCollection StatementCollection;
			Statement EndBlockStatement = null;
			var CatchBlocks = new List<Statement>();
			StatementCollection CatchBlockList = null;
			FinallyBlockStatement FinallyBlock = null;
			List<CommentType> Comments = null;

			TryStatementList = ParseStatementBlock(SpanFrom(Start), TreeType.TryBlockStatement, ref Comments, ref EndBlockStatement);

			while (EndBlockStatement is not null && EndBlockStatement.Type != TreeType.EndBlockStatement)
			{
				if (EndBlockStatement.Type == TreeType.CatchStatement)
				{
					CatchStatement CatchStatement = (CatchStatement)EndBlockStatement;

					List<CommentType> argComments = null;
					StatementCollection = ParseStatementBlock(CatchStatement.Span, TreeType.CatchBlockStatement, ref argComments, ref EndBlockStatement);
					CatchBlocks.Add(new CatchBlockStatement(CatchStatement, StatementCollection, SpanFrom(CatchStatement, EndBlockStatement), null));
				}
				else
				{
					FinallyStatement FinallyStatement = (FinallyStatement)EndBlockStatement;

					List<CommentType> argComments1 = null;
					StatementCollection = ParseStatementBlock(FinallyStatement.Span, TreeType.FinallyBlockStatement, ref argComments1, ref EndBlockStatement);
					FinallyBlock = new FinallyBlockStatement(FinallyStatement, StatementCollection, SpanFrom(FinallyStatement, EndBlockStatement), null);
				}
			}

			if (CatchBlocks.Count > 0)
			{
				CatchBlockList = new StatementCollection(CatchBlocks, null, new Span(((CatchBlockStatement)CatchBlocks[0]).Span.Start, ((CatchBlockStatement)CatchBlocks[CatchBlocks.Count - 1]).Span.Finish));

			}

			return new TryBlockStatement(TryStatementList, CatchBlockList, FinallyBlock, (EndBlockStatement)EndBlockStatement, SpanFrom(Start), Comments);
		}

		private Statement ParseCatchStatement()
		{
			var Start = Read();
			SimpleName Name = null;
			var AsLocation = default(Location);
			TypeName Type = null;
			var WhenLocation = default(Location);
			Expression Filter = null;

			if (Peek().Type == TokenType.Identifier)
			{
				Name = ParseSimpleName(false);

				if (Peek().Type == TokenType.As)
				{
					AsLocation = ReadLocation();
					Type = ParseTypeName(false);

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.When);
					}
				}
			}

			if (Peek().Type == TokenType.When)
			{
				WhenLocation = ReadLocation();
				Filter = ParseExpression();
			}

			return new CatchStatement(Name, AsLocation, Type, WhenLocation, Filter, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseCaseStatement()
		{
			var Start = Read();
			List<Location> CommaLocations;
			List<CaseClause> Cases;
			var CasesStart = Peek();

			if (Peek().Type == TokenType.Else)
			{
				return new CaseElseStatement(ReadLocation(), SpanFrom(Start), ParseTrailingComments());
			}
			else
			{
				CommaLocations = new List<Location>();
				Cases = new List<CaseClause>();

				do
				{
					if (Cases.Count > 0)
					{
						CommaLocations.Add(ReadLocation());
					}

					Cases.Add(ParseCase());
				}
				while (Peek().Type == TokenType.Comma);

				return new CaseStatement(new CaseClauseCollection(Cases, CommaLocations, SpanFrom(CasesStart)), SpanFrom(Start), ParseTrailingComments());
			}
		}

		private Statement ParseSelectBlockStatement()
		{
			var Start = Read();
			var CaseLocation = default(Location);
			Expression SelectExpression;
			StatementCollection Statements;
			Statement EndBlockStatement = null;
			var CaseBlocks = new List<Statement>();
			StatementCollection CaseBlockList = null;
			CaseElseBlockStatement CaseElseBlockStatement = null;
			List<CommentType> Comments = null;

			if (Peek().Type == TokenType.Case)
			{
				CaseLocation = ReadLocation();
			}

			SelectExpression = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			Statements = ParseStatementBlock(SpanFrom(Start), TreeType.SelectBlockStatement, ref Comments, ref EndBlockStatement);

			if (Statements is not null && Statements.Count != 0)
			{
				foreach (Statement Statement in Statements)
				{
					if (Statement.Type != TreeType.EmptyStatement)
					{
						ReportSyntaxError(SyntaxErrorType.ExpectedCase, Statements.Span);
					}
				}
			}

			while (EndBlockStatement is not null && EndBlockStatement.Type != TreeType.EndBlockStatement)
			{
				var CaseStatement = EndBlockStatement;
				StatementCollection CaseStatements;

				if (CaseStatement.Type == TreeType.CaseStatement)
				{
					List<CommentType> argComments = null;
					CaseStatements = ParseStatementBlock(CaseStatement.Span, TreeType.CaseBlockStatement, ref argComments, ref EndBlockStatement);
					CaseBlocks.Add(new CaseBlockStatement((CaseStatement)CaseStatement, CaseStatements, SpanFrom(CaseStatement, EndBlockStatement), null));
				}
				else
				{
					List<CommentType> argComments1 = null;
					CaseStatements = ParseStatementBlock(CaseStatement.Span, TreeType.CaseElseBlockStatement, ref argComments1, ref EndBlockStatement);
					CaseElseBlockStatement = new CaseElseBlockStatement((CaseElseStatement)CaseStatement, CaseStatements, SpanFrom(CaseStatement, EndBlockStatement), null);
				}
			}

			if (CaseBlocks.Count > 0)
			{
				CaseBlockList = new StatementCollection(CaseBlocks, null, new Span(((CaseBlockStatement)CaseBlocks[0]).Span.Start, ((CaseBlockStatement)CaseBlocks[CaseBlocks.Count - 1]).Span.Finish));

			}

			return new SelectBlockStatement(CaseLocation, SelectExpression, Statements, CaseBlockList, CaseElseBlockStatement, (EndBlockStatement)EndBlockStatement, SpanFrom(Start), Comments);
		}

		private Statement ParseElseIfStatement()
		{
			var Start = Read();
			var ThenLocation = default(Location);
			Expression Expression;

			Expression = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Then);
			}

			if (Peek().Type == TokenType.Then)
			{
				ThenLocation = ReadLocation();
			}

			return new ElseIfStatement(Expression, ThenLocation, SpanFrom(Start), ParseTrailingComments());
		}

		private Statement ParseIfBlockStatement()
		{
			var Start = Read();
			Expression Expression;
			var ThenLocation = default(Location);
			StatementCollection Statements;
			StatementCollection IfStatements;
			Statement EndBlockStatement = null;
			var ElseIfBlocks = new List<Statement>();
			StatementCollection ElseIfBlockList = null;
			ElseBlockStatement ElseBlockStatement = null;
			List<CommentType> Comments = null;

			Expression = ParseExpression();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Then);
			}

			if (Peek().Type == TokenType.Then)
			{
				ThenLocation = ReadLocation();

				if (!CanEndStatement(Peek()))
				{
					var ElseLocation = default(Location);
					StatementCollection ElseStatements = null;

					// We're in a line If context
					AtBeginningOfLine = false;
					IfStatements = ParseLineIfStatementBlock();

					if (Peek().Type == TokenType.Else)
					{
						ElseLocation = ReadLocation();
						ElseStatements = ParseLineIfStatementBlock();
					}

					return new LineIfStatement(Expression, ThenLocation, IfStatements, ElseLocation, ElseStatements, SpanFrom(Start), ParseTrailingComments());
				}
			}

			IfStatements = ParseStatementBlock(SpanFrom(Start), TreeType.IfBlockStatement, ref Comments, ref EndBlockStatement);

			while (EndBlockStatement is not null && EndBlockStatement.Type != TreeType.EndBlockStatement)
			{
				var ElseStatement = EndBlockStatement;

				if (ElseStatement.Type == TreeType.ElseIfStatement)
				{
					List<CommentType> argComments = null;
					Statements = ParseStatementBlock(ElseStatement.Span, TreeType.ElseIfBlockStatement, ref argComments, ref EndBlockStatement);
					ElseIfBlocks.Add(new ElseIfBlockStatement((ElseIfStatement)ElseStatement, Statements, SpanFrom(ElseStatement, EndBlockStatement), null));
				}
				else
				{
					List<CommentType> argComments1 = null;
					Statements = ParseStatementBlock(ElseStatement.Span, TreeType.ElseBlockStatement, ref argComments1, ref EndBlockStatement);
					ElseBlockStatement = new ElseBlockStatement((ElseStatement)ElseStatement, Statements, SpanFrom(ElseStatement, EndBlockStatement), null);
				}
			}

			if (ElseIfBlocks.Count > 0)
			{
				ElseIfBlockList = new StatementCollection(ElseIfBlocks, null, new Span(ElseIfBlocks[0].Span.Start, ElseIfBlocks[ElseIfBlocks.Count - 1].Span.Finish));

			}

			return new IfBlockStatement(Expression, ThenLocation, IfStatements, ElseIfBlockList, ElseBlockStatement, (EndBlockStatement)EndBlockStatement, SpanFrom(Start), Comments);
		}

		private Statement ParseStatement([Optional] ref Token terminator)
		{
			var Start = Peek();
			Statement Statement = null;

			// If AtBeginningOfLine Then
			// While ParsePreprocessorStatement(True)
			// Start = Peek()
			// End While
			// End If

			ErrorInConstruct = false;

			switch (Start.Type)
			{
				case TokenType.GoTo:
					{
						Statement = ParseGotoStatement();
						break;
					}

				case TokenType.Exit:
					{
						Statement = ParseExitStatement();
						break;
					}

				case TokenType.Continue:
					{
						Statement = ParseContinueStatement();
						break;
					}

				case TokenType.Stop:
					{
						Statement = new StopStatement(SpanFrom(Read()), ParseTrailingComments());
						break;
					}

				case TokenType.End:
					{
						Statement = ParseEndStatement();
						break;
					}

				// LC Added Wend back
				case TokenType.Wend:
					{
						Statement = ParseWendStatement();
						break;
					}

				case TokenType.Return:
					{
						Statement = ParseExpressionStatement(TreeType.ReturnStatement, true);
						break;
					}

				case TokenType.RaiseEvent:
					{
						Statement = ParseRaiseEventStatement();
						break;
					}

				case TokenType.AddHandler:
				case TokenType.RemoveHandler:
					{
						Statement = ParseHandlerStatement();
						break;
					}

				case TokenType.Error:
					{
						Statement = ParseExpressionStatement(TreeType.ErrorStatement, false);
						break;
					}

				case TokenType.On:
					{
						Statement = ParseOnErrorStatement();
						break;
					}

				case TokenType.Resume:
					{
						Statement = ParseResumeStatement();
						break;
					}

				case TokenType.ReDim:
					{
						Statement = ParseReDimStatement();
						break;
					}

				case TokenType.Erase:
					{
						Statement = ParseEraseStatement();
						break;
					}

				case TokenType.Call:
					{
						Statement = ParseCallStatement();
						break;
					}

				case TokenType.IntegerLiteral:
					{
						if (AtBeginningOfLine)
						{
							Statement = ParseLabelStatement();
						}
						else
						{
							goto default;
						}

						break;
					}

				// LC Add Set statement for VBScript
				case TokenType.Set:
					{
						Read();
						Expression Target;

						Target = ParseBinaryOperatorExpression(PrecedenceLevel.Power);

						if (ErrorInConstruct)
						{
							ResyncAt(TokenType.Equals);
						}

						if (GetAssignmentOperator(Peek().Type) != TreeType.SyntaxError)
						{
							Statement = ParseAssignmentStatement(Target, true);
						}
						else
						{
							// Missing assignment
							goto default;
						}

						break;
					}

				case TokenType.Identifier:
					{
						if (AtBeginningOfLine)
						{
							bool IsLabel;

							Read();
							IsLabel = Peek().Type == TokenType.Colon;
							Backtrack(Start);

							if (IsLabel)
							{
								Statement = ParseLabelStatement();
							}
							
						}

						if (Statement == null && Start.AsUnreservedKeyword() == TokenType.Mid)
						{
							Statement = ParseMidAssignmentStatement();
						}

						if (Statement is null)
						{
							goto case TokenType.Period;
						}
						break;
					}

				case TokenType.Period:
				case TokenType.Exclamation:
				case TokenType.Me:
				case TokenType.MyBase:
				case TokenType.MyClass:
				case TokenType.Boolean:
				case TokenType.Byte:
				case TokenType.Short:
				case TokenType.Integer:
				case TokenType.Long:
				case TokenType.Decimal:
				case TokenType.Single:
				case TokenType.Double:
				case TokenType.Date:
				case TokenType.Char:
				case TokenType.String:
				case TokenType.Object:
				case TokenType.DirectCast:
				case TokenType.CType:
				case TokenType.CBool:
				case TokenType.CByte:
				case TokenType.CShort:
				case TokenType.CInt:
				case TokenType.CLng:
				case TokenType.CDec:
				case TokenType.CSng:
				case TokenType.CDbl:
				case TokenType.CDate:
				case TokenType.CChar:
				case TokenType.CStr:
				case TokenType.CObj:
				case TokenType.GetType:
					{
						Expression Target;

					AssignmentOrCallStatement:
						;

						Target = ParseBinaryOperatorExpression(PrecedenceLevel.Power);

						if (ErrorInConstruct)
						{
							ResyncAt(TokenType.Equals);
						}

						// Could be a function call or it could be an assignment
						if (GetAssignmentOperator(Peek().Type) != TreeType.SyntaxError)
						{
							Statement = ParseAssignmentStatement(Target, false);
						}
						else
						{
							Statement = ParseCallStatement(Target);
						}

						break;
					}

				case TokenType.Public:
				case TokenType.Private:
				case TokenType.Protected:
				case TokenType.Friend:
				case TokenType.Static:
				case TokenType.Shared:
				case TokenType.Shadows:
				case TokenType.Overloads:
				case TokenType.MustInherit:
				case TokenType.NotInheritable:
				case TokenType.Overrides:
				case TokenType.NotOverridable:
				case TokenType.Overridable:
				case TokenType.MustOverride:
				case TokenType.Partial:
				case TokenType.ReadOnly:
				case TokenType.WriteOnly:
				case TokenType.Dim:
				case TokenType.Const:
				case TokenType.Default:
				case TokenType.WithEvents:
				case TokenType.Widening:
				case TokenType.Narrowing:
					{
						Statement = ParseLocalDeclarationStatement();
						break;
					}

				case TokenType.With:
					{
						Statement = ParseExpressionBlockStatement(TreeType.WithBlockStatement);
						break;
					}

				case TokenType.SyncLock:
					{
						Statement = ParseExpressionBlockStatement(TreeType.SyncLockBlockStatement);
						break;
					}

				case TokenType.Using:
					{
						Statement = ParseUsingBlockStatement();
						break;
					}

				case TokenType.While:
					{
						Statement = ParseExpressionBlockStatement(TreeType.WhileBlockStatement);
						break;
					}

				case TokenType.Do:
					{
						Statement = ParseDoBlockStatement();
						break;
					}

				case TokenType.Loop:
					{
						Statement = ParseLoopStatement();
						break;
					}

				case TokenType.For:
					{
						Statement = ParseForBlockStatement();
						break;
					}

				case TokenType.Next:
					{
						Statement = ParseNextStatement();
						break;
					}

				case TokenType.Throw:
					{
						Statement = ParseExpressionStatement(TreeType.ThrowStatement, true);
						break;
					}

				case TokenType.Try:
					{
						Statement = ParseTryBlockStatement();
						break;
					}

				case TokenType.Catch:
					{
						Statement = ParseCatchStatement();
						break;
					}

				case TokenType.Finally:
					{
						Statement = new FinallyStatement(SpanFrom(Read()), ParseTrailingComments());
						break;
					}

				case TokenType.Select:
					{
						Statement = ParseSelectBlockStatement();
						break;
					}

				case TokenType.Case:
					{
						Statement = ParseCaseStatement();
						CanContinueWithoutLineTerminator = true;
						break;
					}

				case TokenType.If:
					{
						Statement = ParseIfBlockStatement();
						break;
					}

				case TokenType.Else:
					{
						Statement = new ElseStatement(SpanFrom(Read()), ParseTrailingComments());
						break;
					}

				case TokenType.ElseIf:
					{
						Statement = ParseElseIfStatement();
						break;
					}

				case TokenType.LineTerminator:
				case TokenType.Colon:
					{
						break;
					}
				// An empty statement

				case TokenType.Comment:
					{
						var Comments = new List<CommentType>();
						Token LastTerminator;

						do
						{
							CommentToken CommentToken = (CommentToken)Scanner.Read();
							Comments.Add(new CommentType(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span));
							LastTerminator = Read(); // Eat the terminator of the comment
						}
						while (Peek().Type == TokenType.Comment);
						Backtrack(LastTerminator);

						Statement = new EmptyStatement(SpanFrom(Start), new List<CommentType>(Comments));
						break;
					}

				default:
					{
						ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
						break;
					}
			}

			terminator = VerifyEndOfStatement();

			return Statement;
		}

		private Statement ParseStatementOrDeclaration([Optional] ref Token terminator)
		{
			var Start = Peek();
			Location StartLocation;
			Statement Statement = null;

			// If AtBeginningOfLine Then
			// While ParsePreprocessorStatement(True)
			// Start = Peek()
			// End While
			// End If

			ErrorInConstruct = false;
			StartLocation = Peek().Span.Start;

			switch (Start.Type)
			{
				case TokenType.GoTo:
					{
						Statement = ParseGotoStatement();
						break;
					}

				case TokenType.Exit:
					{
						Statement = ParseExitStatement();
						break;
					}

				case TokenType.Continue:
					{
						Statement = ParseContinueStatement();
						break;
					}

				case TokenType.Stop:
					{
						Statement = new StopStatement(SpanFrom(Read()), ParseTrailingComments());
						break;
					}

				case TokenType.End:
					{
						Statement = ParseEndStatement();
						break;
					}

				// LC Added Wend back
				case TokenType.Wend:
					{
						Statement = ParseWendStatement();
						break;
					}

				case TokenType.Return:
					{
						Statement = ParseExpressionStatement(TreeType.ReturnStatement, true);
						break;
					}

				case TokenType.RaiseEvent:
					{
						Statement = ParseRaiseEventStatement();
						break;
					}

				case TokenType.AddHandler:
				case TokenType.RemoveHandler:
					{
						Statement = ParseHandlerStatement();
						break;
					}

				case TokenType.Error:
					{
						Statement = ParseExpressionStatement(TreeType.ErrorStatement, false);
						break;
					}

				case TokenType.On:
					{
						Statement = ParseOnErrorStatement();
						break;
					}

				case TokenType.Resume:
					{
						Statement = ParseResumeStatement();
						break;
					}

				case TokenType.ReDim:
					{
						Statement = ParseReDimStatement();
						break;
					}

				case TokenType.Erase:
					{
						Statement = ParseEraseStatement();
						break;
					}

				case TokenType.Call:
					{
						Statement = ParseCallStatement();
						break;
					}

				case TokenType.IntegerLiteral:
					{
						if (AtBeginningOfLine)
						{
							Statement = ParseLabelStatement();
						}
						else
						{
							goto default;
						}

						break;
					}

				// LC Add Set statement for VBScript
				case TokenType.Set:
					{
						Read();
						Expression Target;

						Target = ParseBinaryOperatorExpression(PrecedenceLevel.Power);

						if (ErrorInConstruct)
						{
							ResyncAt(TokenType.Equals);
						}

						if (GetAssignmentOperator(Peek().Type) != TreeType.SyntaxError)
						{
							Statement = ParseAssignmentStatement(Target, true);
						}
						else
						{
							// Missing assignment
							goto default;
						}

						break;
					}

				case TokenType.Identifier:
					{
						if (AtBeginningOfLine)
						{
							bool IsLabel;

							Read();
							IsLabel = Peek().Type == TokenType.Colon;
							Backtrack(Start);

							if (IsLabel)
							{
								Statement = ParseLabelStatement();
							}
						}
						
						if (Statement is null)
						{
							if (Start.AsUnreservedKeyword() == TokenType.Mid)
							{
								Statement = ParseMidAssignmentStatement();
							}

							if (Statement is null)
							{
								goto case TokenType.Period;
							}
						}

						break;
					}

				case TokenType.Period:
				case TokenType.Exclamation:
				case TokenType.Me:
				case TokenType.MyBase:
				case TokenType.MyClass:
				case TokenType.Boolean:
				case TokenType.Byte:
				case TokenType.Short:
				case TokenType.Integer:
				case TokenType.Long:
				case TokenType.Decimal:
				case TokenType.Single:
				case TokenType.Double:
				case TokenType.Date:
				case TokenType.Char:
				case TokenType.String:
				case TokenType.Object:
				case TokenType.DirectCast:
				case TokenType.CType:
				case TokenType.CBool:
				case TokenType.CByte:
				case TokenType.CShort:
				case TokenType.CInt:
				case TokenType.CLng:
				case TokenType.CDec:
				case TokenType.CSng:
				case TokenType.CDbl:
				case TokenType.CDate:
				case TokenType.CChar:
				case TokenType.CStr:
				case TokenType.CObj:
				case TokenType.GetType:
					{
						Expression Target;

					AssignmentOrCallStatement:
						;

						Target = ParseBinaryOperatorExpression(PrecedenceLevel.Power);

						if (ErrorInConstruct)
						{
							ResyncAt(TokenType.Equals);
						}

						// Could be a function call or it could be an assignment
						if (GetAssignmentOperator(Peek().Type) != TreeType.SyntaxError)
						{
							Statement = ParseAssignmentStatement(Target, false);
						}
						else
						{
							Statement = ParseCallStatement(Target);
						}

						break;
					}

				case TokenType.Public:
				case TokenType.Private:
				case TokenType.Protected:
				case TokenType.Friend:
				case TokenType.Static:
				case TokenType.Shared:
				case TokenType.Shadows:
				case TokenType.Overloads:
				case TokenType.MustInherit:
				case TokenType.NotInheritable:
				case TokenType.Overrides:
				case TokenType.NotOverridable:
				case TokenType.Overridable:
				case TokenType.MustOverride:
				case TokenType.Partial:
				case TokenType.ReadOnly:
				case TokenType.WriteOnly:
				case TokenType.Dim:
				case TokenType.Const:
				case TokenType.Default:
				case TokenType.WithEvents:
				case TokenType.Widening:
				case TokenType.Narrowing:
					{
						// LC Sub or function can have public/private modifier
						var NextToken = PeekAheadOne();
						switch (NextToken.Type)
						{
							case TokenType.Sub:
							case TokenType.Function:
							case TokenType.Default:
								{
									var Modifiers = ParseDeclarationModifierList();
									Statement = ParseMethodDeclaration(StartLocation, null, null);
									break;
								}

							default:
								{
									Statement = ParseLocalDeclarationStatement();
									break;
								}
						}

						break;
					}

				case TokenType.With:
					{
						Statement = ParseExpressionBlockStatement(TreeType.WithBlockStatement);
						break;
					}

				case TokenType.SyncLock:
					{
						Statement = ParseExpressionBlockStatement(TreeType.SyncLockBlockStatement);
						break;
					}

				case TokenType.Using:
					{
						Statement = ParseUsingBlockStatement();
						break;
					}

				case TokenType.While:
					{
						Statement = ParseExpressionBlockStatement(TreeType.WhileBlockStatement);
						break;
					}

				case TokenType.Do:
					{
						Statement = ParseDoBlockStatement();
						break;
					}

				case TokenType.Loop:
					{
						Statement = ParseLoopStatement();
						break;
					}

				case TokenType.For:
					{
						Statement = ParseForBlockStatement();
						break;
					}

				case TokenType.Next:
					{
						Statement = ParseNextStatement();
						break;
					}

				case TokenType.Throw:
					{
						Statement = ParseExpressionStatement(TreeType.ThrowStatement, true);
						break;
					}

				case TokenType.Try:
					{
						Statement = ParseTryBlockStatement();
						break;
					}

				case TokenType.Catch:
					{
						Statement = ParseCatchStatement();
						break;
					}

				case TokenType.Finally:
					{
						Statement = new FinallyStatement(SpanFrom(Read()), ParseTrailingComments());
						break;
					}

				case TokenType.Select:
					{
						Statement = ParseSelectBlockStatement();
						break;
					}

				case TokenType.Case:
					{
						Statement = ParseCaseStatement();
						break;
					}

				case TokenType.If:
					{
						Statement = ParseIfBlockStatement();
						break;
					}

				case TokenType.Else:
					{
						Statement = new ElseStatement(SpanFrom(Read()), ParseTrailingComments());
						break;
					}

				case TokenType.ElseIf:
					{
						Statement = ParseElseIfStatement();
						break;
					}

				case TokenType.LineTerminator:
				case TokenType.Colon:
					{
						break;
					}
				// An empty statement

				// LC added method declaration parsing
				case TokenType.Sub:
				case TokenType.Function:
					{
						Statement = ParseMethodDeclaration(StartLocation, null, null);
						break;
					}

				case TokenType.Class:
					{
						Statement = ParseTypeDeclaration(StartLocation, null, null, TreeType.ClassDeclaration);
						break;
					}

				case var @case when @case == TokenType.LineTerminator:
				case var case1 when case1 == TokenType.Colon:
					{
						break;
					}
				// An empty statement

				case TokenType.Imports:
					{
						Statement = ParseImportsDeclaration(StartLocation, null, null);
						break;
					}

				case TokenType.Option:
					{
						Statement = ParseOptionDeclaration(StartLocation, null, null);
						break;
					}

				case TokenType.Comment:
					{
						var Comments = new List<CommentType>();
						Token LastTerminator;

						do
						{
							CommentToken CommentToken = (CommentToken)Scanner.Read();
							Comments.Add(new CommentType(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span));
							LastTerminator = Read(); // Eat the terminator of the comment
						}
						while (Peek().Type == TokenType.Comment);
						Backtrack(LastTerminator);

						Statement = new EmptyStatement(SpanFrom(Start), new List<CommentType>(Comments));
						break;
					}

				default:
					{
						ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
						break;
					}
			}

			terminator = VerifyEndOfStatement();

			return Statement;
		}

		private StatementCollection ParseStatementBlock(Span blockStartSpan, TreeType blockType, ref List<CommentType> Comments, [Optional] ref Statement endStatement)
		{
			var Statements = new List<Statement>();
			var ColonLocations = new List<Location>();
			Token Terminator;
			Token Start;
			Location StatementsEnd;
			bool BlockTerminated = false;

			Debug.Assert(blockType != TreeType.LineIfBlockStatement);
			Comments = ParseTrailingComments();
			Terminator = VerifyEndOfStatement();
			CanContinueWithoutLineTerminator = false;

			if (Terminator.Type == TokenType.Colon)
			{
				if (blockType == TreeType.SubDeclaration || blockType == TreeType.FunctionDeclaration || blockType == TreeType.ConstructorDeclaration || blockType == TreeType.OperatorDeclaration || blockType == TreeType.GetAccessorDeclaration || blockType == TreeType.SetAccessorDeclaration || blockType == TreeType.AddHandlerAccessorDeclaration || blockType == TreeType.RemoveHandlerAccessorDeclaration || blockType == TreeType.RaiseEventAccessorDeclaration)







				{
					ReportSyntaxError(SyntaxErrorType.MethodBodyNotAtLineStart, Terminator.Span);
				}

				ColonLocations.Add(Terminator.Span.Start);
			}

			Start = Peek();
			StatementsEnd = Start.Span.Finish;
			endStatement = null;

			PushBlockContext(blockType);

			while (Peek().Type != TokenType.EndOfStream)
			{
				var PreviousTerminator = Terminator;
				Statement Statement;

				Statement = ParseStatement(ref Terminator);

				if (Statement is not null)
				{
					if (Statement.Type >= TreeType.LoopStatement && Statement.Type <= TreeType.EndBlockStatement)
					{
						if (StatementEndsBlock(blockType, Statement))
						{
							endStatement = Statement;
							Backtrack(Terminator);
							BlockTerminated = true;
							break;
						}
						else
						{
							bool StatementEndsOuterBlock = false;

							// If the end statement matches an outer block context, then we want to unwind
							// up to that level. Otherwise, we want to just give an error and keep going.
							foreach (TreeType BlockContext in BlockContextStack)
							{
								if (StatementEndsBlock(BlockContext, Statement))
								{
									StatementEndsOuterBlock = true;
									break;
								}
							}

							if (StatementEndsOuterBlock)
							{
								ReportMismatchedEndError(blockType, Statement.Span);
								// CONSIDER: Can we avoid parsing and re-parsing this statement?
								Backtrack(PreviousTerminator);
								// We consider the block terminated.
								BlockTerminated = true;
								break;
							}
							else
							{
								ReportMissingBeginStatementError(blockType, Statement);
							}
						}
					}

					Statements.Add(Statement);
				}

				if (Terminator.Type == TokenType.Colon)
				{
					ColonLocations.Add(Terminator.Span.Start);
					StatementsEnd = Terminator.Span.Finish;
				}
				else
				{
					StatementsEnd = Terminator.Span.Finish;
				}
			}

			if (!BlockTerminated)
			{
				ReportMismatchedEndError(blockType, blockStartSpan);
			}

			PopBlockContext();

			if (Statements.Count == 0 && ColonLocations.Count == 0)
			{
				return null;
			}
			else
			{
				return new StatementCollection(Statements, ColonLocations, new Span(Start.Span.Start, StatementsEnd));
			}
		}

		private StatementCollection ParseLineIfStatementBlock()
		{
			var Statements = new List<Statement>();
			var ColonLocations = new List<Location>();
			Token Terminator = null;
			Token Start;
			Location StatementsEnd;

			Start = Peek();
			StatementsEnd = Start.Span.Finish;

			PushBlockContext(TreeType.LineIfBlockStatement);

			while (!CanEndStatement(Peek()))
			{
				Statement Statement;

				Statement = ParseStatement(ref Terminator);

				if (Statement is not null)
				{
					if (Statement.Type >= TreeType.LoopStatement && Statement.Type <= TreeType.EndBlockStatement)
					{
						ReportSyntaxError(SyntaxErrorType.EndInLineIf, Statement.Span);
					}

					Statements.Add(Statement);
				}

				if (Terminator.Type == TokenType.Colon)
				{
					ColonLocations.Add(Terminator.Span.Start);
					StatementsEnd = Terminator.Span.Finish;
				}
				else
				{
					Backtrack(Terminator);
					break;
				}
			}

			// LC LineIf can end with endif
			if (Terminator.Type == TokenType.End)
			{
				Statement Statement;
				Statement = ParseStatement(ref Terminator);
				if (StatementEndsBlock(CurrentBlockContextType(), Statement))
				{
					Backtrack(Terminator);
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedEndIf, Statement.Span);
				}
			}

			PopBlockContext();

			if (Statements.Count == 0 && ColonLocations.Count == 0)
			{
				return null;
			}
			else
			{
				return new StatementCollection(Statements, ColonLocations, new Span(Start.Span.Start, StatementsEnd));
			}
		}

		// *
		// * Modifiers
		// *

		private void ValidateModifierList(ModifierCollection modifiers, ModifierTypes validTypes)
		{
			if (modifiers is null)
			{
				return;
			}

			foreach (Modifier Modifier in modifiers)
			{
				if ((validTypes & Modifier.ModifierType) == 0)
				{
					ReportSyntaxError(SyntaxErrorType.InvalidModifier, Modifier.Span);
				}
			}
		}

		private ModifierCollection ParseDeclarationModifierList()
		{
			var Modifiers = new List<Modifier>();
			var Start = Peek();
			ModifierTypes ModifierTypes = default;
			var FoundTypes = default(ModifierTypes);

			while (true)
			{
				bool exitWhile = false;
				switch (Peek().Type)
				{
					case TokenType.Public:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Public;
							break;
						}

					case TokenType.Private:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Private;
							break;
						}

					case TokenType.Protected:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Protected;
							break;
						}

					case TokenType.Friend:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Friend;
							break;
						}

					case TokenType.Static:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Static;
							break;
						}

					case TokenType.Shared:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Shared;
							break;
						}

					case TokenType.Shadows:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Shadows;
							break;
						}

					case TokenType.Overloads:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Overloads;
							break;
						}

					case TokenType.MustInherit:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.MustInherit;
							break;
						}

					case TokenType.NotInheritable:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.NotInheritable;
							break;
						}

					case TokenType.Overrides:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Overrides;
							break;
						}

					case TokenType.Overridable:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Overridable;
							break;
						}

					case TokenType.NotOverridable:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.NotOverridable;
							break;
						}

					case TokenType.MustOverride:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.MustOverride;
							break;
						}

					case TokenType.Partial:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Partial;
							break;
						}

					case TokenType.ReadOnly:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.ReadOnly;
							break;
						}

					case TokenType.WriteOnly:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.WriteOnly;
							break;
						}

					case TokenType.Dim:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Dim;
							break;
						}

					case TokenType.Const:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Const;
							break;
						}

					case TokenType.Default:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Default;
							break;
						}

					case TokenType.WithEvents:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.WithEvents;
							break;
						}

					case TokenType.Widening:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Widening;
							break;
						}

					case TokenType.Narrowing:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Narrowing;
							break;
						}

					default:
						{
							exitWhile = true;
							break;
						}
				}

				if (exitWhile)
				{
					break;
				}

				if ((FoundTypes & ModifierTypes) != 0)
				{
					ReportSyntaxError(SyntaxErrorType.DuplicateModifier, Peek());
				}
				else
				{
					FoundTypes = FoundTypes | ModifierTypes;
				}

				Modifiers.Add(new Modifier(ModifierTypes, SpanFrom(Read())));
			}

			if (Modifiers.Count == 0)
			{
				return null;
			}
			else
			{
				return new ModifierCollection(Modifiers, SpanFrom(Start));
			}
		}

		private ModifierCollection ParseParameterModifierList()
		{
			var Modifiers = new List<Modifier>();
			var Start = Peek();
			ModifierTypes ModifierTypes = default;
			var FoundTypes = default(ModifierTypes);

			while (true)
			{
				bool exitWhile = false;
				switch (Peek().Type)
				{
					case TokenType.ByVal:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.ByVal;
							break;
						}

					case TokenType.ByRef:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.ByRef;
							break;
						}

					case TokenType.Optional:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.Optional;
							break;
						}

					case TokenType.ParamArray:
						{
							ModifierTypes = VBScript.Parser.ModifierTypes.ParamArray;
							break;
						}

					default:
						{
							exitWhile = true;
							break;
						}
				}

				if (exitWhile)
				{
					break;
				}

				if ((FoundTypes & ModifierTypes) != 0)
				{
					ReportSyntaxError(SyntaxErrorType.DuplicateModifier, Peek());
				}
				else
				{
					FoundTypes = FoundTypes | ModifierTypes;
				}

				Modifiers.Add(new Modifier(ModifierTypes, SpanFrom(Read())));
			}

			if (Modifiers.Count == 0)
			{
				return null;
			}
			else
			{
				return new ModifierCollection(Modifiers, SpanFrom(Start));
			}
		}

		// *
		// * VariableDeclarators
		// *

		private VariableDeclarator ParseVariableDeclarator()
		{
			var DeclarationStart = Peek();
			var VariableNamesCommaLocations = new List<Location>();
			var VariableNames = new List<VariableName>();
			var AsLocation = default(Location);
			var NewLocation = default(Location);
			TypeName Type = null;
			ArgumentCollection NewArguments = null;
			var EqualsLocation = default(Location);
			Initializer Initializer = null;
			VariableNameCollection VariableNameCollection;

			// Parse the declarators
			do
			{
				VariableName VariableName;

				if (VariableNames.Count > 0)
				{
					VariableNamesCommaLocations.Add(ReadLocation());
				}

				VariableName = ParseVariableName(true);

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.As, TokenType.Comma, TokenType.New, TokenType.Equals);
				}

				VariableNames.Add(VariableName);
			}
			while (Peek().Type == TokenType.Comma);

			VariableNameCollection = new VariableNameCollection(VariableNames, VariableNamesCommaLocations, SpanFrom(DeclarationStart));

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();

				if (Peek().Type == TokenType.New)
				{
					NewLocation = ReadLocation();
					Type = ParseTypeName(false);
					NewArguments = ParseArguments();
				}
				else
				{
					Type = ParseTypeName(true);

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.Comma, TokenType.Equals);
					}
				}
			}

			if (Peek().Type == TokenType.Equals && !NewLocation.IsValid)
			{
				EqualsLocation = ReadLocation();
				Initializer = ParseInitializer();

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Comma);
				}
			}

			return new VariableDeclarator(VariableNameCollection, AsLocation, NewLocation, Type, NewArguments, EqualsLocation, Initializer, SpanFrom(DeclarationStart));
		}

		private VariableDeclaratorCollection ParseVariableDeclarators()
		{
			var Start = Peek();
			var VariableDeclarators = new List<VariableDeclarator>();
			var DeclarationsCommaLocations = new List<Location>();

			// Parse the declarations
			do
			{
				if (VariableDeclarators.Count > 0)
				{
					DeclarationsCommaLocations.Add(ReadLocation());
				}

				VariableDeclarators.Add(ParseVariableDeclarator());
			}
			while (Peek().Type == TokenType.Comma);

			return new VariableDeclaratorCollection(VariableDeclarators, DeclarationsCommaLocations, SpanFrom(Start));
		}

		private VariableDeclarator ParseForLoopVariableDeclarator(ref Expression controlExpression)
		{
			var Start = Peek();
			var AsLocation = default(Location);
			TypeName Type = null;
			VariableName VariableName;
			var VariableNames = new List<VariableName>();
			VariableNameCollection VariableNameCollection;

			VariableName = ParseVariableName(false);
			VariableNames.Add(VariableName);
			VariableNameCollection = new VariableNameCollection(VariableNames, null, SpanFrom(Start));

			if (ErrorInConstruct)
			{
				// If we see As before a In or Each, then assume that we are still on the Control Variable Declaration. 
				// Otherwise, don't resync and allow the caller to decide how to recover.
				if (PeekAheadFor(TokenType.As, TokenType.In, TokenType.Equals) == TokenType.As)
				{
					ResyncAt(TokenType.As);
				}
			}

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				Type = ParseTypeName(true);
			}

			controlExpression = new SimpleNameExpression(VariableName.Name, VariableName.Span);

			return new VariableDeclarator(VariableNameCollection, AsLocation, default, Type, null, default, null, SpanFrom(Start));
		}

		// *
		// * CaseClauses
		// *

		private CaseClause ParseCase()
		{
			var Start = Peek();

			if (Start.Type == TokenType.Is || IsRelationalOperator(Start.Type))
			{
				var IsLocation = default(Location);
				Token OperatorToken;
				var Operator = OperatorType.None;
				Expression Operand;

				if (Start.Type == TokenType.Is)
				{
					IsLocation = ReadLocation();
				}

				if (IsRelationalOperator(Peek().Type))
				{
					OperatorToken = Read();
					Operator = GetBinaryOperator(OperatorToken.Type);
					Operand = ParseExpression();

					if (ErrorInConstruct)
					{
						ResyncAt();
					}

					return new ComparisonCaseClause(IsLocation, Operator, OperatorToken.Span.Start, Operand, SpanFrom(Start));
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedRelationalOperator, Peek());
					ResyncAt();
					return null;
				}
			}
			else
			{
				return new RangeCaseClause(ParseExpression(true), SpanFrom(Start));
			}
		}

		// *
		// * Attributes
		// *

		private AttributeCollection ParseAttributeBlock(AttributeTypes attributeTypesAllowed)
		{
			var Start = Peek();
			var Attributes = new List<Attribute>();
			Location RightBracketLocation;
			var CommaLocations = new List<Location>();

			if (Start.Type != TokenType.LessThan)
			{
				return null;
			}

			Read();

			do
			{
				Token AttributeStart;
				var AttributeTypes = VBScript.Parser.AttributeTypes.Regular;
				var AttributeTypeLocation = new Location();
				var ColonLocation = new Location();
				Name Name;
				ArgumentCollection Arguments;

				if (Attributes.Count > 0)
				{
					CommaLocations.Add(ReadLocation());
				}

				AttributeStart = Peek();

				if (AttributeStart.AsUnreservedKeyword() == TokenType.Assembly)
				{
					AttributeTypes = VBScript.Parser.AttributeTypes.Assembly;
					AttributeTypeLocation = ReadLocation();
					ColonLocation = VerifyExpectedToken(TokenType.Colon);
				}
				else if (AttributeStart.Type == TokenType.Module)
				{
					AttributeTypes = VBScript.Parser.AttributeTypes.Module;
					AttributeTypeLocation = ReadLocation();
					ColonLocation = VerifyExpectedToken(TokenType.Colon);
				}

				if ((AttributeTypes & attributeTypesAllowed) == 0)
				{
					ReportSyntaxError(SyntaxErrorType.IncorrectAttributeType, AttributeStart);
				}

				Name = ParseName(true);
				Arguments = ParseArguments();

				Attributes.Add(new Attribute(AttributeTypes, AttributeTypeLocation, ColonLocation, Name, Arguments, SpanFrom(AttributeStart)));
			}
			while (Peek().Type == TokenType.Comma);

			RightBracketLocation = VerifyExpectedToken(TokenType.GreaterThan);

			return new AttributeCollection(Attributes, CommaLocations, RightBracketLocation, SpanFrom(Start));
		}

		private AttributeBlockCollection ParseAttributes(AttributeTypes attributeTypesAllowed = AttributeTypes.Regular)
		{
			var Start = Peek();
			var AttributeBlocks = new List<AttributeCollection>();

			while (Peek().Type == TokenType.LessThan)
				AttributeBlocks.Add(ParseAttributeBlock(attributeTypesAllowed));

			if (AttributeBlocks.Count == 0)
			{
				return null;
			}
			else
			{
				return new AttributeBlockCollection(AttributeBlocks, SpanFrom(Start));
			}
		}

		// *
		// * Declaration statements
		// *

		private NameCollection ParseNameList(bool allowLeadingMeOrMyBase = false)
		{
			var Start = Read();
			var CommaLocations = new List<Location>();
			var Names = new List<Name>();

			do
			{
				if (Names.Count > 0)
				{
					CommaLocations.Add(ReadLocation());
				}

				Names.Add(ParseNameListName(allowLeadingMeOrMyBase));

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Comma);
				}
			}
			while (Peek().Type == TokenType.Comma);

			return new NameCollection(Names, CommaLocations, SpanFrom(Start));
		}

		private Declaration ParsePropertyDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes RegularValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Shared | ModifierTypes.Overridable | ModifierTypes.NotOverridable | ModifierTypes.MustOverride | ModifierTypes.Overrides | ModifierTypes.Overloads | ModifierTypes.Default | ModifierTypes.ReadOnly | ModifierTypes.WriteOnly;





			const ModifierTypes InterfaceValidModifiers = ModifierTypes.Shadows | ModifierTypes.Overloads | ModifierTypes.Default | ModifierTypes.ReadOnly | ModifierTypes.WriteOnly;





			ModifierTypes ValidModifiers;
			Location PropertyLocation;
			SimpleName Name;
			ParameterCollection Parameters;
			var AsLocation = default(Location);
			TypeName ReturnType = null;
			AttributeBlockCollection ReturnTypeAttributes = null;
			NameCollection ImplementsList = null;
			DeclarationCollection Accessors = null;
			EndBlockDeclaration EndBlockDeclaration = null;
			bool InInterface = CurrentBlockContextType() == TreeType.InterfaceDeclaration;
			List<CommentType> Comments = null;
			TypeParameterCollection TypeParameters;

			if (InInterface)
			{
				ValidModifiers = InterfaceValidModifiers;
			}
			else
			{
				ValidModifiers = RegularValidModifiers;
			}

			ValidateModifierList(modifiers, ValidModifiers);
			PropertyLocation = ReadLocation();
			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			TypeParameters = ParseTypeParameters();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			if (TypeParameters is not null && TypeParameters.Count > 0)
			{
				ReportSyntaxError(SyntaxErrorType.PropertiesCantBeGeneric, TypeParameters.Span);
			}

			Parameters = ParseParameters();

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				ReturnTypeAttributes = ParseAttributes();
				ReturnType = ParseTypeName(true);

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Implements);
				}
			}

			if (InInterface)
			{
				Comments = ParseTrailingComments();
			}
			else
			{
				if (Peek().Type == TokenType.Implements)
				{
					ImplementsList = ParseNameList();
				}

				if (modifiers is null || (modifiers.ModifierTypes & ModifierTypes.MustOverride) == 0)
				{
					Accessors = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.PropertyDeclaration, ref Comments, ref EndBlockDeclaration);
				}
				else
				{
					Comments = ParseTrailingComments();
				}
			}

			return new PropertyDeclaration(attributes, modifiers, PropertyLocation, Name, Parameters, AsLocation, ReturnTypeAttributes, ReturnType, ImplementsList, Accessors, EndBlockDeclaration, SpanFrom(startLocation), Comments);

		}

		private Declaration ParseExternalDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Overloads;
			Location DeclareLocation;
			var CharsetLocation = default(Location);
			var Charset = VBScript.Parser.Charset.Auto;
			var MethodType = TreeType.SyntaxError;
			var SubOrFunctionLocation = default(Location);
			SimpleName Name;
			var LibLocation = default(Location);
			StringLiteralExpression LibLiteral = null;
			var AliasLocation = default(Location);
			StringLiteralExpression AliasLiteral = null;
			ParameterCollection Parameters;
			var AsLocation = default(Location);
			TypeName ReturnType = null;
			AttributeBlockCollection ReturnTypeAttributes = null;

			ValidateModifierList(modifiers, ValidModifiers);

			DeclareLocation = ReadLocation();

			switch (Peek().AsUnreservedKeyword())
			{
				case TokenType.Ansi:
					{
						Charset = VBScript.Parser.Charset.Ansi;
						CharsetLocation = ReadLocation();
						break;
					}

				case TokenType.Unicode:
					{
						Charset = VBScript.Parser.Charset.Unicode;
						CharsetLocation = ReadLocation();
						break;
					}

				case TokenType.Auto:
					{
						Charset = VBScript.Parser.Charset.Auto;
						CharsetLocation = ReadLocation();
						break;
					}
			}

			if (Peek().Type == TokenType.Sub)
			{
				MethodType = TreeType.ExternalSubDeclaration;
				SubOrFunctionLocation = ReadLocation();
			}
			else if (Peek().Type == TokenType.Function)
			{
				MethodType = TreeType.ExternalFunctionDeclaration;
				SubOrFunctionLocation = ReadLocation();
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedSubOrFunction, Peek());
			}

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Lib, TokenType.LeftParenthesis);
			}

			if (Peek().Type == TokenType.Lib)
			{
				LibLocation = ReadLocation();

				if (Peek().Type == TokenType.StringLiteral)
				{
					StringLiteralToken Literal = (StringLiteralToken)Read();
					LibLiteral = new StringLiteralExpression(Literal.Literal, Literal.Span);
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
					ResyncAt(TokenType.Alias, TokenType.LeftParenthesis);
				}
			}
			else
			{

				ReportSyntaxError(SyntaxErrorType.ExpectedLib, Peek());
			}

			if (Peek().Type == TokenType.Alias)
			{
				AliasLocation = ReadLocation();

				if (Peek().Type == TokenType.StringLiteral)
				{
					StringLiteralToken Literal = (StringLiteralToken)Read();
					AliasLiteral = new StringLiteralExpression(Literal.Literal, Literal.Span);
				}
				else
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
					ResyncAt(TokenType.LeftParenthesis);
				}
			}

			Parameters = ParseParameters();

			if (MethodType == TreeType.ExternalFunctionDeclaration)
			{
				if (Peek().Type == TokenType.As)
				{
					AsLocation = ReadLocation();
					ReturnTypeAttributes = ParseAttributes();
					ReturnType = ParseTypeName(true);

					if (ErrorInConstruct)
					{
						ResyncAt();
					}
				}

				return new ExternalFunctionDeclaration(attributes, modifiers, DeclareLocation, CharsetLocation, Charset, SubOrFunctionLocation, Name, LibLocation, LibLiteral, AliasLocation, AliasLiteral, Parameters, AsLocation, ReturnTypeAttributes, ReturnType, SpanFrom(startLocation), ParseTrailingComments());

			}
			else if (MethodType == TreeType.ExternalSubDeclaration)
			{
				return new ExternalSubDeclaration(attributes, modifiers, DeclareLocation, CharsetLocation, Charset, SubOrFunctionLocation, Name, LibLocation, LibLiteral, AliasLocation, AliasLiteral, Parameters, SpanFrom(startLocation), ParseTrailingComments());

			}
			else
			{
				return Declaration.GetBadDeclaration(SpanFrom(startLocation), ParseTrailingComments());
			}
		}

		private Declaration ParseMethodDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidMethodModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Shared | ModifierTypes.Overridable | ModifierTypes.NotOverridable | ModifierTypes.MustOverride | ModifierTypes.Overrides | ModifierTypes.Overloads;




			const ModifierTypes ValidConstructorModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shared;
			const ModifierTypes ValidInterfaceModifiers = ModifierTypes.Shadows | ModifierTypes.Overloads;

			TreeType MethodType;
			Location SubOrFunctionLocation;
			SimpleName Name;
			ParameterCollection Parameters;
			var AsLocation = default(Location);
			TypeName ReturnType = null;
			AttributeBlockCollection ReturnTypeAttributes = null;
			NameCollection ImplementsList = null;
			NameCollection HandlesList = null;
			bool AllowKeywordsForName = false;
			var ValidModifiers = ValidMethodModifiers;
			StatementCollection Statements = null;
			Statement EndStatement = null;
			EndBlockDeclaration EndDeclaration = null;
			bool InInterface = CurrentBlockContextType() == TreeType.InterfaceDeclaration;
			List<CommentType> Comments = null;
			TypeParameterCollection TypeParameters = null;

			if (!AtBeginningOfLine)
			{
				ReportSyntaxError(SyntaxErrorType.MethodMustBeFirstStatementOnLine, Peek());
			}

			if (Peek().Type == TokenType.Sub)
			{
				SubOrFunctionLocation = ReadLocation();

				if (Peek().Type == TokenType.New)
				{
					MethodType = TreeType.ConstructorDeclaration;
					AllowKeywordsForName = true;
					ValidModifiers = ValidConstructorModifiers;
				}
				else
				{
					MethodType = TreeType.SubDeclaration;
				}
			}
			else
			{
				SubOrFunctionLocation = ReadLocation();
				MethodType = TreeType.FunctionDeclaration;
			}

			if (InInterface)
			{
				ValidModifiers = ValidInterfaceModifiers;
			}

			ValidateModifierList(modifiers, ValidModifiers);
			Name = ParseSimpleName(AllowKeywordsForName);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			TypeParameters = ParseTypeParameters();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			if (MethodType == TreeType.ConstructorDeclaration && TypeParameters is not null && TypeParameters.Count > 0)
			{
				ReportSyntaxError(SyntaxErrorType.ConstructorsCantBeGeneric, TypeParameters.Span);
			}

			Parameters = ParseParameters();

			if (MethodType == TreeType.FunctionDeclaration && Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				ReturnTypeAttributes = ParseAttributes();
				ReturnType = ParseTypeName(true);

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Implements, TokenType.Handles);
				}
			}

			if (InInterface)
			{
				Comments = ParseTrailingComments();
			}
			else
			{
				if (Peek().Type == TokenType.Implements)
				{
					ImplementsList = ParseNameList();
				}
				else if (Peek().Type == TokenType.Handles)
				{
					HandlesList = ParseNameList(true);
				}

				if (modifiers is null || (modifiers.ModifierTypes & ModifierTypes.MustOverride) == 0)
				{
					Statements = ParseStatementBlock(SpanFrom(startLocation), MethodType, ref Comments, ref EndStatement);
				}
				else
				{
					Comments = ParseTrailingComments();
				}

				if (EndStatement is not null)
				{
					EndDeclaration = new EndBlockDeclaration((EndBlockStatement)EndStatement);
				}
			}

			if (MethodType == TreeType.SubDeclaration)
			{
				return new SubDeclaration(attributes, modifiers, SubOrFunctionLocation, Name, TypeParameters, Parameters, ImplementsList, HandlesList, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
			else if (MethodType == TreeType.FunctionDeclaration)
			{
				return new FunctionDeclaration(attributes, modifiers, SubOrFunctionLocation, Name, TypeParameters, Parameters, AsLocation, ReturnTypeAttributes, ReturnType, ImplementsList, HandlesList, Statements, EndDeclaration, SpanFrom(startLocation), Comments);

			}
			else
			{
				return new ConstructorDeclaration(attributes, modifiers, SubOrFunctionLocation, Name, Parameters, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
		}


		private Declaration ParseOperatorDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidOperatorModifiers = ModifierTypes.Shared | ModifierTypes.Public | ModifierTypes.Shadows | ModifierTypes.Overloads | ModifierTypes.Widening | ModifierTypes.Narrowing;


			Location KeywordLocation;
			Token OperatorToken = null;
			ParameterCollection Parameters;
			var AsLocation = default(Location);
			TypeName ReturnType = null;
			AttributeBlockCollection ReturnTypeAttributes = null;
			var ValidModifiers = ValidOperatorModifiers;
			StatementCollection Statements = null;
			Statement EndStatement = null;
			EndBlockDeclaration EndDeclaration = null;
			List<CommentType> Comments = null;
			TypeParameterCollection TypeParameters;

			if (!AtBeginningOfLine)
			{
				ReportSyntaxError(SyntaxErrorType.MethodMustBeFirstStatementOnLine, Peek());
			}

			KeywordLocation = ReadLocation();
			ValidateModifierList(modifiers, ValidModifiers);

			if (IsOverloadableOperator(Peek()))
			{
				OperatorToken = Read();
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.InvalidOperator, Peek());
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			TypeParameters = ParseTypeParameters();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			if (TypeParameters is not null && TypeParameters.Count > 0)
			{
				ReportSyntaxError(SyntaxErrorType.OperatorsCantBeGeneric, TypeParameters.Span);
			}

			Parameters = ParseParameters();

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				ReturnTypeAttributes = ParseAttributes();
				ReturnType = ParseTypeName(true);

				if (ErrorInConstruct)
				{
					ResyncAt();
				}
			}

			Statements = ParseStatementBlock(SpanFrom(startLocation), TreeType.OperatorDeclaration, ref Comments, ref EndStatement);
			Comments = ParseTrailingComments();

			if (EndStatement is not null)
			{
				EndDeclaration = new EndBlockDeclaration((EndBlockStatement)EndStatement);
			}

			return new OperatorDeclaration(attributes, modifiers, KeywordLocation, OperatorToken, Parameters, AsLocation, ReturnTypeAttributes, ReturnType, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
		}

		private Declaration ParseAccessorDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			TreeType AccessorType;
			Location GetOrSetLocation;
			ParameterCollection Parameters = null;
			StatementCollection Statements;
			Statement EndStatement = null;
			EndBlockDeclaration EndDeclaration = null;
			List<CommentType> Comments = null;
			var ValidModifiers = ModifierTypes.None;

			if (Scanner.Version > LanguageVersion.VisualBasic71)
			{
				ValidModifiers = ValidModifiers | ModifierTypes.AccessModifiers;
			}

			if (!AtBeginningOfLine)
			{
				ReportSyntaxError(SyntaxErrorType.MethodMustBeFirstStatementOnLine, Peek());
			}

			ValidateModifierList(modifiers, ValidModifiers);

			if (Peek().Type == TokenType.Get)
			{
				AccessorType = TreeType.GetAccessorDeclaration;
			}
			else
			{
				AccessorType = TreeType.SetAccessorDeclaration;
			}
			GetOrSetLocation = ReadLocation();

			if (AccessorType == TreeType.SetAccessorDeclaration)
			{
				Parameters = ParseParameters();
			}

			Statements = ParseStatementBlock(SpanFrom(startLocation), AccessorType, ref Comments, ref EndStatement);

			if (EndStatement is not null)
			{
				EndDeclaration = new EndBlockDeclaration((EndBlockStatement)EndStatement);
			}

			if (AccessorType == TreeType.GetAccessorDeclaration)
			{
				return new GetAccessorDeclaration(attributes, modifiers, GetOrSetLocation, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
			else
			{
				return new SetAccessorDeclaration(attributes, modifiers, GetOrSetLocation, Parameters, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
		}

		private Declaration ParseCustomEventDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Shared;

			Location CustomLocation, EventLocation;
			SimpleName Name;
			Location AsLocation;
			TypeName EventType;
			NameCollection ImplementsList = null;
			DeclarationCollection Accessors = null;
			EndBlockDeclaration EndBlockDeclaration = null;
			List<CommentType> Comments = null;

			ValidateModifierList(modifiers, ValidModifiers);
			CustomLocation = ReadLocation();
			Debug.Assert(Peek().Type == TokenType.Event);
			EventLocation = ReadLocation();

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.As);
			}

			AsLocation = VerifyExpectedToken(TokenType.As);
			EventType = ParseTypeName(true);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Implements);
			}

			if (Peek().Type == TokenType.Implements)
			{
				ImplementsList = ParseNameList();
			}

			Accessors = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.CustomEventDeclaration, ref Comments, ref EndBlockDeclaration);

			return new CustomEventDeclaration(attributes, modifiers, CustomLocation, EventLocation, Name, AsLocation, EventType, ImplementsList, Accessors, EndBlockDeclaration, SpanFrom(startLocation), Comments);

		}

		private Declaration ParseEventAccessorDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidModifiers = ModifierTypes.None;
			TreeType AccessorType;
			Location AccessorTypeLocation;
			ParameterCollection Parameters = null;
			StatementCollection Statements;
			Statement EndStatement = null;
			EndBlockDeclaration EndDeclaration = null;
			List<CommentType> Comments = null;

			if (!AtBeginningOfLine)
			{
				ReportSyntaxError(SyntaxErrorType.MethodMustBeFirstStatementOnLine, Peek());
			}

			ValidateModifierList(modifiers, ValidModifiers);

			if (Peek().Type == TokenType.AddHandler)
			{
				AccessorType = TreeType.AddHandlerAccessorDeclaration;
			}
			else if (Peek().Type == TokenType.RemoveHandler)
			{
				AccessorType = TreeType.RemoveHandlerAccessorDeclaration;
			}
			else
			{
				AccessorType = TreeType.RaiseEventAccessorDeclaration;
			}
			AccessorTypeLocation = ReadLocation();

			Parameters = ParseParameters();
			Statements = ParseStatementBlock(SpanFrom(startLocation), AccessorType, ref Comments, ref EndStatement);

			if (EndStatement is not null)
			{
				EndDeclaration = new EndBlockDeclaration((EndBlockStatement)EndStatement);
			}

			if (AccessorType == TreeType.AddHandlerAccessorDeclaration)
			{
				return new AddHandlerAccessorDeclaration(attributes, AccessorTypeLocation, Parameters, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
			else if (AccessorType == TreeType.RemoveHandlerAccessorDeclaration)
			{
				return new RemoveHandlerAccessorDeclaration(attributes, AccessorTypeLocation, Parameters, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
			else
			{
				return new RaiseEventAccessorDeclaration(attributes, AccessorTypeLocation, Parameters, Statements, EndDeclaration, SpanFrom(startLocation), Comments);
			}
		}

		private Declaration ParseEventDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes RegularValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Shared;
			const ModifierTypes InterfaceValidModifiers = ModifierTypes.Shadows;

			Location EventLocation;
			SimpleName Name;
			var AsLocation = default(Location);
			TypeName EventType = null;
			ParameterCollection Parameters = null;
			NameCollection ImplementsList = null;
			bool InInterface = CurrentBlockContextType() == TreeType.InterfaceDeclaration;
			ModifierTypes ValidModifiers;

			if (InInterface)
			{
				ValidModifiers = InterfaceValidModifiers;
			}
			else
			{
				ValidModifiers = RegularValidModifiers;
			}

			ValidateModifierList(modifiers, ValidModifiers);

			EventLocation = ReadLocation();
			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.As, TokenType.LeftParenthesis, TokenType.Implements);
			}

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				EventType = ParseTypeName(false);

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Implements);
				}
			}
			else
			{
				Parameters = ParseParameters();

				// Give a good error if they attempt to do a return type
				if (Peek().Type == TokenType.As)
				{
					var ErrorStart = Peek();

					ResyncAt(TokenType.Implements);
					ReportSyntaxError(SyntaxErrorType.EventsCantBeFunctions, ErrorStart, Peek());
				}
			}

			if (Peek().Type == TokenType.Implements)
			{
				ImplementsList = ParseNameList();
			}

			return new EventDeclaration(attributes, modifiers, EventLocation, Name, Parameters, AsLocation, null, EventType, ImplementsList, SpanFrom(startLocation), ParseTrailingComments());
		}

		private Declaration ParseVariableListDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			ModifierTypes ValidModifiers;

			if (modifiers is not null && (modifiers.ModifierTypes & ModifierTypes.Const) != 0)
			{
				ValidModifiers = ModifierTypes.Const | ModifierTypes.AccessModifiers | ModifierTypes.Shadows;
			}
			else
			{
				ValidModifiers = ModifierTypes.Dim | ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Shared | ModifierTypes.ReadOnly | ModifierTypes.WithEvents;

			}

			ValidateModifierList(modifiers, ValidModifiers);

			if (modifiers is null)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedModifier, Peek());
			}

			return new VariableListDeclaration(attributes, modifiers, ParseVariableDeclarators(), SpanFrom(startLocation), ParseTrailingComments());
		}

		private Declaration ParseEndDeclaration()
		{
			var Start = Read();
			var EndType = GetBlockType(Peek().Type);

			switch (EndType)
			{
				case BlockType.Sub:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndSubNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.Function:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndFunctionNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.Operator:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndOperatorNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.Get:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndGetNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.Set:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndSetNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.AddHandler:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndAddHandlerNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.RemoveHandler:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndRemoveHandlerNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.RaiseEvent:
					{
						if (!AtBeginningOfLine)
						{
							ReportSyntaxError(SyntaxErrorType.EndRaiseEventNotAtLineStart, SpanFrom(Start));
						}

						break;
					}

				case BlockType.None:
					{
						ReportSyntaxError(SyntaxErrorType.UnrecognizedEnd, Peek());
						return Declaration.GetBadDeclaration(SpanFrom(Start), ParseTrailingComments());
					}
			}

			return new EndBlockDeclaration(EndType, ReadLocation(), SpanFrom(Start), ParseTrailingComments());
		}

		private Declaration ParseTypeDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers, TreeType blockType)
		{
			ModifierTypes ValidModifiers;
			Location KeywordLocation;
			SimpleName Name;
			DeclarationCollection Members;
			EndBlockDeclaration EndBlockDeclaration = null;
			List<CommentType> Comments = null;
			TypeParameterCollection TypeParameters = null;

			if (blockType == TreeType.ModuleDeclaration)
			{
				ValidModifiers = ModifierTypes.AccessModifiers;
			}
			else
			{
				ValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows;

				if (blockType == TreeType.ClassDeclaration)
				{
					ValidModifiers = ValidModifiers | ModifierTypes.MustInherit | ModifierTypes.NotInheritable;
				}

				if (blockType == TreeType.ClassDeclaration || blockType == TreeType.StructureDeclaration)
				{
					ValidModifiers = ValidModifiers | ModifierTypes.Partial;
				}
			}

			ValidateModifierList(modifiers, ValidModifiers);

			KeywordLocation = ReadLocation();

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			TypeParameters = ParseTypeParameters();

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			if (blockType == TreeType.ModuleDeclaration && TypeParameters is not null && TypeParameters.Count > 0)
			{
				ReportSyntaxError(SyntaxErrorType.ModulesCantBeGeneric, TypeParameters.Span);
			}

			Members = ParseDeclarationBlock(SpanFrom(startLocation), blockType, ref Comments, ref EndBlockDeclaration);

			switch (blockType)
			{
				case TreeType.ClassDeclaration:
					{
						return new ClassDeclaration(attributes, modifiers, KeywordLocation, Name, TypeParameters, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments);
					}

				case TreeType.ModuleDeclaration:
					{
						return new ModuleDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments);
					}

				case TreeType.InterfaceDeclaration:
					{
						return new InterfaceDeclaration(attributes, modifiers, KeywordLocation, Name, TypeParameters, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments);
					}

				case TreeType.StructureDeclaration:
					{
						return new StructureDeclaration(attributes, modifiers, KeywordLocation, Name, TypeParameters, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments);
					}

				default:
					{
						Debug.Assert(false, "unexpected!");
						return null;
					}
			}
		}

		private Declaration ParseEnumDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows;
			Location KeywordLocation;
			SimpleName Name;
			var AsLocation = default(Location);
			TypeName Type = null;
			DeclarationCollection Members;
			EndBlockDeclaration EndBlockDeclaration = null;
			List<CommentType> Comments = null;

			ValidateModifierList(modifiers, ValidModifiers);

			KeywordLocation = ReadLocation();

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.As);
			}

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				Type = ParseTypeName(false);

				if (ErrorInConstruct)
				{
					ResyncAt();
				}
			}

			Members = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.EnumDeclaration, ref Comments, ref EndBlockDeclaration);

			if (Members is null || Members.Count == 0)
			{
				ReportSyntaxError(SyntaxErrorType.EmptyEnum, Name.Span);
			}

			return new EnumDeclaration(attributes, modifiers, KeywordLocation, Name, AsLocation, Type, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments);
		}

		private Declaration ParseDelegateDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			const ModifierTypes ValidModifiers = ModifierTypes.AccessModifiers | ModifierTypes.Shadows | ModifierTypes.Shared;
			Location DelegateLocation;
			var MethodType = TreeType.SyntaxError;
			var SubOrFunctionLocation = default(Location);
			SimpleName Name;
			ParameterCollection Parameters;
			var AsLocation = default(Location);
			TypeName ReturnType = null;
			AttributeBlockCollection ReturnTypeAttributes = null;
			TypeParameterCollection TypeParameters = null;

			ValidateModifierList(modifiers, ValidModifiers);

			DelegateLocation = ReadLocation();

			if (Peek().Type == TokenType.Sub)
			{
				SubOrFunctionLocation = ReadLocation();
				MethodType = TreeType.SubDeclaration;
			}
			else if (Peek().Type == TokenType.Function)
			{
				SubOrFunctionLocation = ReadLocation();
				MethodType = TreeType.FunctionDeclaration;
			}
			else
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedSubOrFunction, Peek());
				MethodType = TreeType.SubDeclaration;
			}

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			TypeParameters = ParseTypeParameters();

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.LeftParenthesis, TokenType.As);
			}

			Parameters = ParseParameters();

			if (MethodType == TreeType.FunctionDeclaration && Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				ReturnTypeAttributes = ParseAttributes();
				ReturnType = ParseTypeName(true);

				if (ErrorInConstruct)
				{
					ResyncAt();
				}
			}

			if (MethodType == TreeType.SubDeclaration)
			{
				return new DelegateSubDeclaration(attributes, modifiers, DelegateLocation, SubOrFunctionLocation, Name, TypeParameters, Parameters, SpanFrom(startLocation), ParseTrailingComments());
			}
			else
			{
				return new DelegateFunctionDeclaration(attributes, modifiers, DelegateLocation, SubOrFunctionLocation, Name, TypeParameters, Parameters, AsLocation, ReturnTypeAttributes, ReturnType, SpanFrom(startLocation), ParseTrailingComments());
			}
		}

		private Declaration ParseTypeListDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers, TreeType listType)
		{
			var CommaLocations = new List<Location>();
			var Types = new List<TypeName>();
			Token ListStart;

			Read();

			if (attributes is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnTypeListDeclaration, attributes.Span);
			}

			if (modifiers is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnTypeListDeclaration, modifiers.Span);
			}

			ListStart = Peek();

			do
			{
				if (Types.Count > 0)
				{
					CommaLocations.Add(ReadLocation());
				}

				Types.Add(ParseTypeName(false));

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Comma);
				}
			}
			while (Peek().Type == TokenType.Comma);

			if (listType == TreeType.InheritsDeclaration)
			{
				return new InheritsDeclaration(new TypeNameCollection(Types, CommaLocations, SpanFrom(ListStart)), SpanFrom(startLocation), ParseTrailingComments());
			}
			else
			{
				return new ImplementsDeclaration(new TypeNameCollection(Types, CommaLocations, SpanFrom(ListStart)), SpanFrom(startLocation), ParseTrailingComments());
			}
		}

		private Declaration ParseNamespaceDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			Location KeywordLocation;
			Name Name;
			DeclarationCollection Members;
			EndBlockDeclaration EndBlockDeclaration = null;
			List<CommentType> Comments = null;

			if (attributes is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnNamespaceDeclaration, attributes.Span);
			}

			if (modifiers is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnNamespaceDeclaration, modifiers.Span);
			}

			KeywordLocation = ReadLocation();

			Name = ParseName(false);

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			Members = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.NamespaceDeclaration, ref Comments, ref EndBlockDeclaration);

			return new NamespaceDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments);
		}

		private Declaration ParseImportsDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			var ImportMembers = new List<Import>();
			var CommaLocations = new List<Location>();
			Token ListStart;

			if (attributes is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnImportsDeclaration, attributes.Span);
			}

			if (modifiers is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnImportsDeclaration, modifiers.Span);
			}

			Read();
			ListStart = Peek();

			do
			{
				if (ImportMembers.Count > 0)
				{
					CommaLocations.Add(ReadLocation());
				}

				if (PeekAheadFor(TokenType.Equals, TokenType.Comma, TokenType.Period) == TokenType.Equals)
				{
					var ImportStart = Peek();
					SimpleName Name;
					Location EqualsLocation;
					TypeName AliasedTypeName;

					Name = ParseSimpleName(false);
					EqualsLocation = ReadLocation();
					AliasedTypeName = ParseNamedTypeName(false);

					if (ErrorInConstruct)
					{
						ResyncAt();
					}

					ImportMembers.Add(new AliasImport(Name, EqualsLocation, AliasedTypeName, SpanFrom(ImportStart)));
				}
				else
				{
					var ImportStart = Peek();
					TypeName TypeName;

					TypeName = ParseNamedTypeName(false);

					if (ErrorInConstruct)
					{
						ResyncAt();
					}

					ImportMembers.Add(new NameImport(TypeName, SpanFrom(ImportStart)));
				}
			}
			while (Peek().Type == TokenType.Comma);

			return new ImportsDeclaration(new ImportCollection(ImportMembers, CommaLocations, SpanFrom(ListStart)), SpanFrom(startLocation), ParseTrailingComments());
		}

		private Declaration ParseOptionDeclaration(Location startLocation, AttributeBlockCollection attributes, ModifierCollection modifiers)
		{
			OptionType OptionType;
			var OptionTypeLocation = default(Location);
			var OptionArgumentLocation = default(Location);

			Read();

			if (attributes is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnOptionDeclaration, attributes.Span);
			}

			if (modifiers is not null)
			{
				ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnOptionDeclaration, modifiers.Span);
			}

			if (Peek().AsUnreservedKeyword() == TokenType.Explicit)
			{
				OptionTypeLocation = ReadLocation();

				if (Peek().AsUnreservedKeyword() == TokenType.Off)
				{
					OptionArgumentLocation = ReadLocation();
					OptionType = VBScript.Parser.OptionType.ExplicitOff;
				}
				else if (Peek().Type == TokenType.On)
				{
					OptionArgumentLocation = ReadLocation();
					OptionType = VBScript.Parser.OptionType.ExplicitOn;
				}
				else if (Peek().Type == TokenType.Identifier)
				{
					OptionType = VBScript.Parser.OptionType.SyntaxError;
					ReportSyntaxError(SyntaxErrorType.InvalidOptionExplicitType, SpanFrom(startLocation));
				}
				else
				{
					OptionType = VBScript.Parser.OptionType.Explicit;
				}
			}
			else if (Peek().AsUnreservedKeyword() == TokenType.Strict)
			{
				OptionTypeLocation = ReadLocation();

				if (Peek().AsUnreservedKeyword() == TokenType.Off)
				{
					OptionArgumentLocation = ReadLocation();
					OptionType = VBScript.Parser.OptionType.StrictOff;
				}
				else if (Peek().Type == TokenType.On)
				{
					OptionArgumentLocation = ReadLocation();
					OptionType = VBScript.Parser.OptionType.StrictOn;
				}
				else if (Peek().Type == TokenType.Identifier)
				{
					OptionType = VBScript.Parser.OptionType.SyntaxError;
					ReportSyntaxError(SyntaxErrorType.InvalidOptionStrictType, SpanFrom(startLocation));
				}
				else
				{
					OptionType = VBScript.Parser.OptionType.Strict;
				}
			}
			else if (Peek().AsUnreservedKeyword() == TokenType.Compare)
			{
				OptionTypeLocation = ReadLocation();

				if (Peek().AsUnreservedKeyword() == TokenType.Binary)
				{
					OptionArgumentLocation = ReadLocation();
					OptionType = VBScript.Parser.OptionType.CompareBinary;
				}
				else if (Peek().AsUnreservedKeyword() == TokenType.Text)
				{
					OptionArgumentLocation = ReadLocation();
					OptionType = VBScript.Parser.OptionType.CompareText;
				}
				else
				{
					OptionType = VBScript.Parser.OptionType.SyntaxError;
					ReportSyntaxError(SyntaxErrorType.InvalidOptionCompareType, SpanFrom(startLocation));
				}
			}
			else
			{
				OptionType = VBScript.Parser.OptionType.SyntaxError;
				ReportSyntaxError(SyntaxErrorType.InvalidOptionType, SpanFrom(startLocation));
			}

			if (ErrorInConstruct)
			{
				ResyncAt();
			}

			return new OptionDeclaration(OptionType, OptionTypeLocation, OptionArgumentLocation, SpanFrom(startLocation), ParseTrailingComments());
		}

		private Declaration ParseAttributeDeclaration()
		{
			AttributeBlockCollection Attributes;

			Attributes = ParseAttributes(AttributeTypes.Module | AttributeTypes.Assembly);

			return new AttributeDeclaration(Attributes, Attributes.Span, ParseTrailingComments());
		}

		private Declaration ParseDeclaration([Optional] ref Token terminator)
		{
			Token Start;
			Location StartLocation;
			Declaration Declaration = null;
			AttributeBlockCollection Attributes = null;
			ModifierCollection Modifiers = null;
			var LookAhead = TokenType.None;

			// If AtBeginningOfLine Then
			// While ParsePreprocessorStatement(False)
			// ' Loop
			// End While
			// End If

			Start = Peek();

			ErrorInConstruct = false;

			StartLocation = Peek().Span.Start;
			LookAhead = PeekAheadFor(TokenType.Assembly, TokenType.Module, TokenType.GreaterThan);
			if (Peek().Type != TokenType.LessThan || LookAhead != TokenType.Assembly && LookAhead != TokenType.Module)
			{
				Attributes = ParseAttributes();
				Modifiers = ParseDeclarationModifierList();
			}

			switch (Peek().Type)
			{
				case TokenType.End:
					{
						if (Attributes is null && Modifiers is null)
						{
							Declaration = ParseEndDeclaration();
						}
						else
						{
							goto case TokenType.Identifier;
						}

						break;
					}

				case TokenType.Property:
					{
						Declaration = ParsePropertyDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Declare:
					{
						Declaration = ParseExternalDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Sub:
				case TokenType.Function:
					{
						Declaration = ParseMethodDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Get:
				case TokenType.Set:
					{
						Declaration = ParseAccessorDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.AddHandler:
				case TokenType.RemoveHandler:
				case TokenType.RaiseEvent:
					{
						Declaration = ParseEventAccessorDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Event:
					{
						Declaration = ParseEventDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Operator:
					{
						Declaration = ParseOperatorDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Delegate:
					{
						Declaration = ParseDelegateDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Class:
					{
						Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.ClassDeclaration);
						break;
					}

				case TokenType.Structure:
					{
						Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.StructureDeclaration);
						break;
					}

				case TokenType.Module:
					{
						Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.ModuleDeclaration);
						break;
					}

				case TokenType.Interface:
					{
						Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.InterfaceDeclaration);
						break;
					}

				case TokenType.Enum:
					{
						Declaration = ParseEnumDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Namespace:
					{
						Declaration = ParseNamespaceDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Implements:
					{
						Declaration = ParseTypeListDeclaration(StartLocation, Attributes, Modifiers, TreeType.ImplementsDeclaration);
						break;
					}

				case TokenType.Inherits:
					{
						Declaration = ParseTypeListDeclaration(StartLocation, Attributes, Modifiers, TreeType.InheritsDeclaration);
						break;
					}

				case TokenType.Imports:
					{
						Declaration = ParseImportsDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.Option:
					{
						Declaration = ParseOptionDeclaration(StartLocation, Attributes, Modifiers);
						break;
					}

				case TokenType.LessThan:
					{
						Declaration = ParseAttributeDeclaration();
						break;
					}

				case TokenType.Identifier:
					{
						if (Peek().AsUnreservedKeyword() == TokenType.Custom && PeekAheadOne().Type == TokenType.Event)
						{
							Declaration = ParseCustomEventDeclaration(StartLocation, Attributes, Modifiers);
						}
						else
						{
							Declaration = ParseVariableListDeclaration(StartLocation, Attributes, Modifiers);
						}

						break;
					}

				case TokenType.LineTerminator:
				case TokenType.Colon:
				case TokenType.EndOfStream:
					{
						if (Attributes is null && Modifiers is null)
						{
						}
						// An empty declaration
						else
						{
							ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Peek());
						}

						break;
					}

				case TokenType.Comment:
					{
						var Comments = new List<CommentType>();
						Token LastTerminator;

						do
						{
							CommentToken CommentToken = (CommentToken)Scanner.Read();
							Comments.Add(new CommentType(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span));
							LastTerminator = Read(); // Eat the terminator of the comment
						}
						while (Peek().Type == TokenType.Comment);
						Backtrack(LastTerminator);

						Declaration = new EmptyDeclaration(SpanFrom(Start), Comments);
						break;
					}

				default:
					{
						ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
						break;
					}
			}

			terminator = VerifyEndOfStatement();

			return Declaration;
		}

		private Declaration ParseDeclarationInEnum([Optional] ref Token terminator)
		{
			Token Start;
			Location StartLocation;
			AttributeBlockCollection Attributes;
			SimpleName Name;
			var EqualsLocation = default(Location);
			Expression Expression = null;
			Declaration Declaration = null;

			// If AtBeginningOfLine Then
			// While ParsePreprocessorStatement(False)
			// ' Loop
			// End While
			// End If

			Start = Peek();

			if (Start.Type == TokenType.Comment)
			{
				var Comments = new List<CommentType>();
				Token LastTerminator;

				do
				{
					CommentToken CommentToken = (CommentToken)Scanner.Read();
					Comments.Add(new CommentType(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span));
					LastTerminator = Read(); // Eat the terminator of the comment
				}
				while (Peek().Type == TokenType.Comment);
				Backtrack(LastTerminator);

				Declaration = new EmptyDeclaration(SpanFrom(Start), Comments);
				goto HaveStatement;
			}

			if (Start.Type == TokenType.LineTerminator || Start.Type == TokenType.Colon)
			{
				goto HaveStatement;
			}

			ErrorInConstruct = false;

			StartLocation = Peek().Span.Start;
			Attributes = ParseAttributes();

			if (Peek().Type == TokenType.End && Attributes is null)
			{
				Declaration = ParseEndDeclaration();
				goto HaveStatement;
			}

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Equals);
			}

			if (Peek().Type == TokenType.Equals)
			{
				EqualsLocation = ReadLocation();
				Expression = ParseExpression();

				if (ErrorInConstruct)
				{
					ResyncAt();
				}
			}

			Declaration = new EnumValueDeclaration(Attributes, Name, EqualsLocation, Expression, SpanFrom(StartLocation), ParseTrailingComments());

		HaveStatement:
			;

			terminator = VerifyEndOfStatement();

			return Declaration;
		}

		private DeclarationCollection ParseDeclarationBlock(Span blockStartSpan, TreeType blockType, ref List<CommentType> Comments, [Optional] ref EndBlockDeclaration endDeclaration)
		{
			var Declarations = new List<Declaration>();
			var ColonLocations = new List<Location>();
			Token Terminator;
			Token Start;
			Location DeclarationsEnd;
			bool BlockTerminated = false;

			Comments = ParseTrailingComments();
			Terminator = VerifyEndOfStatement();

			if (Terminator.Type == TokenType.Colon)
			{
				ColonLocations.Add(Terminator.Span.Start);
			}

			Start = Peek();
			DeclarationsEnd = Start.Span.Finish;
			endDeclaration = null;

			PushBlockContext(blockType);

			while (Peek().Type != TokenType.EndOfStream)
			{
				var PreviousTerminator = Terminator;
				Declaration Declaration;

				if (blockType == TreeType.EnumDeclaration)
				{
					Declaration = ParseDeclarationInEnum(ref Terminator);
				}
				else
				{
					Declaration = ParseDeclaration(ref Terminator);
				}

				if (Declaration is not null)
				{
					SyntaxErrorType ErrorType;

					if (Declaration.Type == TreeType.EndBlockDeclaration)
					{
						EndBlockDeclaration PotentialEndDeclaration = (EndBlockDeclaration)Declaration;

						if (DeclarationEndsBlock(blockType, PotentialEndDeclaration))
						{
							endDeclaration = PotentialEndDeclaration;
							Backtrack(Terminator);
							BlockTerminated = true;
							break;
						}
						else
						{
							bool DeclarationEndsOuterBlock = false;

							// If the end Declaration matches an outer block context, then we want to unwind
							// up to that level. Otherwise, we want to just give an error and keep going.
							foreach (TreeType BlockContext in BlockContextStack)
							{
								if (DeclarationEndsBlock(BlockContext, PotentialEndDeclaration))
								{
									DeclarationEndsOuterBlock = true;
									break;
								}
							}

							if (DeclarationEndsOuterBlock)
							{
								ReportMismatchedEndError(blockType, Declaration.Span);
								// CONSIDER: Can we avoid parsing and re-parsing this declaration?
								Backtrack(PreviousTerminator);
								// We consider the block terminated.
								BlockTerminated = true;
								break;
							}
							else
							{
								ReportMissingBeginDeclarationError(PotentialEndDeclaration);
							}
						}
					}
					else
					{
						ErrorType = ValidDeclaration(blockType, Declaration, Declarations);

						if (ErrorType != SyntaxErrorType.None)
						{
							ReportSyntaxError(ErrorType, Declaration.Span);
						}
					}

					Declarations.Add(Declaration);
				}

				if (Terminator.Type == TokenType.Colon)
				{
					ColonLocations.Add(Terminator.Span.Start);
				}

				DeclarationsEnd = Terminator.Span.Finish;
			}

			if (!BlockTerminated)
			{
				ReportMismatchedEndError(blockType, blockStartSpan);
			}

			PopBlockContext();

			if (Declarations.Count == 0 && ColonLocations.Count == 0)
			{
				return null;
			}
			else
			{
				return new DeclarationCollection(Declarations, ColonLocations, new Span(Start.Span.Start, DeclarationsEnd));
			}
		}

		// *
		// * Parameters
		// *

		private Parameter ParseParameter()
		{
			var Start = Peek();
			AttributeBlockCollection Attributes;
			ModifierCollection Modifiers;
			VariableName VariableName;
			var AsLocation = default(Location);
			TypeName Type = null;
			var EqualsLocation = default(Location);
			Initializer Initializer = null;

			Attributes = ParseAttributes();
			Modifiers = ParseParameterModifierList();
			VariableName = ParseVariableName(false);

			if (ErrorInConstruct)
			{
				// If we see As before a comma or RParen, then assume that
				// we are still on the same parameter. Otherwise, don't resync
				// and allow the caller to decide how to recover.
				if (PeekAheadFor(TokenType.As, TokenType.Comma, TokenType.RightParenthesis) == TokenType.As)
				{
					ResyncAt(TokenType.As);
				}
			}

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				Type = ParseTypeName(true);
			}

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Equals, TokenType.Comma, TokenType.RightParenthesis);
			}

			if (Peek().Type == TokenType.Equals)
			{
				EqualsLocation = ReadLocation();
				Initializer = ParseInitializer();
			}

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
			}

			return new Parameter(Attributes, Modifiers, VariableName, AsLocation, Type, EqualsLocation, Initializer, SpanFrom(Start));
		}

		private bool ParametersContinue()
		{
			var NextToken = Peek();

			if (NextToken.Type == TokenType.Comma)
			{
				return true;
			}
			else if (NextToken.Type == TokenType.RightParenthesis || MustEndStatement(NextToken))
			{
				return false;
			}

			ReportSyntaxError(SyntaxErrorType.ParameterSyntax, NextToken);
			ResyncAt(TokenType.Comma, TokenType.RightParenthesis);

			if (Peek().Type == TokenType.Comma)
			{
				ErrorInConstruct = false;
				return true;
			}

			return false;
		}

		private ParameterCollection ParseParameters()
		{
			var Start = Peek();
			var Parameters = new List<Parameter>();
			var CommaLocations = new List<Location>();
			var RightParenthesisLocation = default(Location);

			if (Start.Type != TokenType.LeftParenthesis)
			{
				return null;
			}
			else
			{
				Read();
			}

			if (Peek().Type != TokenType.RightParenthesis)
			{
				do
				{
					if (Parameters.Count > 0)
					{
						CommaLocations.Add(ReadLocation());
					}

					Parameters.Add(ParseParameter());

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
					}
				}
				while (ParametersContinue());
			}

			if (Peek().Type == TokenType.RightParenthesis)
			{
				RightParenthesisLocation = ReadLocation();
			}
			else
			{
				var CurrentToken = Peek();

				// On error, peek for ")" with "(". If ")" seen before 
				// "(", then sync on that. Otherwise, assume missing ")"
				// and let caller decide.
				ResyncAt(TokenType.LeftParenthesis, TokenType.RightParenthesis);

				if (Peek().Type == TokenType.RightParenthesis)
				{
					ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek());
					RightParenthesisLocation = ReadLocation();
				}
				else
				{
					Backtrack(CurrentToken);
					ReportSyntaxError(SyntaxErrorType.ExpectedRightParenthesis, Peek());
				}
			}

			return new ParameterCollection(Parameters, CommaLocations, RightParenthesisLocation, SpanFrom(Start));
		}

		// *
		// * Type Parameters
		// *

		private TypeConstraintCollection ParseTypeConstraints()
		{
			var Start = Peek();
			var CommaLocations = new List<Location>();
			var Types = new List<TypeName>();
			var RightBracketLocation = default(Location);

			if (Peek().Type == TokenType.LeftCurlyBrace)
			{
				Read();

				do
				{
					if (Types.Count > 0)
					{
						CommaLocations.Add(ReadLocation());
					}

					Types.Add(ParseTypeName(true));

					if (ErrorInConstruct)
					{
						ResyncAt(TokenType.Comma);
					}
				}
				while (Peek().Type == TokenType.Comma);

				RightBracketLocation = VerifyExpectedToken(TokenType.RightCurlyBrace);
			}
			else
			{
				Types.Add(ParseTypeName(true));
			}

			return new TypeConstraintCollection(Types, CommaLocations, RightBracketLocation, SpanFrom(Start));
		}

		private TypeParameter ParseTypeParameter()
		{
			var Start = Peek();
			SimpleName Name;
			var AsLocation = default(Location);
			TypeConstraintCollection TypeConstraints = null;

			Name = ParseSimpleName(false);

			if (ErrorInConstruct)
			{
				// If we see As before a comma or RParen, then assume that
				// we are still on the same parameter. Otherwise, don't resync
				// and allow the caller to decide how to recover.
				if (PeekAheadFor(TokenType.As, TokenType.Comma, TokenType.RightParenthesis) == TokenType.As)
				{
					ResyncAt(TokenType.As);
				}
			}

			if (Peek().Type == TokenType.As)
			{
				AsLocation = ReadLocation();
				TypeConstraints = ParseTypeConstraints();
			}

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Equals, TokenType.Comma, TokenType.RightParenthesis);
			}

			if (ErrorInConstruct)
			{
				ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
			}

			return new TypeParameter(Name, AsLocation, TypeConstraints, SpanFrom(Start));
		}

		private TypeParameterCollection ParseTypeParameters()
		{
			var Start = Peek();
			Location OfLocation;
			var TypeParameters = new List<TypeParameter>();
			var CommaLocations = new List<Location>();
			Location RightParenthesisLocation;

			if (Start.Type != TokenType.LeftParenthesis || Scanner.Version < LanguageVersion.VisualBasic80)
			{
				return null;
			}
			else
			{
				Read();

				if (Peek().Type != TokenType.Of || Scanner.Version < LanguageVersion.VisualBasic80)
				{
					Backtrack(Start);

					return null;
				}
			}

			OfLocation = VerifyExpectedToken(TokenType.Of);

			do
			{
				if (TypeParameters.Count > 0)
				{
					CommaLocations.Add(ReadLocation());
				}

				TypeParameters.Add(ParseTypeParameter());

				if (ErrorInConstruct)
				{
					ResyncAt(TokenType.Comma, TokenType.RightParenthesis);
				}
			}
			while (ParametersContinue());

			RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis);

			return new TypeParameterCollection(OfLocation, TypeParameters, CommaLocations, RightParenthesisLocation, SpanFrom(Start));
		}

		// *
		// * Files
		// *

		private File ParseFile()
		{
			var Declarations = new List<Declaration>();
			var ColonLocations = new List<Location>();
			Token Terminator = null;
			var Start = Peek();

			while (Peek().Type != TokenType.EndOfStream)
			{
				Declaration Declaration;

				Declaration = ParseDeclaration(ref Terminator);

				if (Declaration is not null)
				{
					var ErrorType = SyntaxErrorType.None;

					ErrorType = ValidDeclaration(TreeType.File, Declaration, Declarations);

					if (ErrorType != SyntaxErrorType.None)
					{
						ReportSyntaxError(ErrorType, Declaration.Span);
					}

					Declarations.Add(Declaration);
				}

				if (Terminator.Type == TokenType.Colon)
				{
					ColonLocations.Add(Terminator.Span.Start);
				}
			}

			if (Declarations.Count == 0 && ColonLocations.Count == 0)
			{
				return new File(null, SpanFrom(Start));
			}
			else
			{
				return new File(new DeclarationCollection(Declarations, ColonLocations, SpanFrom(Start)), SpanFrom(Start));
			}
		}
		// LC Parse the script files
		private ScriptBlock ParseScriptFile()
		{
			var Statements = new List<Statement>();
			var ColonLocations = new List<Location>();
			Token Terminator = null;
			Token Start;
			Location StatementsEnd;
			// Dim BlockTerminated As Boolean = False

			Start = Peek();
			StatementsEnd = Start.Span.Finish;

			while (Peek().Type != TokenType.EndOfStream)
			{
				// Dim PreviousTerminator As Token = Terminator
				Statement Statement;

				Statement = ParseStatementOrDeclaration(ref Terminator);

				if (Statement is not null)
				{
					Statements.Add(Statement);
				}

				if (Terminator.Type == TokenType.Colon)
				{
					ColonLocations.Add(Terminator.Span.Start);
					StatementsEnd = Terminator.Span.Finish;
				}
				else
				{
					StatementsEnd = Terminator.Span.Finish;
				}
			}

			if (Statements.Count == 0 && ColonLocations.Count == 0)
			{
				return new ScriptBlock(null, SpanFrom(Start));
			}
			else
			{
				return new ScriptBlock(new StatementCollection(Statements, ColonLocations, new Span(Start.Span.Start, StatementsEnd)), SpanFrom(Start));
			}
		}

		// *
		// * Preprocessor statements
		// *

		private void ParseExternalSourceStatement(Token start)
		{
			long Line;
			string File;

			// Consume the ExternalSource keyword
			Read();

			if (CurrentExternalSourceContext is not null)
			{
				ResyncAt();
				ReportSyntaxError(SyntaxErrorType.NestedExternalSourceStatement, SpanFrom(start));
			}
			else
			{
				VerifyExpectedToken(TokenType.LeftParenthesis);

				if (Peek().Type != TokenType.StringLiteral)
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
					ResyncAt();
					return;
				}

				File = ((StringLiteralToken)Read()).Literal;
				VerifyExpectedToken(TokenType.Comma);

				if (Peek().Type != TokenType.IntegerLiteral)
				{
					ReportSyntaxError(SyntaxErrorType.ExpectedIntegerLiteral, Peek());
					ResyncAt();
					return;
				}

				Line = ((IntegerLiteralToken)Read()).Literal;

				VerifyExpectedToken(TokenType.RightParenthesis);

				CurrentExternalSourceContext = new ExternalSourceContext();
				{
					ref var withBlock = ref CurrentExternalSourceContext;
					withBlock.File = File;
					withBlock.Line = Line;
					withBlock.Start = Peek().Span.Start;
				}
			}
		}

		private void ParseExternalChecksumStatement()
		{
			string Filename, Guid, Checksum;

			// Consume the ExternalChecksum keyword
			Read();
			VerifyExpectedToken(TokenType.LeftParenthesis);

			if (Peek().Type != TokenType.StringLiteral)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
				ResyncAt();
				return;
			}

			Filename = ((StringLiteralToken)Read()).Literal;
			VerifyExpectedToken(TokenType.Comma);

			if (Peek().Type != TokenType.StringLiteral)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
				ResyncAt();
				return;
			}

			Guid = ((StringLiteralToken)Read()).Literal;
			VerifyExpectedToken(TokenType.Comma);

			if (Peek().Type != TokenType.StringLiteral)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
				ResyncAt();
				return;
			}

			Checksum = ((StringLiteralToken)Read()).Literal;
			VerifyExpectedToken(TokenType.RightParenthesis);

			if (ExternalChecksums is not null)
			{
				ExternalChecksums.Add(new ExternalChecksum(Filename, Guid, Checksum));
			}
		}

		private void ParseRegionStatement(Token start, bool statementLevel)
		{
			string Description;
			RegionContext RegionContext;

			if (statementLevel == true)
			{
				ResyncAt();
				ReportSyntaxError(SyntaxErrorType.RegionInsideMethod, SpanFrom(start));
				return;
			}

			// Consume the Region keyword
			Read();

			if (Peek().Type != TokenType.StringLiteral)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek());
				ResyncAt();
				return;
			}

			Description = ((StringLiteralToken)Read()).Literal;

			RegionContext = new RegionContext();
			RegionContext.Description = Description;
			RegionContext.Start = Peek().Span.Start;
			RegionContextStack.Push(RegionContext);
		}

		private void ParseEndPreprocessingStatement(Token start, bool statementLevel)
		{
			// Consume the End keyword
			Read();

			if (Peek().AsUnreservedKeyword() == TokenType.ExternalSource)
			{
				Read();

				if (CurrentExternalSourceContext is null)
				{
					ReportSyntaxError(SyntaxErrorType.EndExternalSourceWithoutExternalSource, SpanFrom(start));
					ResyncAt();
				}
				else
				{
					if (ExternalLineMappings is not null)
					{
						{
							ref var withBlock = ref CurrentExternalSourceContext;
							ExternalLineMappings.Add(new ExternalLineMapping(withBlock.Start, start.Span.Start, withBlock.File, withBlock.Line));
						}
					}
					CurrentExternalSourceContext = null;
				}

				return;
			}
			else if (Peek().AsUnreservedKeyword() == TokenType.Region)
			{
				Read();

				if (statementLevel == true)
				{
					ResyncAt();
					ReportSyntaxError(SyntaxErrorType.RegionInsideMethod, SpanFrom(start));
					return;
				}

				if (RegionContextStack.Count == 0)
				{
					ReportSyntaxError(SyntaxErrorType.EndRegionWithoutRegion, SpanFrom(start));
					ResyncAt();
				}
				else
				{
					var RegionContext = RegionContextStack.Pop();

					if (SourceRegions is not null)
					{
						SourceRegions.Add(new SourceRegion(RegionContext.Start, start.Span.Start, RegionContext.Description));
					}
				}

				return;
			}
			else if (Peek().Type == TokenType.If)
			{
				// Read the If keyword
				Read();

				if (ConditionalCompilationContextStack.Count == 0)
				{
					ReportSyntaxError(SyntaxErrorType.CCEndIfWithoutCCIf, SpanFrom(start));
				}
				else
				{
					ConditionalCompilationContextStack.Pop();
				}

				return;
			}

			ResyncAt();
			ReportSyntaxError(SyntaxErrorType.ExpectedEndKind, Peek());
		}

		// Private Shared Function EvaluateCCLiteral(ByVal expression As LiteralExpression) As Object
		// Select Case expression.Type
		// Case TreeType.IntegerLiteralExpression
		// Return CType(expression, IntegerLiteralExpression).Literal

		// Case TreeType.FloatingPointLiteralExpression
		// Return CType(expression, FloatingPointLiteralExpression).Literal

		// Case TreeType.StringLiteralExpression
		// Return CType(expression, StringLiteralExpression).Literal

		// Case TreeType.CharacterLiteralExpression
		// Return CType(expression, CharacterLiteralExpression).Literal

		// Case TreeType.DateLiteralExpression
		// Return CType(expression, DateLiteralExpression).Literal

		// Case TreeType.DecimalLiteralExpression
		// Return CType(expression, DecimalLiteralExpression).Literal

		// Case TreeType.BooleanLiteralExpression
		// Return CType(expression, BooleanLiteralExpression).Literal

		// Case Else
		// Debug.Assert(False, "Unexpected!")
		// Return Nothing
		// End Select
		// End Function

		private static TypeCode TypeCodeOfCastExpression(IntrinsicType castType)
		{
			switch (castType)
			{
				case IntrinsicType.Boolean:
					{
						return TypeCode.Boolean;
					}

				case IntrinsicType.Byte:
					{
						return TypeCode.Byte;
					}

				case IntrinsicType.Char:
					{
						return TypeCode.Char;
					}

				case IntrinsicType.Date:
					{
						return TypeCode.DateTime;
					}

				case IntrinsicType.Decimal:
					{
						return TypeCode.Decimal;
					}

				case IntrinsicType.Double:
					{
						return TypeCode.Double;
					}

				case IntrinsicType.Integer:
					{
						return TypeCode.Int32;
					}

				case IntrinsicType.Long:
					{
						return TypeCode.Int64;
					}

				case IntrinsicType.Object:
					{
						return TypeCode.Object;
					}

				case IntrinsicType.Short:
					{
						return TypeCode.Int16;
					}

				case IntrinsicType.Single:
					{
						return TypeCode.Single;
					}

				case IntrinsicType.String:
					{
						return TypeCode.String;
					}

				default:
					{
						Debug.Assert(false, "Unexpected!");
						return TypeCode.Empty;
					}
			}
		}

		// Private Function EvaluateCCCast(ByVal expression As IntrinsicCastExpression) As Object
		// ' This cast is safe because only intrinsics are ever returned
		// Dim Operand As IConvertible = CType(EvaluateCCExpression(expression.Operand), IConvertible)
		// Dim OperandType As TypeCode
		// Dim CastType As TypeCode = TypeCodeOfCastExpression(expression.IntrinsicType)

		// If CastType = TypeCode.Empty Then
		// Return Nothing
		// End If

		// If Operand Is Nothing Then
		// Operand = 0
		// End If

		// OperandType = Operand.GetTypeCode()

		// If CastType = OperandType OrElse CastType = TypeCode.Object Then
		// Return Operand
		// End If

		// Select Case OperandType
		// Case TypeCode.Boolean
		// If CastType = TypeCode.Byte Then
		// Operand = 255
		// Else
		// Operand = -1
		// End If
		// OperandType = TypeCode.Int32

		// Case TypeCode.String
		// If CastType <> TypeCode.Char Then
		// ReportSyntaxError(SyntaxErrorType.CantCastStringInCCExpression, expression.Span)
		// Return Nothing
		// End If

		// Case TypeCode.Char
		// If CastType <> TypeCode.String Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
		// Return Nothing
		// End If

		// Case TypeCode.DateTime
		// ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
		// Return Nothing
		// End Select

		// Select Case expression.IntrinsicType
		// Case IntrinsicType.Boolean
		// Return CBool(Operand)

		// Case IntrinsicType.Byte
		// Return CByte(Operand)

		// Case IntrinsicType.Short
		// Return CShort(Operand)

		// Case IntrinsicType.Integer
		// Return CInt(Operand)

		// Case IntrinsicType.Long
		// Return CLng(Operand)

		// Case IntrinsicType.Decimal
		// Return CDec(Operand)

		// Case IntrinsicType.Single
		// Return CSng(Operand)

		// Case IntrinsicType.Double
		// Return CDbl(Operand)

		// Case IntrinsicType.Char
		// If OperandType = TypeCode.String Then
		// Return CChar(DirectCast(Operand, String))
		// End If

		// ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
		// Return Nothing

		// Case IntrinsicType.String
		// If OperandType = TypeCode.Char Then
		// Return CStr(DirectCast(Operand, Char))
		// End If

		// ReportSyntaxError(SyntaxErrorType.CantCastStringInCCExpression, expression.Span)
		// Return Nothing

		// Case IntrinsicType.Date
		// ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
		// Return Nothing

		// Case Else
		// Debug.Assert(False, "Unexpected!")
		// Return Nothing
		// End Select
		// End Function

		// Private Function EvaluateCCUnaryOperator(ByVal expression As UnaryOperatorExpression) As Object
		// ' This cast is safe because only intrinsics are ever returned
		// Dim Operand As IConvertible = CType(EvaluateCCExpression(expression.Operand), IConvertible)
		// Dim OperandType As TypeCode

		// If Operand Is Nothing Then
		// Operand = 0
		// End If

		// OperandType = Operand.GetTypeCode()

		// If OperandType = TypeCode.String OrElse OperandType = TypeCode.Char OrElse OperandType = TypeCode.DateTime Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// End If

		// Select Case expression.[Operator]
		// Case OperatorType.UnaryPlus
		// If OperandType = TypeCode.Boolean Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// Else
		// Return Operand
		// End If

		// Case OperatorType.Negate
		// If OperandType = TypeCode.Boolean OrElse OperandType = TypeCode.Byte Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// Else
		// Return CompilerServices.ObjectType.NegObj(Operand)
		// End If

		// Case OperatorType.Not
		// If OperandType = TypeCode.Decimal OrElse OperandType = TypeCode.Single OrElse _
		// OperandType = TypeCode.Double Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// Else
		// Return CompilerServices.ObjectType.NotObj(Operand)
		// End If

		// Case Else
		// Debug.Assert(False, "Unexpected!")
		// Return Nothing
		// End Select
		// End Function

		private static bool EitherIsTypeCode(TypeCode x, TypeCode y, TypeCode @type)
		{
			return x == type || y == type;
		}

		private static bool IsEitherTypeCode(TypeCode x, TypeCode type1, TypeCode type2)
		{
			return x == type1 || x == type2;
		}

		// Private Function EvaluateCCBinaryOperator(ByVal expression As BinaryOperatorExpression) As Object
		// ' This cast is safe because only intrinsics are ever returned
		// Dim LeftOperand As IConvertible = CType(EvaluateCCExpression(expression.LeftOperand), IConvertible)
		// Dim RightOperand As IConvertible = CType(EvaluateCCExpression(expression.RightOperand), IConvertible)
		// Dim LeftOperandType, RightOperandType As TypeCode

		// If LeftOperand Is Nothing Then
		// LeftOperand = 0
		// End If

		// If RightOperand Is Nothing Then
		// RightOperand = 0
		// End If

		// LeftOperandType = LeftOperand.GetTypeCode()
		// RightOperandType = RightOperand.GetTypeCode()

		// If EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.DateTime) Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// End If

		// If expression.[Operator] <> OperatorType.Concatenate AndAlso _
		// expression.[Operator] <> OperatorType.Plus AndAlso _
		// expression.[Operator] <> OperatorType.Equals AndAlso _
		// expression.[Operator] <> OperatorType.NotEquals AndAlso _
		// (EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char) OrElse _
		// EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String)) Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// End If

		// Select Case expression.[Operator]
		// Case OperatorType.Plus
		// If EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String) OrElse _
		// EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char) Then
		// If Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
		// Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// Else
		// Return CStr(LeftOperand) + CStr(RightOperand)
		// End If
		// Else
		// Return CompilerServices.ObjectType.AddObj(LeftOperand, RightOperand)
		// End If

		// Case OperatorType.Minus
		// Return CompilerServices.ObjectType.SubObj(LeftOperand, RightOperand)

		// Case OperatorType.Multiply
		// Return CompilerServices.ObjectType.MulObj(LeftOperand, RightOperand)

		// Case OperatorType.IntegralDivide
		// Return CompilerServices.ObjectType.IDivObj(LeftOperand, RightOperand)

		// Case OperatorType.Divide
		// Return CompilerServices.ObjectType.DivObj(LeftOperand, RightOperand)

		// Case OperatorType.Modulus
		// Return CompilerServices.ObjectType.ModObj(LeftOperand, RightOperand)

		// Case OperatorType.Power
		// Return CompilerServices.ObjectType.PowObj(LeftOperand, RightOperand)

		// Case OperatorType.ShiftLeft
		// Return CompilerServices.ObjectType.ShiftLeftObj(LeftOperand, CInt(RightOperand))

		// Case OperatorType.ShiftRight
		// Return CompilerServices.ObjectType.ShiftRightObj(LeftOperand, CInt(RightOperand))

		// Case OperatorType.And
		// Return CompilerServices.ObjectType.BitAndObj(LeftOperand, CInt(RightOperand))

		// Case OperatorType.Or
		// Return CompilerServices.ObjectType.BitOrObj(LeftOperand, CInt(RightOperand))

		// Case OperatorType.Xor
		// Return CompilerServices.ObjectType.BitXorObj(LeftOperand, CInt(RightOperand))

		// Case OperatorType.AndAlso
		// Return CBool(LeftOperand) AndAlso CBool(RightOperand)

		// Case OperatorType.OrElse
		// Return CBool(LeftOperand) OrElse CBool(RightOperand)

		// Case OperatorType.Equals
		// If (EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String) OrElse _
		// EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char)) AndAlso _
		// (Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
		// Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String)) Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// End If

		// Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) = 0

		// Case OperatorType.NotEquals
		// If (EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String) OrElse _
		// EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char)) AndAlso _
		// (Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
		// Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String)) Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// End If

		// Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) <> 0

		// Case OperatorType.LessThan
		// Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) = -1

		// Case OperatorType.GreaterThan
		// Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) = 1

		// Case OperatorType.LessThanEquals
		// Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) <> 1

		// Case OperatorType.GreaterThanEquals
		// Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) <> -1

		// Case OperatorType.Concatenate
		// If Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
		// Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) Then
		// ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
		// Return Nothing
		// Else
		// Return CStr(LeftOperand) & CStr(RightOperand)
		// End If

		// Case Else
		// Debug.Assert(False, "Unexpected!")
		// Return Nothing
		// End Select
		// End Function

		// Private Function EvaluateCCExpression(ByVal expression As Expression) As Object
		// Select Case expression.Type
		// Case TreeType.SyntaxError
		// ' Do nothing

		// Case TreeType.NothingExpression
		// Return Nothing

		// Case TreeType.IntegerLiteralExpression, TreeType.FloatingPointLiteralExpression, _
		// TreeType.StringLiteralExpression, TreeType.CharacterLiteralExpression, _
		// TreeType.DateLiteralExpression, TreeType.DecimalLiteralExpression, _
		// TreeType.BooleanLiteralExpression
		// Return EvaluateCCLiteral(CType(expression, LiteralExpression))

		// Case TreeType.ParentheticalExpression
		// Return EvaluateCCExpression(CType(expression, ParentheticalExpression).Operand)

		// Case TreeType.SimpleNameExpression
		// If ConditionalCompilationConstants.ContainsKey(CType(expression, SimpleNameExpression).Name.Name) Then
		// Return ConditionalCompilationConstants(CType(expression, SimpleNameExpression).Name.Name)
		// Else
		// Return Nothing
		// End If

		// Case TreeType.IntrinsicCastExpression
		// Return EvaluateCCCast(CType(expression, IntrinsicCastExpression))

		// Case TreeType.UnaryOperatorExpression
		// Return EvaluateCCUnaryOperator(CType(expression, UnaryOperatorExpression))

		// Case TreeType.BinaryOperatorExpression
		// Return EvaluateCCBinaryOperator(CType(expression, BinaryOperatorExpression))

		// Case Else
		// ReportSyntaxError(SyntaxErrorType.CCExpressionRequired, expression.Span)
		// End Select

		// Return Nothing
		// End Function

		// Private Sub ParseConditionalConstantStatement()
		// Dim Identifier As IdentifierToken
		// Dim Expression As Expression

		// ' Consume the Const keyword
		// Read()

		// If Peek().Type = TokenType.Identifier Then
		// Identifier = CType(Read(), IdentifierToken)
		// Else
		// ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Peek())
		// ResyncAt()
		// Return
		// End If

		// VerifyExpectedToken(TokenType.Equals)
		// Expression = ParseExpression()

		// If Not ErrorInConstruct Then
		// ConditionalCompilationConstants.Add(Identifier.Identifier, EvaluateCCExpression(Expression))
		// Else
		// ResyncAt()
		// End If
		// End Sub

		// Private Sub ParseConditionalIfStatement()
		// Dim Expression As Expression
		// Dim CCContext As ConditionalCompilationContext

		// ' Consume the If
		// Read()

		// Expression = ParseExpression()

		// If ErrorInConstruct Then
		// ResyncAt(TokenType.Then)
		// End If

		// If Peek().Type = TokenType.Then Then
		// ' Consume the Then keyword
		// Read()
		// End If

		// CCContext = New ConditionalCompilationContext
		// With CCContext
		// .BlockActive = CBool(EvaluateCCExpression(Expression))
		// .AnyBlocksActive = .BlockActive
		// End With
		// ConditionalCompilationContextStack.Push(CCContext)
		// End Sub

		// Private Sub ParseConditionalElseIfStatement(ByVal start As Token)
		// Dim Expression As Expression
		// Dim CCContext As ConditionalCompilationContext

		// ' Consume the If
		// Read()

		// Expression = ParseExpression()

		// If ErrorInConstruct Then
		// ResyncAt(TokenType.Then)
		// End If

		// If Peek().Type = TokenType.Then Then
		// ' Consume the Then keyword
		// Read()
		// End If

		// If ConditionalCompilationContextStack.Count = 0 Then
		// ReportSyntaxError(SyntaxErrorType.CCElseIfWithoutCCIf, SpanFrom(start))
		// Else
		// CCContext = ConditionalCompilationContextStack.Peek()

		// If CCContext.SeenElse Then
		// ReportSyntaxError(SyntaxErrorType.CCElseIfAfterCCElse, SpanFrom(start))
		// CCContext.BlockActive = False
		// ElseIf CCContext.BlockActive Then
		// CCContext.BlockActive = False
		// ElseIf Not CCContext.AnyBlocksActive AndAlso CBool(EvaluateCCExpression(Expression)) Then
		// CCContext.BlockActive = True
		// CCContext.AnyBlocksActive = True
		// End If
		// End If
		// End Sub

		// Private Sub ParseConditionalElseStatement(ByVal start As Token)
		// Dim CCContext As ConditionalCompilationContext

		// ' Consume the else
		// Read()

		// If ConditionalCompilationContextStack.Count = 0 Then
		// ReportSyntaxError(SyntaxErrorType.CCElseWithoutCCIf, SpanFrom(start))
		// Else
		// CCContext = ConditionalCompilationContextStack.Peek()

		// If CCContext.SeenElse Then
		// ReportSyntaxError(SyntaxErrorType.CCElseAfterCCElse, SpanFrom(start))
		// CCContext.BlockActive = False
		// Else
		// CCContext.SeenElse = True

		// If CCContext.BlockActive Then
		// CCContext.BlockActive = False
		// ElseIf Not CCContext.AnyBlocksActive Then
		// CCContext.BlockActive = True
		// End If
		// End If
		// End If
		// End Sub

		// Private Function ParsePreprocessorStatement(ByVal statementLevel As Boolean) As Boolean
		// Dim Start As Token = Peek()

		// Debug.Assert(AtBeginningOfLine, "Must be at beginning of line!")

		// If Not Preprocess Then
		// Return False
		// End If

		// If Start.Type = TokenType.Pound Then
		// ErrorInConstruct = False

		// ' Consume the pound
		// Read()

		// Select Case Peek().AsUnreservedKeyword()
		// Case TokenType.Const
		// ParseConditionalConstantStatement()

		// Case TokenType.If
		// ParseConditionalIfStatement()

		// Case TokenType.Else
		// ParseConditionalElseStatement(Start)

		// Case TokenType.ElseIf
		// ParseConditionalElseIfStatement(Start)

		// Case TokenType.ExternalSource
		// ParseExternalSourceStatement(Start)

		// Case TokenType.Region
		// ParseRegionStatement(Start, statementLevel)

		// Case TokenType.ExternalChecksum
		// ParseExternalChecksumStatement()

		// Case TokenType.End
		// ParseEndPreprocessingStatement(Start, statementLevel)

		// Case Else
		// InvalidStatement:
		// ResyncAt()
		// ReportSyntaxError(SyntaxErrorType.InvalidPreprocessorStatement, SpanFrom(Start))
		// End Select

		// ParseTrailingComments()

		// If Peek().Type <> TokenType.LineTerminator AndAlso Peek().Type <> TokenType.EndOfStream Then
		// ReportSyntaxError(SyntaxErrorType.ExpectedEndOfStatement, Peek())
		// ResyncAt()
		// End If

		// Read()
		// Return True
		// Else
		// ' If we're in a false conditional compilation statement, then keep reading lines as if they
		// ' were preprocessing statements until we are done.
		// If Start.Type <> TokenType.EndOfStream AndAlso _
		// ConditionalCompilationContextStack.Count > 0 AndAlso _
		// Not ConditionalCompilationContextStack.Peek().BlockActive Then
		// ResyncAt()
		// Read()

		// Return True
		// Else
		// Return False
		// End If
		// End If
		// End Function

		// *
		// * Public APIs
		// *

		private void StartParsing(Scanner scanner, IList<SyntaxError> errorTable, bool preprocess = false, IDictionary<string, object> conditionalCompilationConstants = null, IList<SourceRegion> sourceRegions = null, IList<ExternalLineMapping> externalLineMappings = null, IList<ExternalChecksum> externalChecksums = null)





		{
			Scanner = scanner;
			ErrorTable = errorTable;
			Preprocess = preprocess;
			if (conditionalCompilationConstants is null)
			{
				ConditionalCompilationConstants = new Dictionary<string, object>();
			}
			else
			{
				// We have to clone this because the same hashtable could be used for
				// multiple parses.
				ConditionalCompilationConstants = new Dictionary<string, object>(conditionalCompilationConstants);
			}
			ExternalLineMappings = externalLineMappings;
			SourceRegions = sourceRegions;
			ExternalChecksums = externalChecksums;
			ErrorInConstruct = false;
			AtBeginningOfLine = true;
			BlockContextStack.Clear();
		}

		private void FinishParsing()
		{
			if (CurrentExternalSourceContext is not null)
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedEndExternalSource, Peek());
			}

			if (!(RegionContextStack.Count == 0))
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedEndRegion, Peek());
			}

			if (!(ConditionalCompilationContextStack.Count == 0))
			{
				ReportSyntaxError(SyntaxErrorType.ExpectedCCEndIf, Peek());
			}

			StartParsing(null, null, false, null, null, null);
		}

		/// <summary>
    /// Parse an entire file.
    /// </summary>
    /// <param name="scanner">The scanner to use to fetch the tokens.</param>
    /// <param name="errorTable">The list of errors produced during parsing.</param>
    /// <returns>A file-level parse tree.</returns>
		public File ParseFile(Scanner scanner, IList<SyntaxError> errorTable)
		{
			File File;

			StartParsing(scanner, errorTable, true);
			File = ParseFile();
			FinishParsing();

			return File;
		}

		/// <summary>
    /// Parse an entire file.
    /// </summary>
    /// <param name="scanner">The scanner to use to fetch the tokens.</param>
    /// <param name="errorTable">The list of errors produced during parsing.</param>
    /// <param name="conditionalCompilationConstants">Pre-defined conditional compilation constants.</param>
    /// <param name="sourceRegions">Source regions defined in the file.</param>
    /// <param name="externalLineMappings">External line mappings defined in the file.</param>
    /// <returns>A file-level parse tree.</returns>
		public File ParseFile(Scanner scanner, IList<SyntaxError> errorTable, IDictionary<string, object> conditionalCompilationConstants, IList<SourceRegion> sourceRegions, IList<ExternalLineMapping> externalLineMappings, IList<ExternalChecksum> externalChecksums)




		{
			File File;

			StartParsing(scanner, errorTable, true, conditionalCompilationConstants, sourceRegions, externalLineMappings, externalChecksums);
			File = ParseFile();
			FinishParsing();

			return File;
		}

		// LC the entry method to parse script file
		public ScriptBlock ParseScriptFile(Scanner scanner, IList<SyntaxError> errorTable)
		{
			ScriptBlock File;

			StartParsing(scanner, errorTable, true);
			File = ParseScriptFile();
			FinishParsing();

			return File;
		}
		// LC the entry method to parse script file
		public ScriptBlock ParseScriptFile(Scanner scanner, IList<SyntaxError> errorTable, IDictionary<string, object> conditionalCompilationConstants, IList<SourceRegion> sourceRegions, IList<ExternalLineMapping> externalLineMappings, IList<ExternalChecksum> externalChecksums)




		{
			ScriptBlock File;

			StartParsing(scanner, errorTable, true, conditionalCompilationConstants, sourceRegions, externalLineMappings, externalChecksums);
			File = ParseScriptFile();
			FinishParsing();

			return File;
		}


		/// <summary>
    /// Parse a declaration.
    /// </summary>
    /// <param name="scanner">The scanner to use to fetch the tokens.</param>
    /// <param name="errorTable">The list of errors produced during parsing.</param>
    /// <returns>A declaration-level parse tree.</returns>
		public Declaration ParseDeclaration(Scanner scanner, IList<SyntaxError> errorTable)
		{
			Declaration Declaration;

			StartParsing(scanner, errorTable);
			Token argterminator = null;
			Declaration = ParseDeclaration(terminator: ref argterminator);
			FinishParsing();

			return Declaration;
		}

		/// <summary>
    /// Parse a statement.
    /// </summary>
    /// <param name="scanner">The scanner to use to fetch the tokens.</param>
    /// <param name="errorTable">The list of errors produced during parsing.</param>
    /// <returns>A statement-level parse tree.</returns>
		public Statement ParseStatement(Scanner scanner, IList<SyntaxError> errorTable)
		{
			Statement Statement;

			StartParsing(scanner, errorTable);
			Token argterminator = null;
			Statement = ParseStatement(terminator: ref argterminator);
			FinishParsing();

			return Statement;
		}

		/// <summary>
    /// Parse an expression.
    /// </summary>
    /// <param name="scanner">The scanner to use to fetch the tokens.</param>
    /// <param name="errorTable">The list of errors produced during parsing.</param>
    /// <returns>An expression-level parse tree.</returns>
		public Expression ParseExpression(Scanner scanner, IList<SyntaxError> errorTable)
		{
			Expression Expression;

			StartParsing(scanner, errorTable);
			Expression = ParseExpression();
			FinishParsing();

			return Expression;
		}

		/// <summary>
    /// Parse a type name.
    /// </summary>
    /// <param name="scanner">The scanner to use to fetch the tokens.</param>
    /// <param name="errorTable">The list of errors produced during parsing.</param>
    /// <returns>A typename-level parse tree.</returns>
		public TypeName ParseTypeName(Scanner scanner, IList<SyntaxError> errorTable)
		{
			TypeName TypeName;

			StartParsing(scanner, errorTable);
			TypeName = ParseTypeName(true);
			FinishParsing();

			return TypeName;
		}
	}
}