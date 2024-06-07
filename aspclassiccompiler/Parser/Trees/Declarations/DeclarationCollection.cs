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
/// A read-only collection of declarations.
/// </summary>
	public sealed class DeclarationCollection : ColonDelimitedTreeCollection<Declaration>
	{

		/// <summary>
    /// Constructs a new collection of declarations.
    /// </summary>
    /// <param name="declarations">The declarations in the collection.</param>
    /// <param name="colonLocations">The locations of the colons in the collection.</param>
    /// <param name="span">The location of the parse tree.</param>
		public DeclarationCollection(IList<Declaration> declarations, IList<Location> colonLocations, Span span) : base(TreeType.DeclarationCollection, declarations, colonLocations, span)
		{

			// A declaration collection may need to hold just a colon.
			if ((declarations is null || declarations.Count == 0) && (colonLocations is null || colonLocations.Count == 0))
			{
				throw new ArgumentException("DeclarationCollection cannot be empty.");
			}
		}
	}
}