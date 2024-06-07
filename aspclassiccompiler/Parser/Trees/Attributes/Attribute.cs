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
/// A parse tree for an attribute usage.
/// </summary>
	public sealed class Attribute : Tree
	{

		private readonly AttributeTypes _AttributeType;
		private readonly Location _AttributeTypeLocation;
		private readonly Location _ColonLocation;
		private readonly Name _Name;
		private readonly ArgumentCollection _Arguments;

		/// <summary>
    /// The target type of the attribute.
    /// </summary>
		public AttributeTypes AttributeType
		{
			get
			{
				return _AttributeType;
			}
		}

		/// <summary>
    /// The location of the attribute type, if any.
    /// </summary>
		public Location AttributeTypeLocation
		{
			get
			{
				return _AttributeTypeLocation;
			}
		}

		/// <summary>
    /// The location of the ':', if any.
    /// </summary>
		public Location ColonLocation
		{
			get
			{
				return _ColonLocation;
			}
		}

		/// <summary>
    /// The name of the attribute being applied.
    /// </summary>
		public Name Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
    /// The arguments to the attribute.
    /// </summary>
		public ArgumentCollection Arguments
		{
			get
			{
				return _Arguments;
			}
		}

		/// <summary>
    /// Constructs a new attribute parse tree.
    /// </summary>
    /// <param name="attributeType">The target type of the attribute.</param>
    /// <param name="attributeTypeLocation">The location of the attribute type.</param>
    /// <param name="colonLocation">The location of the ':'.</param>
    /// <param name="name">The name of the attribute being applied.</param>
    /// <param name="arguments">The arguments to the attribute.</param>
    /// <param name="span">The location of the parse tree.</param>
		public Attribute(AttributeTypes attributeType, Location attributeTypeLocation, Location colonLocation, Name name, ArgumentCollection arguments, Span span) : base(TreeType.Attribute, span)
		{

			if (name is null)
			{
				throw new ArgumentNullException("name");
			}

			SetParent(name);
			SetParent(arguments);

			_AttributeType = attributeType;
			_AttributeTypeLocation = attributeTypeLocation;
			_ColonLocation = colonLocation;
			_Name = name;
			_Arguments = arguments;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, Name);
			AddChild(childList, Arguments);
		}
	}
}