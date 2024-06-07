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
/// A parse tree for a Case Else statement.
/// </summary>
	public sealed class CaseElseStatement : Statement
	{

		private readonly Location _ElseLocation;

		/// <summary>
    /// The location of the 'Else'.
    /// </summary>
		public Location ElseLocation
		{
			get
			{
				return _ElseLocation;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a Case Else statement.
    /// </summary>
    /// <param name="elseLocation">The location of the 'Else'.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public CaseElseStatement(Location elseLocation, Span span, IList<CommentType> comments) : base(TreeType.CaseElseStatement, span, comments)
		{

			_ElseLocation = elseLocation;
		}
	}
}