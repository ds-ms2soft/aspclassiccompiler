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
/// A parse tree for a simple name expression.
/// </summary>
	public sealed class SimpleNameExpression : Expression
	{

		private readonly SimpleName _Name;

		/// <summary>
    /// The name.
    /// </summary>
		public SimpleName Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a simple name expression.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="span">The location of the parse tree.</param>
		public SimpleNameExpression(SimpleName name, Span span) : base(TreeType.SimpleNameExpression, span)
		{

			if (name is null)
			{
				throw new ArgumentNullException("name");
			}

			SetParent(name);

			_Name = name;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Name);
		}
	}
}