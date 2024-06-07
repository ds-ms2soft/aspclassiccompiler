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
/// A parse tree for a case clause in a Select statement.
/// </summary>
	public abstract class CaseClause : Tree
	{

		protected CaseClause(TreeType @type, Span span) : base(type, span)
		{

			Debug.Assert(type == TreeType.ComparisonCaseClause || type == TreeType.RangeCaseClause);
		}
	}
}