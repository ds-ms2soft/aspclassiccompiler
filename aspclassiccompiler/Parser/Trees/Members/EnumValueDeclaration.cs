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
/// A parse tree for an enumerated value declaration.
/// </summary>
	public sealed class EnumValueDeclaration : ModifiedDeclaration
	{

		private readonly Name _Name;
		private readonly Location _EqualsLocation;
		private readonly Expression _Expression;

		/// <summary>
    /// The name of the enumerated value.
    /// </summary>
		public Name Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// The location of the '=', if any.
    /// </summary>
		public Location EqualsLocation
		{
			get
			{
				return _EqualsLocation;
			}
		}

		/// <summary>
    /// The enumerated value, if any.
    /// </summary>
		public Expression Expression
		{
			get
			{
				return _Expression;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for an enumerated value.
    /// </summary>
    /// <param name="attributes">The attributes on the declaration.</param>
    /// <param name="name">The name of the declaration.</param>
    /// <param name="equalsLocation">The location of the '=', if any.</param>
    /// <param name="expression">The enumerated value, if any.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public EnumValueDeclaration(AttributeBlockCollection attributes, Name name, Location equalsLocation, Expression expression, Span span, IList<CommentType> comments) : base(TreeType.EnumValueDeclaration, attributes, null, span, comments)
		{

			if (name is null)
			{
				throw new ArgumentNullException("name");
			}

			SetParent(name);
			SetParent(expression);

			_Name = name;
			_EqualsLocation = equalsLocation;
			_Expression = expression;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);

			AddChild(childList, Name);
			AddChild(childList, Expression);
		}
	}
}