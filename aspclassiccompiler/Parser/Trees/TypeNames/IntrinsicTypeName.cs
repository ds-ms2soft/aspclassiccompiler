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
/// A parse tree for an intrinsic type name.
/// </summary>
	public sealed class IntrinsicTypeName : TypeName
	{

		private IntrinsicType _IntrinsicType;

		/// <summary>
    /// The intrinsic type.
    /// </summary>
		public IntrinsicType IntrinsicType
		{
			get
			{
				return _IntrinsicType;
			}
		}

		/// <summary>
    /// Constructs a new intrinsic type parse tree.
    /// </summary>
    /// <param name="intrinsicType">The intrinsic type.</param>
    /// <param name="span">The location of the parse tree.</param>
		public IntrinsicTypeName(IntrinsicType intrinsicType, Span span) : base(TreeType.IntrinsicType, span)
		{

			if (intrinsicType < IntrinsicType.Boolean || intrinsicType > IntrinsicType.Object)
			{
				throw new ArgumentOutOfRangeException("intrinsicType");
			}

			_IntrinsicType = intrinsicType;
		}
	}
}