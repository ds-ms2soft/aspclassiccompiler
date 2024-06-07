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
/// A parse tree for an Exit statement.
/// </summary>
	public sealed class ExitStatement : Statement
	{

		private readonly BlockType _ExitType;
		private readonly Location _ExitArgumentLocation;

		/// <summary>
    /// The type of tree this statement exits.
    /// </summary>
		public BlockType ExitType
		{
			get
			{
				return _ExitType;
			}
		}

		/// <summary>
    /// The location of the exit statement type.
    /// </summary>
		public Location ExitArgumentLocation
		{
			get
			{
				return _ExitArgumentLocation;
			}
		}

		/// <summary>
    /// Constructs a parse tree for an Exit statement.
    /// </summary>
    /// <param name="exitType">The type of tree this statement exits.</param>
    /// <param name="exitArgumentLocation">The location of the exit statement type.</param>
    /// <param name="span">The location of the parse tree.</param>
    /// <param name="comments">The comments for the parse tree.</param>
		public ExitStatement(BlockType exitType, Location exitArgumentLocation, Span span, IList<CommentType> comments) : base(TreeType.ExitStatement, span, comments)
		{

			switch (exitType)
			{
				case BlockType.Do:
				case BlockType.For:
				case BlockType.While:
				case BlockType.Select:
				case BlockType.Sub:
				case BlockType.Function:
				// OK

				case BlockType.Property:
				case BlockType.Try:
				case BlockType.None:
					{
						break;
					}

				default:
					{
						throw new ArgumentOutOfRangeException("exitType");
					}
			}

			_ExitType = exitType;
			_ExitArgumentLocation = exitArgumentLocation;
		}
	}
}