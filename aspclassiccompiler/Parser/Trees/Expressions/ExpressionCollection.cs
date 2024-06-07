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
/// A read-only collection of expressions.
/// </summary>
	public sealed class ExpressionCollection : CommaDelimitedTreeCollection<Expression>
	{

		/// <summary>
    /// Constructs a new collection of expressions.
    /// </summary>
    /// <param name="expressions">The expressions in the collection.</param>
    /// <param name="commaLocations">The locations of the commas in the collection.</param>
    /// <param name="span">The location of the parse tree.</param>
		public ExpressionCollection(IList<Expression> expressions, IList<Location> commaLocations, Span span) : base(TreeType.ExpressionCollection, expressions, commaLocations, span)
		{

			if ((expressions is null || expressions.Count == 0) && (commaLocations is null || commaLocations.Count == 0))
			{
				throw new ArgumentException("ExpressionCollection cannot be empty.");
			}
		}
	}
}