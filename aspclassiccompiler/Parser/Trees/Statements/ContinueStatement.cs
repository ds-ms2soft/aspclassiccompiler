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
/// A parse tree for an Continue statement.
/// </summary>
	public sealed class ContinueStatement : Statement
	{

		private readonly BlockType _ContinueType;
		private readonly Location _ContinueArgumentLocation;

		/// <summary>
    /// The type of tree this statement continues.
    /// </summary>
		public BlockType ContinueType
		{
			get
			{
				return _ContinueType;
			}
		}

		/// <summary>
    /// The location of the Continue statement type.
    /// </summary>
		public Location ContinueArgumentLocation
		{
			get
			{
				return _ContinueArgumentLocation;
			}
		}

		/// <summary>
    /// Constructs a parse tree for an Continue statement.
    /// </summary>
    /// <param name="continueType">The type of tree this statement continues.</param>
    /// <param name="continueArgumentLocation">The location of the Continue statement type.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ContinueStatement(BlockType continueType, Location continueArgumentLocation, Span span, IList<CommentType> comments) : base(TreeType.ContinueStatement, span, comments)
		{

			switch (continueType)
			{
				// OK

				case BlockType.Do:
				case BlockType.For:
				case BlockType.While:
				case BlockType.None:
					{
						break;
					}

				default:
					{
						throw new ArgumentOutOfRangeException("continueType");
					}
			}

			_ContinueType = continueType;
			_ContinueArgumentLocation = continueArgumentLocation;
		}
	}
}