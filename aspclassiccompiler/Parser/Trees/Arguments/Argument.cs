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
/// A parse tree for an argument to a call or index.
/// </summary>
	public sealed class Argument : Tree
	{

		private readonly SimpleName _Name;
		private readonly Location _ColonEqualsLocation;
		private readonly Expression _Expression;

		/// <summary>
    /// The name of the argument, if any.
    /// </summary>
		public SimpleName Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// The location of the ':=', if any.
    /// </summary>
		public Location ColonEqualsLocation
		{
			get
			{
				return _ColonEqualsLocation;
			}
		}

		/// <summary>
    /// The argument, if any.
    /// </summary>
		public Expression Expression
		{
			get
			{
				return _Expression;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for an argument.
    /// </summary>
    /// <param name="name">The name of the argument, if any.</param>
    /// <param name="colonEqualsLocation">The location of the ':=', if any.</param>
    /// <param name="expression">The expression, if any.</param>
    /// <param name="span">The location of the parse tree.</param>
		public Argument(SimpleName name, Location colonEqualsLocation, Expression expression, Span span) : base(TreeType.Argument, span)
		{

			if (expression is null)
			{
				throw new ArgumentNullException("expression");
			}

			SetParent(name);
			SetParent(expression);

			_Name = name;
			_ColonEqualsLocation = colonEqualsLocation;
			_Expression = expression;
		}

		private Argument() : base(TreeType.Argument, default)
		{
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Name);
			AddChild(childList, Expression);
		}
	}
}