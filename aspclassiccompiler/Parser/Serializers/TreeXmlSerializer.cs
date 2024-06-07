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

using System.Xml;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Dlrsoft.VBScript.Parser
{

	public class TreeXmlSerializer
	{
		private readonly XmlWriter Writer;

		public TreeXmlSerializer(XmlWriter Writer)
		{
			this.Writer = Writer;
		}

		private static TokenType GetOperatorToken(OperatorType @type)
		{
			switch (type)
			{
				case OperatorType.Concatenate:
					{
						return TokenType.Ampersand;
					}

				case OperatorType.Multiply:
					{
						return TokenType.Star;
					}

				case OperatorType.Divide:
					{
						return TokenType.ForwardSlash;
					}

				case OperatorType.IntegralDivide:
					{
						return TokenType.BackwardSlash;
					}

				case OperatorType.Power:
					{
						return TokenType.Caret;
					}

				case OperatorType.Plus:
				case OperatorType.UnaryPlus:
					{
						return TokenType.Plus;
					}

				case OperatorType.Minus:
				case OperatorType.Negate:
					{
						return TokenType.Minus;
					}

				case OperatorType.LessThan:
					{
						return TokenType.LessThan;
					}

				case OperatorType.LessThanEquals:
					{
						return TokenType.LessThanEquals;
					}

				case OperatorType.Equals:
					{
						return TokenType.Equals;
					}

				case OperatorType.NotEquals:
					{
						return TokenType.NotEquals;
					}

				case OperatorType.GreaterThan:
					{
						return TokenType.GreaterThan;
					}

				case OperatorType.GreaterThanEquals:
					{
						return TokenType.GreaterThanEquals;
					}

				case OperatorType.ShiftLeft:
					{
						return TokenType.LessThanLessThan;
					}

				case OperatorType.ShiftRight:
					{
						return TokenType.GreaterThanGreaterThan;
					}

				case OperatorType.Modulus:
					{
						return TokenType.Mod;
					}

				case OperatorType.Or:
					{
						return TokenType.Or;
					}

				case OperatorType.OrElse:
					{
						return TokenType.OrElse;
					}

				case OperatorType.And:
					{
						return TokenType.And;
					}

				case OperatorType.AndAlso:
					{
						return TokenType.AndAlso;
					}

				case OperatorType.Xor:
					{
						return TokenType.Xor;
					}

				case OperatorType.Like:
					{
						return TokenType.Like;
					}

				case OperatorType.Is:
					{
						return TokenType.Is;
					}

				case OperatorType.IsNot:
					{
						return TokenType.IsNot;
					}

				case OperatorType.Not:
					{
						return TokenType.Not;
					}

				case OperatorType.To:
					{
						return TokenType.To;
					}

				default:
					{
						return TokenType.LexicalError;
					}
			}
		}

		private static TokenType GetCompoundAssignmentOperatorToken(OperatorType compoundOperator)
		{
			switch (compoundOperator)
			{
				case OperatorType.Plus:
					{
						return TokenType.PlusEquals;
					}

				case OperatorType.Concatenate:
					{
						return TokenType.AmpersandEquals;
					}

				case OperatorType.Multiply:
					{
						return TokenType.StarEquals;
					}

				case OperatorType.Minus:
					{
						return TokenType.MinusEquals;
					}

				case OperatorType.Divide:
					{
						return TokenType.ForwardSlashEquals;
					}

				case OperatorType.IntegralDivide:
					{
						return TokenType.BackwardSlashEquals;
					}

				case OperatorType.Power:
					{
						return TokenType.CaretEquals;
					}

				case OperatorType.ShiftLeft:
					{
						return TokenType.LessThanLessThanEquals;
					}

				case OperatorType.ShiftRight:
					{
						return TokenType.GreaterThanGreaterThanEquals;
					}

				default:
					{
						Debug.Assert(false, "Unexpected!");
						throw new NotSupportedException("Unknown OperatorType");
					}
			}
		}

		private static TokenType GetBlockTypeToken(BlockType blockType)
		{
			switch (blockType)
			{
				case BlockType.Class:
					{
						return TokenType.Class;
					}

				case BlockType.Enum:
					{
						return TokenType.Enum;
					}

				case BlockType.Function:
					{
						return TokenType.Function;
					}

				case BlockType.Get:
					{
						return TokenType.Get;
					}

				case BlockType.Event:
					{
						return TokenType.Event;
					}

				case BlockType.AddHandler:
					{
						return TokenType.AddHandler;
					}

				case BlockType.RemoveHandler:
					{
						return TokenType.RemoveHandler;
					}

				case BlockType.RaiseEvent:
					{
						return TokenType.RaiseEvent;
					}

				case BlockType.If:
					{
						return TokenType.If;
					}

				case BlockType.Interface:
					{
						return TokenType.Interface;
					}

				case BlockType.Module:
					{
						return TokenType.Module;
					}

				case BlockType.Namespace:
					{
						return TokenType.Namespace;
					}

				case BlockType.Property:
					{
						return TokenType.Property;
					}

				case BlockType.Select:
					{
						return TokenType.Select;
					}

				case BlockType.Set:
					{
						return TokenType.Set;
					}

				case BlockType.Structure:
					{
						return TokenType.Structure;
					}

				case BlockType.Sub:
					{
						return TokenType.Sub;
					}

				case BlockType.SyncLock:
					{
						return TokenType.SyncLock;
					}

				case BlockType.Using:
					{
						return TokenType.Using;
					}

				case BlockType.Try:
					{
						return TokenType.Try;
					}

				case BlockType.While:
					{
						return TokenType.While;
					}

				case BlockType.With:
					{
						return TokenType.With;
					}

				case BlockType.None:
					{
						return TokenType.LexicalError;
					}

				case BlockType.Do:
					{
						return TokenType.Do;
					}

				case BlockType.For:
					{
						return TokenType.For;
					}

				case BlockType.Operator:
					{
						return TokenType.Operator;
					}

				default:
					{
						Debug.Assert(false, "Unexpected!");
						throw new NotSupportedException("Unknown block type");
					}
			}
		}

		private void SerializeSpan(Span Span)
		{
			Writer.WriteAttributeString("startLine", Span.Start.Line.ToString());
			Writer.WriteAttributeString("startCol", Span.Start.Column.ToString());
			Writer.WriteAttributeString("endLine", Span.Finish.Line.ToString());
			Writer.WriteAttributeString("endCol", Span.Finish.Column.ToString());
		}

		private void SerializeLocation(Location Location)
		{
			Writer.WriteAttributeString("line", Location.Line.ToString());
			Writer.WriteAttributeString("col", Location.Column.ToString());
		}

		protected void SerializeToken(TokenType TokenType, Location Location)
		{
			if (Location.IsValid)
			{
				Writer.WriteStartElement(TokenType.ToString());
				SerializeLocation(Location);
				Writer.WriteEndElement();
			}
		}

		private Dictionary<TypeCharacter, string> _SerializeTypeCharacter_TypeCharacterTable = default;

		private void SerializeTypeCharacter(TypeCharacter TypeCharacter)
		{
			if (TypeCharacter != TypeCharacter.None)
			{

				if (_SerializeTypeCharacter_TypeCharacterTable is null)
				{
					var Table = new Dictionary<TypeCharacter, string>();
					// NOTE: These have to be in the same order as the enum!
					string[] TypeCharacters = new string[] { "$", "%", "&", "S", "I", "L", "!", "#", "@", "F", "R", "D", "US", "UI", "UL" };
					var TableTypeCharacter = TypeCharacter.StringSymbol;

					for (int Index = 0, loopTo = TypeCharacters.Length - 1; Index <= loopTo; Index++)
					{
						Table.Add(TableTypeCharacter, TypeCharacters[Index]);
						TableTypeCharacter = (TypeCharacter)((int)TableTypeCharacter << 1);
					}

					_SerializeTypeCharacter_TypeCharacterTable = Table;
				}

				Writer.WriteAttributeString("typeChar", _SerializeTypeCharacter_TypeCharacterTable[TypeCharacter]);
			}
		}

		private void SerializeColonDelimitedList<T>(ColonDelimitedTreeCollection<T> List) where T : Tree
		{
			IEnumerator<Location> ColonEnumerator;
			bool MoreColons;

			if (List.ColonLocations is not null)
			{
				ColonEnumerator = List.ColonLocations.GetEnumerator();
				MoreColons = ColonEnumerator.MoveNext();
			}
			else
			{
				ColonEnumerator = null;
				MoreColons = false;
			}

			foreach (Tree Child in List.Children)
			{
				while (MoreColons && ColonEnumerator.Current <= Child.Span.Start)
				{
					SerializeToken(TokenType.Colon, ColonEnumerator.Current);
					MoreColons = ColonEnumerator.MoveNext();
				}

				Serialize(Child);
			}

			while (MoreColons)
			{
				SerializeToken(TokenType.Colon, ColonEnumerator.Current);
				MoreColons = ColonEnumerator.MoveNext();
			}
		}

		private void SerializeCommaDelimitedList<T>(CommaDelimitedTreeCollection<T> List) where T : Tree
		{
			IEnumerator<Location> CommaEnumerator;
			bool MoreCommas;

			if (List.CommaLocations is not null)
			{
				CommaEnumerator = List.CommaLocations.GetEnumerator();
				MoreCommas = CommaEnumerator.MoveNext();
			}
			else
			{
				CommaEnumerator = null;
				MoreCommas = false;
			}

			foreach (Tree Child in List.Children)
			{
				if (Child is not null)
				{
					while (MoreCommas && CommaEnumerator.Current <= Child.Span.Start)
					{
						SerializeToken(TokenType.Comma, CommaEnumerator.Current);
						MoreCommas = CommaEnumerator.MoveNext();
					}

					Serialize(Child);
				}
			}

			while (MoreCommas)
			{
				SerializeToken(TokenType.Comma, CommaEnumerator.Current);
				MoreCommas = CommaEnumerator.MoveNext();
			}
		}

		private void SerializeList(Tree List)
		{
			switch (List.Type)
			{
				case TreeType.ArgumentCollection:
					{
						SerializeCommaDelimitedList((ArgumentCollection)List);
						{
							var withBlock = (ArgumentCollection)List;
							if (withBlock.RightParenthesisLocation.IsValid)
							{
								SerializeToken(TokenType.RightParenthesis, withBlock.RightParenthesisLocation);
							}
						}

						break;
					}

				case TreeType.AttributeCollection:
					{
						SerializeCommaDelimitedList((AttributeCollection)List);
						{
							var withBlock1 = (AttributeCollection)List;
							if (withBlock1.RightBracketLocation.IsValid)
							{
								SerializeToken(TokenType.GreaterThan, withBlock1.RightBracketLocation);
							}
						}

						break;
					}

				case TreeType.CaseClauseCollection:
					{
						SerializeCommaDelimitedList((CaseClauseCollection)List);
						break;
					}

				case TreeType.ExpressionCollection:
					{
						SerializeCommaDelimitedList((ExpressionCollection)List);
						break;
					}

				case TreeType.ImportCollection:
					{
						SerializeCommaDelimitedList((ImportCollection)List);
						break;
					}

				case TreeType.InitializerCollection:
					{
						SerializeCommaDelimitedList((InitializerCollection)List);
						{
							var withBlock2 = (InitializerCollection)List;
							if (withBlock2.RightCurlyBraceLocation.IsValid)
							{
								SerializeToken(TokenType.RightCurlyBrace, withBlock2.RightCurlyBraceLocation);
							}
						}

						break;
					}

				case TreeType.NameCollection:
					{
						SerializeCommaDelimitedList((NameCollection)List);
						break;
					}

				case TreeType.VariableNameCollection:
					{
						SerializeCommaDelimitedList((VariableNameCollection)List);
						break;
					}

				case TreeType.ParameterCollection:
					{
						SerializeCommaDelimitedList((ParameterCollection)List);
						{
							var withBlock3 = (ParameterCollection)List;
							if (withBlock3.RightParenthesisLocation.IsValid)
							{
								SerializeToken(TokenType.RightParenthesis, withBlock3.RightParenthesisLocation);
							}
						}

						break;
					}

				case TreeType.TypeNameCollection:
					{
						SerializeCommaDelimitedList((TypeNameCollection)List);
						break;
					}

				case TreeType.VariableDeclaratorCollection:
					{
						SerializeCommaDelimitedList((VariableDeclaratorCollection)List);
						break;
					}

				case TreeType.DeclarationCollection:
					{
						SerializeColonDelimitedList((DeclarationCollection)List);
						break;
					}

				case TreeType.StatementCollection:
					{
						SerializeColonDelimitedList((StatementCollection)List);
						break;
					}

				case TreeType.TypeParameterCollection:
					{
						{
							var withBlock4 = (TypeParameterCollection)List;
							if (withBlock4.OfLocation.IsValid)
							{
								SerializeToken(TokenType.Of, withBlock4.OfLocation);
							}
							SerializeCommaDelimitedList((TypeParameterCollection)List);
							if (withBlock4.RightParenthesisLocation.IsValid)
							{
								SerializeToken(TokenType.RightParenthesis, withBlock4.RightParenthesisLocation);
							}
						}

						break;
					}

				case TreeType.TypeConstraintCollection:
					{
						SerializeCommaDelimitedList((TypeConstraintCollection)List);
						{
							var withBlock5 = (TypeConstraintCollection)List;
							if (withBlock5.RightBracketLocation.IsValid)
							{
								SerializeToken(TokenType.RightCurlyBrace, withBlock5.RightBracketLocation);
							}
						}

						break;
					}

				case TreeType.TypeArgumentCollection:
					{
						{
							var withBlock6 = (TypeArgumentCollection)List;
							if (withBlock6.OfLocation.IsValid)
							{
								SerializeToken(TokenType.Of, withBlock6.OfLocation);
							}
							SerializeCommaDelimitedList((TypeArgumentCollection)List);
							if (withBlock6.RightParenthesisLocation.IsValid)
							{
								SerializeToken(TokenType.RightParenthesis, withBlock6.RightParenthesisLocation);
							}
						}

						break;
					}

				default:
					{
						foreach (Tree Child in List.Children)
							Serialize(Child);
						break;
					}
			}
		}

		private void SerializeName(Tree Name)
		{
			switch (Name.Type)
			{
				case TreeType.SimpleName:
					{
						{
							var withBlock = (SimpleName)Name;
							SerializeTypeCharacter(withBlock.TypeCharacter);
							Writer.WriteAttributeString("escaped", Conversions.ToString(withBlock.Escaped));
							Writer.WriteString(withBlock.Name);
						}

						break;
					}

				case TreeType.VariableName:
					{
						{
							var withBlock1 = (VariableName)Name;
							Serialize(withBlock1.Name);
							Serialize(withBlock1.ArrayType);
						}

						break;
					}

				case TreeType.QualifiedName:
					{
						{
							var withBlock2 = (QualifiedName)Name;
							Serialize(withBlock2.Qualifier);
							SerializeToken(TokenType.Period, withBlock2.DotLocation);
							Serialize(withBlock2.Name);
						}

						break;
					}
			}
		}

		private void SerializeType(Tree Type)
		{
			switch (Type.Type)
			{
				case TreeType.IntrinsicType:
					{
						Writer.WriteAttributeString("intrinsicType", ((IntrinsicTypeName)Type).IntrinsicType.ToString());
						break;
					}

				case TreeType.NamedType:
					{
						{
							var withBlock = (NamedTypeName)Type;
							Serialize(withBlock.Name);
						}

						break;
					}

				case TreeType.ArrayType:
					{
						{
							var withBlock1 = (ArrayTypeName)Type;
							Writer.WriteAttributeString("rank", withBlock1.Rank.ToString());
							Serialize(withBlock1.ElementTypeName);
							Serialize(withBlock1.Arguments);
						}

						break;
					}

				case TreeType.ConstructedType:
					{
						{
							var withBlock2 = (ConstructedTypeName)Type;
							Serialize(withBlock2.Name);
							Serialize(withBlock2.TypeArguments);
						}

						break;
					}
			}
		}

		private void SerializeInitializer(Tree Initializer)
		{
			switch (Initializer.Type)
			{
				case TreeType.AggregateInitializer:
					{
						{
							var withBlock = (AggregateInitializer)Initializer;
							Serialize(withBlock.Elements);
						}

						break;
					}

				case TreeType.ExpressionInitializer:
					{
						{
							var withBlock1 = (ExpressionInitializer)Initializer;
							Serialize(withBlock1.Expression);
						}

						break;
					}
			}
		}

		private void SerializeExpression(Tree Expression)
		{
			Writer.WriteAttributeString("isConstant", Conversions.ToString(((Expression)Expression).IsConstant));

			switch (Expression.Type)
			{
				case TreeType.StringLiteralExpression:
					{
						{
							var withBlock = (StringLiteralExpression)Expression;
							Writer.WriteString(withBlock.Literal);
						}

						break;
					}

				case TreeType.CharacterLiteralExpression:
					{
						{
							var withBlock1 = (CharacterLiteralExpression)Expression;
							Writer.WriteString(Conversions.ToString(withBlock1.Literal));
						}

						break;
					}

				case TreeType.DateLiteralExpression:
					{
						{
							var withBlock2 = (DateLiteralExpression)Expression;
							Writer.WriteString(Conversions.ToString(withBlock2.Literal));
						}

						break;
					}

				case TreeType.IntegerLiteralExpression:
					{
						{
							var withBlock3 = (IntegerLiteralExpression)Expression;
							SerializeTypeCharacter(withBlock3.TypeCharacter);
							Writer.WriteAttributeString("base", withBlock3.IntegerBase.ToString());
							Writer.WriteString(withBlock3.Literal.ToString());
						}

						break;
					}

				case TreeType.FloatingPointLiteralExpression:
					{
						{
							var withBlock4 = (FloatingPointLiteralExpression)Expression;
							SerializeTypeCharacter(withBlock4.TypeCharacter);
							Writer.WriteString(withBlock4.Literal.ToString());
						}

						break;
					}

				case TreeType.DecimalLiteralExpression:
					{
						{
							var withBlock5 = (DecimalLiteralExpression)Expression;
							SerializeTypeCharacter(withBlock5.TypeCharacter);
							Writer.WriteString(withBlock5.Literal.ToString());
						}

						break;
					}

				case TreeType.BooleanLiteralExpression:
					{
						{
							var withBlock6 = (BooleanLiteralExpression)Expression;
							Writer.WriteString(Conversions.ToString(withBlock6.Literal));
						}

						break;
					}

				case TreeType.GetTypeExpression:
					{
						{
							var withBlock7 = (GetTypeExpression)Expression;
							SerializeToken(TokenType.LeftParenthesis, withBlock7.LeftParenthesisLocation);
							Serialize(withBlock7.Target);
							SerializeToken(TokenType.RightParenthesis, withBlock7.RightParenthesisLocation);
						}

						break;
					}

				case TreeType.CTypeExpression:
				case TreeType.DirectCastExpression:
					{
						{
							var withBlock8 = (CastTypeExpression)Expression;
							SerializeToken(TokenType.LeftParenthesis, withBlock8.LeftParenthesisLocation);
							Serialize(withBlock8.Operand);
							SerializeToken(TokenType.Comma, withBlock8.CommaLocation);
							Serialize(withBlock8.Target);
							SerializeToken(TokenType.RightParenthesis, withBlock8.RightParenthesisLocation);
						}

						break;
					}

				case TreeType.TypeOfExpression:
					{
						{
							var withBlock9 = (TypeOfExpression)Expression;
							Serialize(withBlock9.Operand);
							SerializeToken(TokenType.Is, withBlock9.IsLocation);
							Serialize(withBlock9.Target);
						}

						break;
					}

				case TreeType.IntrinsicCastExpression:
					{
						{
							var withBlock10 = (IntrinsicCastExpression)Expression;
							Writer.WriteAttributeString("intrinsicType", withBlock10.IntrinsicType.ToString());
							SerializeToken(TokenType.LeftParenthesis, withBlock10.LeftParenthesisLocation);
							Serialize(withBlock10.Operand);
							SerializeToken(TokenType.RightParenthesis, withBlock10.RightParenthesisLocation);
						}

						break;
					}

				case TreeType.QualifiedExpression:
					{
						{
							var withBlock11 = (QualifiedExpression)Expression;
							Serialize(withBlock11.Qualifier);
							SerializeToken(TokenType.Period, withBlock11.DotLocation);
							Serialize(withBlock11.Name);
						}

						break;
					}

				case TreeType.DictionaryLookupExpression:
					{
						{
							var withBlock12 = (DictionaryLookupExpression)Expression;
							Serialize(withBlock12.Qualifier);
							SerializeToken(TokenType.Exclamation, withBlock12.BangLocation);
							Serialize(withBlock12.Name);
						}

						break;
					}

				case TreeType.InstanceExpression:
					{
						{
							var withBlock13 = (InstanceExpression)Expression;
							Writer.WriteAttributeString("type", withBlock13.InstanceType.ToString());
						}

						break;
					}

				case TreeType.ParentheticalExpression:
					{
						{
							var withBlock14 = (ParentheticalExpression)Expression;
							Serialize(withBlock14.Operand);
							SerializeToken(TokenType.RightParenthesis, withBlock14.RightParenthesisLocation);
						}

						break;
					}

				case TreeType.BinaryOperatorExpression:
					{
						{
							var withBlock15 = (BinaryOperatorExpression)Expression;
							Writer.WriteAttributeString("operator", withBlock15.Operator.ToString());
							Serialize(withBlock15.LeftOperand);
							SerializeToken(GetOperatorToken(withBlock15.Operator), withBlock15.OperatorLocation);
							Serialize(withBlock15.RightOperand);
						}

						break;
					}


				case TreeType.UnaryOperatorExpression:
					{
						{
							var withBlock16 = (UnaryOperatorExpression)Expression;
							SerializeToken(GetOperatorToken(withBlock16.Operator), withBlock16.Span.Start);
							Serialize(withBlock16.Operand);
						}

						break;
					}

				default:
					{
						foreach (Tree Child in Expression.Children)
							Serialize(Child);
						break;
					}
			}
		}

		private void SerializeStatementComments(Tree Statement)
		{
			{
				var withBlock = (Statement)Statement;
				if (withBlock.Comments is not null)
				{
					foreach (CommentType Comment in withBlock.Comments)
						Serialize(Comment);
				}
			}
		}

		private void SerializeStatement(Tree Statement)
		{
			switch (Statement.Type)
			{
				case TreeType.GotoStatement:
				case TreeType.LabelStatement:
					{
						{
							var withBlock = (LabelReferenceStatement)Statement;
							Writer.WriteAttributeString("isLineNumber", Conversions.ToString(withBlock.IsLineNumber));
							SerializeStatementComments(Statement);
							Serialize(withBlock.Name);
						}

						break;
					}

				case TreeType.ContinueStatement:
					{
						{
							var withBlock1 = (ContinueStatement)Statement;
							Writer.WriteAttributeString("continueType", withBlock1.ContinueType.ToString());
							SerializeStatementComments(Statement);
							SerializeToken(GetBlockTypeToken(withBlock1.ContinueType), withBlock1.ContinueArgumentLocation);
						}

						break;
					}

				case TreeType.ExitStatement:
					{
						{
							var withBlock2 = (ExitStatement)Statement;
							Writer.WriteAttributeString("exitType", withBlock2.ExitType.ToString());
							SerializeStatementComments(Statement);
							SerializeToken(GetBlockTypeToken(withBlock2.ExitType), withBlock2.ExitArgumentLocation);
						}

						break;
					}

				case TreeType.ReturnStatement:
				case TreeType.ErrorStatement:
				case TreeType.ThrowStatement:
					{
						{
							var withBlock3 = (ExpressionStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock3.Expression);
						}

						break;
					}

				case TreeType.RaiseEventStatement:
					{
						{
							var withBlock4 = (RaiseEventStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock4.Name);
							Serialize(withBlock4.Arguments);
						}

						break;
					}

				case TreeType.AddHandlerStatement:
				case TreeType.RemoveHandlerStatement:
					{
						{
							var withBlock5 = (HandlerStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock5.Name);
							SerializeToken(TokenType.Comma, withBlock5.CommaLocation);
							Serialize(withBlock5.DelegateExpression);
						}

						break;
					}

				case TreeType.OnErrorStatement:
					{
						{
							var withBlock6 = (OnErrorStatement)Statement;
							Writer.WriteAttributeString("onErrorType", withBlock6.OnErrorType.ToString());
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.Error, withBlock6.ErrorLocation);

							switch (withBlock6.OnErrorType)
							{
								case OnErrorType.Zero:
									{
										SerializeToken(TokenType.GoTo, withBlock6.ResumeOrGoToLocation);
										Writer.WriteStartElement("Zero");
										SerializeLocation(withBlock6.NextOrZeroOrMinusLocation);
										Writer.WriteEndElement();
										break;
									}

								case OnErrorType.MinusOne:
									{
										SerializeToken(TokenType.GoTo, withBlock6.ResumeOrGoToLocation);
										SerializeToken(TokenType.Minus, withBlock6.NextOrZeroOrMinusLocation);
										Writer.WriteStartElement("One");
										SerializeLocation(withBlock6.OneLocation);
										Writer.WriteEndElement();
										break;
									}

								case OnErrorType.Label:
									{
										SerializeToken(TokenType.GoTo, withBlock6.ResumeOrGoToLocation);
										Serialize(withBlock6.Name);
										break;
									}

								case OnErrorType.Next:
									{
										SerializeToken(TokenType.Resume, withBlock6.ResumeOrGoToLocation);
										SerializeToken(TokenType.Next, withBlock6.NextOrZeroOrMinusLocation);
										break;
									}

								case OnErrorType.Bad:
									{
										break;
									}
									// Do nothing
							}
						}

						break;
					}

				case TreeType.ResumeStatement:
					{
						{
							var withBlock7 = (ResumeStatement)Statement;
							Writer.WriteAttributeString("resumeType", withBlock7.ResumeType.ToString());
							SerializeStatementComments(Statement);
							switch (withBlock7.ResumeType)
							{
								case ResumeType.Next:
									{
										SerializeToken(TokenType.Next, withBlock7.NextLocation);
										break;
									}

								case ResumeType.Label:
									{
										Serialize(withBlock7.Name);
										break;
									}

								case ResumeType.None:
									{
										break;
									}
									// Do nothing
							}
						}

						break;
					}

				case TreeType.ReDimStatement:
					{
						{
							var withBlock8 = (ReDimStatement)Statement;
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.Preserve, withBlock8.PreserveLocation);
							Serialize(withBlock8.Variables);
						}

						break;
					}

				case TreeType.EraseStatement:
					{
						{
							var withBlock9 = (EraseStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock9.Variables);
						}

						break;
					}

				case TreeType.CallStatement:
					{
						{
							var withBlock10 = (CallStatement)Statement;
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.Call, withBlock10.CallLocation);
							Serialize(withBlock10.TargetExpression);
							Serialize(withBlock10.Arguments);
						}

						break;
					}

				case TreeType.AssignmentStatement:
					{
						{
							var withBlock11 = (AssignmentStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock11.TargetExpression);
							SerializeToken(TokenType.Equals, withBlock11.OperatorLocation);
							Serialize(withBlock11.SourceExpression);
						}

						break;
					}

				case TreeType.MidAssignmentStatement:
					{
						{
							var withBlock12 = (MidAssignmentStatement)Statement;
							Writer.WriteAttributeString("hasTypeCharacter", Conversions.ToString(withBlock12.HasTypeCharacter));
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.LeftParenthesis, withBlock12.LeftParenthesisLocation);
							Serialize(withBlock12.TargetExpression);
							SerializeToken(TokenType.Comma, withBlock12.StartCommaLocation);
							Serialize(withBlock12.StartExpression);
							SerializeToken(TokenType.Comma, withBlock12.LengthCommaLocation);
							Serialize(withBlock12.LengthExpression);
							SerializeToken(TokenType.RightParenthesis, withBlock12.RightParenthesisLocation);
							SerializeToken(TokenType.Equals, withBlock12.OperatorLocation);
							Serialize(withBlock12.SourceExpression);
						}

						break;
					}

				case TreeType.CompoundAssignmentStatement:
					{
						{
							var withBlock13 = (CompoundAssignmentStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock13.TargetExpression);
							SerializeToken(GetCompoundAssignmentOperatorToken(withBlock13.CompoundOperator), withBlock13.OperatorLocation);
							Serialize(withBlock13.SourceExpression);
						}

						break;
					}

				case TreeType.LocalDeclarationStatement:
					{
						{
							var withBlock14 = (LocalDeclarationStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock14.Modifiers);
							Serialize(withBlock14.VariableDeclarators);
						}

						break;
					}

				case TreeType.EndBlockStatement:
					{
						{
							var withBlock15 = (EndBlockStatement)Statement;
							Writer.WriteAttributeString("endType", withBlock15.EndType.ToString());
							SerializeStatementComments(Statement);
							SerializeToken(GetBlockTypeToken(withBlock15.EndType), withBlock15.EndArgumentLocation);
						}

						break;
					}

				case TreeType.WithBlockStatement:
				case TreeType.SyncLockBlockStatement:
				case TreeType.WhileBlockStatement:
					{
						{
							var withBlock16 = (ExpressionBlockStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock16.Expression);
							Serialize(withBlock16.Statements);
							Serialize(withBlock16.EndStatement);
						}

						break;
					}

				case TreeType.UsingBlockStatement:
					{
						{
							var withBlock17 = (UsingBlockStatement)Statement;
							SerializeStatementComments(Statement);
							if (withBlock17.Expression is not null)
							{
								Serialize(withBlock17.Expression);
							}
							else
							{
								Serialize(withBlock17.VariableDeclarators);
							}
							Serialize(withBlock17.Statements);
							Serialize(withBlock17.EndStatement);
						}

						break;
					}

				case TreeType.DoBlockStatement:
					{
						{
							var withBlock18 = (DoBlockStatement)Statement;
							if (withBlock18.Expression is not null)
							{
								Writer.WriteAttributeString("isWhile", Conversion.Str(withBlock18.IsWhile));
								SerializeStatementComments(Statement);
								if (withBlock18.IsWhile)
								{
									SerializeToken(TokenType.While, withBlock18.WhileOrUntilLocation);
								}
								else
								{
									SerializeToken(TokenType.Until, withBlock18.WhileOrUntilLocation);
								}
								Serialize(withBlock18.Expression);
							}
							else
							{
								SerializeStatementComments(Statement);
							}
							Serialize(withBlock18.Statements);
							Serialize(withBlock18.EndStatement);
						}

						break;
					}

				case TreeType.LoopStatement:
					{
						{
							var withBlock19 = (LoopStatement)Statement;
							if (withBlock19.Expression is not null)
							{
								Writer.WriteAttributeString("isWhile", Conversion.Str(withBlock19.IsWhile));
								SerializeStatementComments(Statement);
								if (withBlock19.IsWhile)
								{
									SerializeToken(TokenType.While, withBlock19.WhileOrUntilLocation);
								}
								else
								{
									SerializeToken(TokenType.Until, withBlock19.WhileOrUntilLocation);
								}
								Serialize(withBlock19.Expression);
							}
							else
							{
								SerializeStatementComments(Statement);
							}
						}

						break;
					}

				case TreeType.NextStatement:
					{
						{
							var withBlock20 = (NextStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock20.Variables);
						}

						break;
					}

				case TreeType.ForBlockStatement:
					{
						{
							var withBlock21 = (ForBlockStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock21.ControlExpression);
							Serialize(withBlock21.ControlVariableDeclarator);
							SerializeToken(TokenType.Equals, withBlock21.EqualsLocation);
							Serialize(withBlock21.LowerBoundExpression);
							SerializeToken(TokenType.To, withBlock21.ToLocation);
							Serialize(withBlock21.UpperBoundExpression);
							SerializeToken(TokenType.Step, withBlock21.StepLocation);
							Serialize(withBlock21.StepExpression);
							Serialize(withBlock21.Statements);
							Serialize(withBlock21.NextStatement);
						}

						break;
					}

				case TreeType.ForEachBlockStatement:
					{
						{
							var withBlock22 = (ForEachBlockStatement)Statement;
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.Each, withBlock22.EachLocation);
							Serialize(withBlock22.ControlExpression);
							Serialize(withBlock22.ControlVariableDeclarator);
							SerializeToken(TokenType.In, withBlock22.InLocation);
							Serialize(withBlock22.CollectionExpression);
							Serialize(withBlock22.Statements);
							Serialize(withBlock22.NextStatement);
						}

						break;
					}

				case TreeType.CatchStatement:
					{
						{
							var withBlock23 = (CatchStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock23.Name);
							SerializeToken(TokenType.As, withBlock23.AsLocation);
							Serialize(withBlock23.ExceptionType);
							SerializeToken(TokenType.When, withBlock23.WhenLocation);
							Serialize(withBlock23.FilterExpression);
						}

						break;
					}

				case TreeType.CaseElseStatement:
					{
						{
							var withBlock24 = (CaseElseStatement)Statement;
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.Else, withBlock24.ElseLocation);
						}

						break;
					}

				case TreeType.SelectBlockStatement:
					{
						{
							var withBlock25 = (SelectBlockStatement)Statement;
							SerializeStatementComments(Statement);
							SerializeToken(TokenType.Case, withBlock25.CaseLocation);
							Serialize(withBlock25.Expression);
							Serialize(withBlock25.Statements);
							Serialize(withBlock25.CaseBlockStatements);
							Serialize(withBlock25.CaseElseBlockStatement);
							Serialize(withBlock25.EndStatement);
						}

						break;
					}

				case TreeType.ElseIfStatement:
					{
						{
							var withBlock26 = (ElseIfStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock26.Expression);
							SerializeToken(TokenType.Then, withBlock26.ThenLocation);
						}

						break;
					}

				case TreeType.IfBlockStatement:
					{
						{
							var withBlock27 = (IfBlockStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock27.Expression);
							SerializeToken(TokenType.Then, withBlock27.ThenLocation);
							Serialize(withBlock27.Statements);
							Serialize(withBlock27.ElseIfBlockStatements);
							Serialize(withBlock27.ElseBlockStatement);
							Serialize(withBlock27.EndStatement);
						}

						break;
					}

				case TreeType.LineIfBlockStatement:
					{
						{
							var withBlock28 = (LineIfStatement)Statement;
							SerializeStatementComments(Statement);
							Serialize(withBlock28.Expression);
							SerializeToken(TokenType.Then, withBlock28.ThenLocation);
							Serialize(withBlock28.IfStatements);
							SerializeToken(TokenType.Else, withBlock28.ElseLocation);
							Serialize(withBlock28.ElseStatements);
						}

						break;
					}

				default:
					{
						SerializeStatementComments(Statement);
						foreach (Tree Child in Statement.Children)
							Serialize(Child);
						break;
					}
			}
		}

		private void SerializeCaseClause(Tree CaseClause)
		{
			switch (CaseClause.Type)
			{
				case TreeType.ComparisonCaseClause:
					{
						{
							var withBlock = (ComparisonCaseClause)CaseClause;
							SerializeToken(TokenType.Is, withBlock.IsLocation);
							SerializeToken(GetOperatorToken(withBlock.ComparisonOperator), withBlock.OperatorLocation);
							Serialize(withBlock.Operand);
						}

						break;
					}

				case TreeType.RangeCaseClause:
					{
						{
							var withBlock1 = (RangeCaseClause)CaseClause;
							Serialize(withBlock1.RangeExpression);
						}

						break;
					}

				default:
					{
						Debug.Assert(false, "Unexpected.");
						break;
					}
			}
		}

		private void SerializeDeclarationComments(Tree Declaration)
		{
			{
				var withBlock = (Declaration)Declaration;
				if (withBlock.Comments is not null)
				{
					foreach (CommentType Comment in withBlock.Comments)
						Serialize(Comment);
				}
			}
		}

		private void SerializeDeclaration(Tree Declaration)
		{
			switch (Declaration.Type)
			{
				case TreeType.EndBlockDeclaration:
					{
						{
							var withBlock = (EndBlockDeclaration)Declaration;
							Writer.WriteAttributeString("endType", withBlock.EndType.ToString());
							SerializeDeclarationComments(Declaration);
							SerializeToken(GetBlockTypeToken(withBlock.EndType), withBlock.EndArgumentLocation);
						}

						break;
					}

				case TreeType.EventDeclaration:
					{
						{
							var withBlock1 = (EventDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock1.Attributes);
							Serialize(withBlock1.Modifiers);
							SerializeToken(TokenType.Event, withBlock1.KeywordLocation);
							Serialize(withBlock1.Name);
							Serialize(withBlock1.Parameters);
							SerializeToken(TokenType.As, withBlock1.AsLocation);
							Serialize(withBlock1.ResultTypeAttributes);
							Serialize(withBlock1.ResultType);
							Serialize(withBlock1.ImplementsList);
						}

						break;
					}

				case TreeType.CustomEventDeclaration:
					{
						{
							var withBlock2 = (CustomEventDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock2.Attributes);
							Serialize(withBlock2.Modifiers);
							SerializeToken(TokenType.Custom, withBlock2.CustomLocation);
							SerializeToken(TokenType.Event, withBlock2.KeywordLocation);
							Serialize(withBlock2.Name);
							SerializeToken(TokenType.As, withBlock2.AsLocation);
							Serialize(withBlock2.ResultType);
							Serialize(withBlock2.ImplementsList);
							Serialize(withBlock2.Accessors);
							Serialize(withBlock2.EndDeclaration);
						}

						break;
					}

				case TreeType.ConstructorDeclaration:
				case TreeType.SubDeclaration:
				case TreeType.FunctionDeclaration:
					{
						{
							var withBlock3 = (MethodDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock3.Attributes);
							Serialize(withBlock3.Modifiers);

							switch (Declaration.Type)
							{
								case TreeType.SubDeclaration:
									{
										SerializeToken(TokenType.Sub, withBlock3.KeywordLocation);
										break;
									}

								case TreeType.FunctionDeclaration:
									{
										SerializeToken(TokenType.Function, withBlock3.KeywordLocation);
										break;
									}

								case TreeType.ConstructorDeclaration:
									{
										SerializeToken(TokenType.New, withBlock3.KeywordLocation);
										break;
									}
							}

							Serialize(withBlock3.Name);
							Serialize(withBlock3.Parameters);
							Serialize(withBlock3.TypeParameters);
							SerializeToken(TokenType.As, withBlock3.AsLocation);
							Serialize(withBlock3.ResultTypeAttributes);
							Serialize(withBlock3.ResultType);
							Serialize(withBlock3.ImplementsList);
							Serialize(withBlock3.HandlesList);
							Serialize(withBlock3.Statements);
							Serialize(withBlock3.EndDeclaration);
						}

						break;
					}

				case TreeType.OperatorDeclaration:
					{
						{
							var withBlock4 = (OperatorDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock4.Attributes);
							Serialize(withBlock4.Modifiers);
							SerializeToken(TokenType.Operator, withBlock4.KeywordLocation);
							if (withBlock4.OperatorToken is not null)
							{
								SerializeToken(withBlock4.OperatorToken.Type, withBlock4.OperatorToken.Span.Start);
							}
							Serialize(withBlock4.Parameters);
							SerializeToken(TokenType.As, withBlock4.AsLocation);
							Serialize(withBlock4.ResultTypeAttributes);
							Serialize(withBlock4.ResultType);
							Serialize(withBlock4.Statements);
							Serialize(withBlock4.EndDeclaration);
						}

						break;
					}

				case TreeType.ExternalSubDeclaration:
				case TreeType.ExternalFunctionDeclaration:
					{
						{
							var withBlock5 = (ExternalDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock5.Attributes);
							Serialize(withBlock5.Modifiers);

							SerializeToken(TokenType.Declare, withBlock5.KeywordLocation);

							switch (withBlock5.Charset)
							{
								case Charset.Auto:
									{
										SerializeToken(TokenType.Auto, withBlock5.CharsetLocation);
										break;
									}

								case Charset.Ansi:
									{
										SerializeToken(TokenType.Ansi, withBlock5.CharsetLocation);
										break;
									}

								case Charset.Unicode:
									{
										SerializeToken(TokenType.Unicode, withBlock5.CharsetLocation);
										break;
									}
							}

							switch (Declaration.Type)
							{
								case TreeType.ExternalSubDeclaration:
									{
										SerializeToken(TokenType.Sub, withBlock5.SubOrFunctionLocation);
										break;
									}

								case TreeType.ExternalFunctionDeclaration:
									{
										SerializeToken(TokenType.Function, withBlock5.SubOrFunctionLocation);
										break;
									}
							}

							Serialize(withBlock5.Name);
							SerializeToken(TokenType.Lib, withBlock5.LibLocation);
							Serialize(withBlock5.LibLiteral);
							SerializeToken(TokenType.Alias, withBlock5.AliasLocation);
							Serialize(withBlock5.AliasLiteral);
							Serialize(withBlock5.Parameters);
							SerializeToken(TokenType.As, withBlock5.AsLocation);
							Serialize(withBlock5.ResultTypeAttributes);
							Serialize(withBlock5.ResultType);
						}

						break;
					}

				case TreeType.PropertyDeclaration:
					{
						{
							var withBlock6 = (PropertyDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock6.Attributes);
							Serialize(withBlock6.Modifiers);
							SerializeToken(TokenType.Event, withBlock6.KeywordLocation);
							Serialize(withBlock6.Name);
							Serialize(withBlock6.Parameters);
							SerializeToken(TokenType.As, withBlock6.AsLocation);
							Serialize(withBlock6.ResultTypeAttributes);
							Serialize(withBlock6.ResultType);
							Serialize(withBlock6.ImplementsList);
							Serialize(withBlock6.Accessors);
							Serialize(withBlock6.EndDeclaration);
						}

						break;
					}

				case TreeType.GetAccessorDeclaration:
					{
						{
							var withBlock7 = (GetAccessorDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock7.Attributes);
							Serialize(withBlock7.Modifiers);
							SerializeToken(TokenType.Get, withBlock7.GetLocation);
							Serialize(withBlock7.Statements);
							Serialize(withBlock7.EndDeclaration);
						}

						break;
					}

				case TreeType.SetAccessorDeclaration:
					{
						{
							var withBlock8 = (SetAccessorDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock8.Attributes);
							Serialize(withBlock8.Modifiers);
							SerializeToken(TokenType.Set, withBlock8.SetLocation);
							Serialize(withBlock8.Parameters);
							Serialize(withBlock8.Statements);
							Serialize(withBlock8.EndDeclaration);
						}

						break;
					}

				case TreeType.EnumValueDeclaration:
					{
						{
							var withBlock9 = (EnumValueDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock9.Attributes);
							Serialize(withBlock9.Modifiers);
							Serialize(withBlock9.Name);
							SerializeToken(TokenType.Equals, withBlock9.EqualsLocation);
							Serialize(withBlock9.Expression);
						}

						break;
					}

				case TreeType.DelegateSubDeclaration:
					{
						{
							var withBlock10 = (DelegateSubDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock10.Attributes);
							Serialize(withBlock10.Modifiers);
							SerializeToken(TokenType.Delegate, withBlock10.KeywordLocation);
							SerializeToken(TokenType.Sub, withBlock10.SubOrFunctionLocation);
							Serialize(withBlock10.Name);
							Serialize(withBlock10.TypeParameters);
							Serialize(withBlock10.Parameters);
						}

						break;
					}

				case TreeType.DelegateFunctionDeclaration:
					{
						{
							var withBlock11 = (DelegateFunctionDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock11.Attributes);
							Serialize(withBlock11.Modifiers);
							SerializeToken(TokenType.Delegate, withBlock11.KeywordLocation);
							SerializeToken(TokenType.Function, withBlock11.SubOrFunctionLocation);
							Serialize(withBlock11.Name);
							Serialize(withBlock11.Parameters);
							SerializeToken(TokenType.As, withBlock11.AsLocation);
							Serialize(withBlock11.ResultTypeAttributes);
							Serialize(withBlock11.ResultType);
						}

						break;
					}

				case TreeType.ModuleDeclaration:
					{
						{
							var withBlock12 = (BlockDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock12.Attributes);
							Serialize(withBlock12.Modifiers);
							SerializeToken(TokenType.Module, withBlock12.KeywordLocation);
							Serialize(withBlock12.Name);
							Serialize(withBlock12.Declarations);
							Serialize(withBlock12.EndDeclaration);
						}

						break;
					}

				case TreeType.ClassDeclaration:
				case TreeType.StructureDeclaration:
				case TreeType.InterfaceDeclaration:
					{
						{
							var withBlock13 = (GenericBlockDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock13.Attributes);
							Serialize(withBlock13.Modifiers);

							switch (Declaration.Type)
							{
								case TreeType.ClassDeclaration:
									{
										SerializeToken(TokenType.Class, withBlock13.KeywordLocation);
										break;
									}

								case TreeType.StructureDeclaration:
									{
										SerializeToken(TokenType.Structure, withBlock13.KeywordLocation);
										break;
									}

								case TreeType.ModuleDeclaration:
									{
										SerializeToken(TokenType.Module, withBlock13.KeywordLocation);
										break;
									}

								case TreeType.InterfaceDeclaration:
									{
										SerializeToken(TokenType.Interface, withBlock13.KeywordLocation);
										break;
									}
							}

							Serialize(withBlock13.Name);
							Serialize(withBlock13.TypeParameters);
							Serialize(withBlock13.Declarations);
							Serialize(withBlock13.EndDeclaration);
						}

						break;
					}

				case TreeType.NamespaceDeclaration:
					{
						{
							var withBlock14 = (NamespaceDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock14.Attributes);
							Serialize(withBlock14.Modifiers);

							SerializeToken(TokenType.Namespace, withBlock14.NamespaceLocation);
							Serialize(withBlock14.Name);
							Serialize(withBlock14.Declarations);
							Serialize(withBlock14.EndDeclaration);
						}

						break;
					}

				case TreeType.EnumDeclaration:
					{
						{
							var withBlock15 = (EnumDeclaration)Declaration;
							SerializeDeclarationComments(Declaration);
							Serialize(withBlock15.Attributes);
							Serialize(withBlock15.Modifiers);
							SerializeToken(TokenType.Enum, withBlock15.KeywordLocation);
							Serialize(withBlock15.Name);
							SerializeToken(TokenType.As, withBlock15.AsLocation);
							Serialize(withBlock15.ElementType);
							Serialize(withBlock15.Declarations);
							Serialize(withBlock15.EndDeclaration);
						}

						break;
					}

				case TreeType.OptionDeclaration:
					{
						{
							var withBlock16 = (OptionDeclaration)Declaration;
							Writer.WriteAttributeString("type", withBlock16.OptionType.ToString());
							SerializeDeclarationComments(Declaration);

							switch (withBlock16.OptionType)
							{
								case OptionType.SyntaxError:
									{
										break;
									}

								case OptionType.Explicit:
								case OptionType.ExplicitOn:
								case OptionType.ExplicitOff:
									{
										Writer.WriteStartElement("Explicit");
										SerializeLocation(withBlock16.OptionTypeLocation);
										Writer.WriteEndElement();

										if (withBlock16.OptionType == OptionType.ExplicitOn)
										{
											SerializeToken(TokenType.On, withBlock16.OptionArgumentLocation);
										}
										else if (withBlock16.OptionType == OptionType.ExplicitOff)
										{
											Writer.WriteStartElement("Off");
											SerializeLocation(withBlock16.OptionArgumentLocation);
											Writer.WriteEndElement();
										}

										break;
									}

								case OptionType.Strict:
								case OptionType.StrictOn:
								case OptionType.StrictOff:
									{

										Writer.WriteStartElement("Strict");
										SerializeLocation(withBlock16.OptionTypeLocation);
										Writer.WriteEndElement();

										if (withBlock16.OptionType == OptionType.StrictOn)
										{
											SerializeToken(TokenType.On, withBlock16.OptionArgumentLocation);
										}
										else if (withBlock16.OptionType == OptionType.StrictOff)
										{
											Writer.WriteStartElement("Off");
											SerializeLocation(withBlock16.OptionArgumentLocation);
											Writer.WriteEndElement();
										}

										break;
									}

								case OptionType.CompareBinary:
								case OptionType.CompareText:
									{
										Writer.WriteStartElement("Compare");
										SerializeLocation(withBlock16.OptionTypeLocation);
										Writer.WriteEndElement();

										if (withBlock16.OptionType == OptionType.CompareBinary)
										{
											Writer.WriteStartElement("Binary");
											SerializeLocation(withBlock16.OptionArgumentLocation);
											Writer.WriteEndElement();
										}
										else
										{
											Writer.WriteStartElement("Text");
											SerializeLocation(withBlock16.OptionArgumentLocation);
											Writer.WriteEndElement();
										}

										break;
									}
							}
						}

						break;
					}

				default:
					{
						SerializeDeclarationComments(Declaration);
						foreach (Tree Child in Declaration.Children)
							Serialize(Child);
						break;
					}
			}
		}

		private void SerializeImport(Tree Import)
		{
			switch (Import.Type)
			{
				case TreeType.NameImport:
					{
						{
							var withBlock = (NameImport)Import;
							Serialize(withBlock.TypeName);
						}

						break;
					}

				case TreeType.AliasImport:
					{
						{
							var withBlock1 = (AliasImport)Import;
							Serialize(withBlock1.AliasedTypeName);
							SerializeToken(TokenType.Equals, withBlock1.EqualsLocation);
							Serialize(withBlock1.Name);
						}

						break;
					}
			}
		}

		public void Serialize(Tree Tree)
		{
			if (Tree is null)
			{
				return;
			}

			Writer.WriteStartElement(Tree.Type.ToString());
			if (Tree.IsBad)
			{
				Writer.WriteAttributeString("isBad", Conversions.ToString(true));
			}
			SerializeSpan(Tree.Span);

			switch (Tree.Type)
			{
				case var @case when TreeType.ArgumentCollection <= @case && @case <= TreeType.DeclarationCollection:
					{
						SerializeList(Tree);
						break;
					}

				case TreeType.Comment:
					{
						{
							var withBlock = (CommentType)Tree;
							Writer.WriteAttributeString("isRem", Conversions.ToString(withBlock.IsREM));

							if (withBlock.Comment is not null)
							{
								Writer.WriteString(withBlock.Comment);
							}
						}

						break;
					}

				case var case1 when TreeType.SimpleName <= case1 && case1 <= TreeType.QualifiedName:
					{
						SerializeName(Tree);
						break;
					}

				case var case2 when TreeType.IntrinsicType <= case2 && case2 <= TreeType.ArrayType:
					{
						SerializeType(Tree);
						break;
					}

				case TreeType.Argument:
					{
						{
							var withBlock1 = (Argument)Tree;
							Serialize(withBlock1.Name);
							SerializeToken(TokenType.ColonEquals, withBlock1.ColonEqualsLocation);
							Serialize(withBlock1.Expression);
						}

						break;
					}

				case var case3 when TreeType.SimpleNameExpression <= case3 && case3 <= TreeType.GetTypeExpression:
					{
						SerializeExpression(Tree);
						break;
					}

				case var case4 when TreeType.EmptyStatement <= case4 && case4 <= TreeType.EndBlockStatement:
					{
						SerializeStatement(Tree);
						break;
					}

				case TreeType.Modifier:
					{
						{
							var withBlock2 = (Modifier)Tree;
							Writer.WriteAttributeString("type", withBlock2.ModifierType.ToString());
						}

						break;
					}

				case TreeType.VariableDeclarator:
					{
						{
							var withBlock3 = (VariableDeclarator)Tree;
							Serialize(withBlock3.VariableNames);
							SerializeToken(TokenType.As, withBlock3.AsLocation);
							SerializeToken(TokenType.New, withBlock3.NewLocation);
							Serialize(withBlock3.VariableType);
							Serialize(withBlock3.Arguments);
							SerializeToken(TokenType.Equals, withBlock3.EqualsLocation);
							Serialize(withBlock3.Initializer);
						}

						break;
					}

				case var case5 when TreeType.ComparisonCaseClause <= case5 && case5 <= TreeType.RangeCaseClause:
					{
						SerializeCaseClause(Tree);
						break;
					}

				case TreeType.Attribute:
					{
						{
							var withBlock4 = (Attribute)Tree;
							Writer.WriteAttributeString("type", withBlock4.AttributeType.ToString());

							switch (withBlock4.AttributeType)
							{
								case AttributeTypes.Assembly:
									{
										SerializeToken(TokenType.Colon, withBlock4.ColonLocation);
										SerializeToken(TokenType.Assembly, withBlock4.AttributeTypeLocation);
										break;
									}

								case AttributeTypes.Module:
									{
										SerializeToken(TokenType.Module, withBlock4.AttributeTypeLocation);
										SerializeToken(TokenType.Colon, withBlock4.ColonLocation);
										break;
									}
							}

							Serialize(withBlock4.Name);
							Serialize(withBlock4.Arguments);
						}

						break;
					}

				case var case6 when TreeType.EmptyDeclaration <= case6 && case6 <= TreeType.DelegateFunctionDeclaration:
					{
						SerializeDeclaration(Tree);
						break;
					}

				case TreeType.Parameter:
					{
						{
							var withBlock5 = (Parameter)Tree;
							Serialize(withBlock5.Attributes);
							Serialize(withBlock5.Modifiers);
							Serialize(withBlock5.VariableName);
							SerializeToken(TokenType.As, withBlock5.AsLocation);
							Serialize(withBlock5.ParameterType);
							SerializeToken(TokenType.Equals, withBlock5.EqualsLocation);
							Serialize(withBlock5.Initializer);
						}

						break;
					}

				case TreeType.TypeParameter:
					{
						{
							var withBlock6 = (TypeParameter)Tree;
							Serialize(withBlock6.TypeName);
							SerializeToken(TokenType.As, withBlock6.AsLocation);
							Serialize(withBlock6.TypeConstraints);
						}

						break;
					}

				case var case7 when TreeType.NameImport <= case7 && case7 <= TreeType.AliasImport:
					{
						SerializeImport(Tree);
						break;
					}

				default:
					{
						foreach (Tree Child in Tree.Children)
							Serialize(Child);
						break;
					}
			}

			Writer.WriteEndElement();
		}
	}
}