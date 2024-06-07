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
/// A parse tree for a For Each statement.
/// </summary>
	public sealed class ForEachBlockStatement : BlockStatement
	{

		private readonly Location _EachLocation;
		private readonly Expression _ControlExpression;
		private readonly VariableDeclarator _ControlVariableDeclarator;
		private readonly Location _InLocation;
		private readonly Expression _CollectionExpression;
		private readonly NextStatement _NextStatement;

		/// <summary>
    /// The location of the 'Each'.
    /// </summary>
		public Location EachLocation
		{
			get
			{
				return _EachLocation;
			}
		}

		/// <summary>
    /// The control expression.
    /// </summary>
		public Expression ControlExpression
		{
			get
			{
				return _ControlExpression;
			}
		}

		/// <summary>
    /// The control variable declarator, if any.
    /// </summary>
		public VariableDeclarator ControlVariableDeclarator
		{
			get
			{
				return _ControlVariableDeclarator;
			}
		}

		/// <summary>
    /// The location of the 'In'.
    /// </summary>
		public Location InLocation
		{
			get
			{
				return _InLocation;
			}
		}

		/// <summary>
    /// The collection expression.
    /// </summary>
		public Expression CollectionExpression
		{
			get
			{
				return _CollectionExpression;
			}
		}

		/// <summary>
    /// The Next statement, if any.
    /// </summary>
		public NextStatement NextStatement
		{
			get
			{
				return _NextStatement;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a For Each statement.
    /// </summary>
    /// <param name="eachLocation">The location of the 'Each'.</param>
    /// <param name="controlExpression">The control expression.</param>
    /// <param name="controlVariableDeclarator">The control variable declarator, if any.</param>
    /// <param name="inLocation">The location of the 'In'.</param>
    /// <param name="collectionExpression">The collection expression.</param>
    /// <param name="statements">The statements in the block.</param>
    /// <param name="nextStatement">The Next statement, if any.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ForEachBlockStatement(Location eachLocation, Expression controlExpression, VariableDeclarator controlVariableDeclarator, Location inLocation, Expression collectionExpression, StatementCollection statements, NextStatement nextStatement, Span span, IList<CommentType> comments) : base(TreeType.ForEachBlockStatement, statements, span, comments)
		{

			if (controlExpression is null)
			{
				throw new ArgumentNullException("controlExpression");
			}

			SetParent(controlExpression);
			SetParent(controlVariableDeclarator);
			SetParent(collectionExpression);
			SetParent(nextStatement);

			_EachLocation = eachLocation;
			_ControlExpression = controlExpression;
			_ControlVariableDeclarator = controlVariableDeclarator;
			_InLocation = inLocation;
			_CollectionExpression = collectionExpression;
			_NextStatement = nextStatement;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, ControlExpression);
			AddChild(childList, ControlVariableDeclarator);
			AddChild(childList, CollectionExpression);
			base.GetChildTrees(childList);
			AddChild(childList, NextStatement);
		}
	}
}