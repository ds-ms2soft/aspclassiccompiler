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
/// A parse tree to represent a variable name.
/// </summary>
/// <remarks>
/// A variable name can have an array modifier after it (e.g. 'x(10) As Integer').
/// </remarks>
	public sealed class VariableName : Name
	{

		private readonly SimpleName _Name;
		private readonly ArrayTypeName _ArrayType;

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
    /// The array modifier, if any.
    /// </summary>
		public ArrayTypeName ArrayType
		{
			get
			{
				return _ArrayType;
			}
		}

		/// <summary>
    /// Constructs a new variable name parse tree.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="arrayType">The array modifier, if any.</param>
    /// <param name="span">The location of the parse tree.</param>
		public VariableName(SimpleName name, ArrayTypeName arrayType, Span span) : base(TreeType.VariableName, span)
		{

			if (name is null)
			{
				throw new ArgumentNullException("name");
			}

			SetParent(name);
			SetParent(arrayType);

			_Name = name;
			_ArrayType = arrayType;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Name);
			AddChild(childList, ArrayType);
		}
	}
}