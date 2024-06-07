using System.Diagnostics;

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
/// A parse tree for a special name (i.e. 'Global').
/// </summary>
	public sealed class SpecialName : Name
	{

		/// <summary>
    /// Constructs a new special name parse tree.
    /// </summary>
    /// <param name="span">The location of the parse tree.</param>
		public SpecialName(TreeType @type, Span span) : base(type, span)
		{

			Debug.Assert(type == TreeType.GlobalNamespaceName || type == TreeType.MeName || type == TreeType.MyBaseName);
		}
	}
}