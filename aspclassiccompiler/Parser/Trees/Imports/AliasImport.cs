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
/// A parse tree for an Imports statement that aliases a type or namespace.
/// </summary>
	public sealed class AliasImport : Import
	{

		private readonly SimpleName _Name;
		private readonly Location _EqualsLocation;
		private readonly TypeName _AliasedTypeName;

		/// <summary>
    /// The alias name.
    /// </summary>
		public SimpleName Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// The location of the '='.
    /// </summary>
		public Location EqualsLocation
		{
			get
			{
				return _EqualsLocation;
			}
		}

		/// <summary>
    /// The name being aliased.
    /// </summary>
		public TypeName AliasedTypeName
		{
			get
			{
				return _AliasedTypeName;
			}
		}

		/// <summary>
    /// Constructs a new aliased import parse tree.
    /// </summary>
    /// <param name="name">The name of the alias.</param>
    /// <param name="equalsLocation">The location of the '='.</param>
    /// <param name="aliasedTypeName">The name being aliased.</param>
    /// <param name="span">The location of the parse tree.</param>
		public AliasImport(SimpleName name, Location equalsLocation, TypeName aliasedTypeName, Span span) : base(TreeType.AliasImport, span)
		{

			if (aliasedTypeName is null)
			{
				throw new ArgumentNullException("aliasedTypeName");
			}

			if (name is null)
			{
				throw new ArgumentNullException("name");
			}

			SetParent(name);
			SetParent(aliasedTypeName);

			_Name = name;
			_EqualsLocation = equalsLocation;
			_AliasedTypeName = aliasedTypeName;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);

			AddChild(childList, AliasedTypeName);
		}
	}
}