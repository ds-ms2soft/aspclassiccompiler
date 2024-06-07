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
/// A read-only collection of names.
/// </summary>
	public sealed class NameCollection : CommaDelimitedTreeCollection<Name>
	{
		/// <summary>
    /// Constructs a new name collection.
    /// </summary>
    /// <param name="names">The names in the collection.</param>
    /// <param name="commaLocations">The locations of the commas in the collection.</param>
    /// <param name="span">The location of the parse tree.</param>
		public NameCollection(IList<Name> names, IList<Location> commaLocations, Span span) : base(TreeType.NameCollection, names, commaLocations, span)
		{

			if (names is null || names.Count == 0)
			{
				throw new ArgumentException("NameCollection cannot be empty.");
			}
		}
	}
}