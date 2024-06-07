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
/// A parse tree for a qualified name (e.g. 'foo.bar').
/// </summary>
	public sealed class QualifiedName : Name
	{

		private readonly Name _Qualifier;
		private readonly Location _DotLocation;
		private readonly SimpleName _Name;

		/// <summary>
    /// The qualifier on the left-hand side of the dot.
    /// </summary>
		public Name Qualifier
		{
			get
			{
				return _Qualifier;
			}
		}

		/// <summary>
    /// The location of the dot.
    /// </summary>
		public Location DotLocation
		{
			get
			{
				return _DotLocation;
			}
		}

		/// <summary>
    /// The name on the right-hand side of the dot.
    /// </summary>
		public SimpleName Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// Constructs a new parse tree for a qualified name.
    /// </summary>
    /// <param name="qualifier">The qualifier on the left-hand side of the dot.</param>
    /// <param name="dotLocation">The location of the dot.</param>
    /// <param name="name">The name on the right-hand side of the dot.</param>
    /// <param name="span">The location of the parse tree.</param>
		public QualifiedName(Name qualifier, Location dotLocation, SimpleName name, Span span) : base(TreeType.QualifiedName, span)
		{

			if (qualifier is null)
			{
				throw new ArgumentNullException("qualifier");
			}

			if (name is null)
			{
				throw new ArgumentNullException("name");
			}

			SetParent(qualifier);
			SetParent(name);

			_Qualifier = qualifier;
			_DotLocation = dotLocation;
			_Name = name;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Qualifier);
			AddChild(childList, Name);
		}
	}
}