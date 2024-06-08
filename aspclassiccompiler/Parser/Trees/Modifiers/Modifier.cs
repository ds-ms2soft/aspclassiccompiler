using System;

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
	/// A parse tree for a declaration modifier.
	/// </summary>
	public sealed class Modifier : Tree
	{

		private readonly ModifierTypes _ModifierType;

		/// <summary>
    /// The type of the modifier.
    /// </summary>
		public ModifierTypes ModifierType
		{
			get
			{
				return _ModifierType;
			}
		}

		/// <summary>
    /// Constructs a new modifier parse tree.
    /// </summary>
    /// <param name="modifierType">The type of the modifier.</param>
    /// <param name="span">The location of the parse tree.</param>
		public Modifier(ModifierTypes modifierType, Span span) : base(TreeType.Modifier, span)
		{

			if (((int)modifierType & (int)modifierType - 1) != 0 || modifierType < ModifierTypes.None || modifierType > ModifierTypes.Narrowing)

			{
				throw new ArgumentOutOfRangeException("modifierType");
			}

			_ModifierType = modifierType;
		}

		public override string ToString()
		{
			switch (ModifierType)
			{
				case ModifierTypes.Dim:
					return "Dim";
				case ModifierTypes.Const:
					return "Const";
				default:
					throw new ArgumentOutOfRangeException("Unimplemented modifier type");
			}
		}
	}
}