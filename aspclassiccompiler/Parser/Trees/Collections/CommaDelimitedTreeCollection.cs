using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// A collection of trees that are delimited by commas.
/// </summary>
	public abstract class CommaDelimitedTreeCollection<T> : TreeCollection<T> where T : Tree
	{

		private readonly ReadOnlyCollection<Location> _CommaLocations;

		/// <summary>
    /// The location of the commas in the list.
    /// </summary>
		public ReadOnlyCollection<Location> CommaLocations
		{
			get
			{
				return _CommaLocations;
			}
		}

		protected CommaDelimitedTreeCollection(TreeType @type, IList<T> trees, IList<Location> commaLocations, Span span) : base(type, trees, span)
		{

			Debug.Assert(type >= TreeType.ArgumentCollection && type <= TreeType.ImportCollection);

			if (commaLocations is not null && commaLocations.Count > 0)
			{
				_CommaLocations = new ReadOnlyCollection<Location>(commaLocations);
			}
		}
	}
}