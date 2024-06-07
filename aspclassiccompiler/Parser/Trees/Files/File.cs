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
/// A parse tree for an entire file.
/// </summary>
	public sealed class File : Tree
	{

		private readonly DeclarationCollection _Declarations;

		/// <summary>
    /// The declarations in the file.
    /// </summary>
		public DeclarationCollection Declarations
		{
			get
			{
				return _Declarations;
			}
		}

		/// <summary>
    /// Constructs a new file parse tree.
    /// </summary>
    /// <param name="declarations">The declarations in the file.</param>
    /// <param name="span">The location of the tree.</param>
		public File(DeclarationCollection declarations, Span span) : base(TreeType.File, span)
		{

			SetParent(declarations);

			_Declarations = declarations;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Declarations);
		}
	}
}