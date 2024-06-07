using System.Collections.Generic;
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
/// A parse tree for a delegate declaration.
/// </summary>
	public abstract class DelegateDeclaration : SignatureDeclaration
	{

		private readonly Location _SubOrFunctionLocation;

		/// <summary>
    /// The location of 'Sub' or 'Function'.
    /// </summary>
		public Location SubOrFunctionLocation
		{
			get
			{
				return _SubOrFunctionLocation;
			}
		}

		protected DelegateDeclaration(TreeType @type, AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, Location subOrFunctionLocation, SimpleName name, TypeParameterCollection typeParameters, ParameterCollection parameters, Location asLocation, AttributeBlockCollection resultTypeAttributes, TypeName resultType, Span span, IList<CommentType> comments) : base(type, attributes, modifiers, keywordLocation, name, typeParameters, parameters, asLocation, resultTypeAttributes, resultType, span, comments)
		{

			Debug.Assert(type == TreeType.DelegateSubDeclaration || type == TreeType.DelegateFunctionDeclaration);

			_SubOrFunctionLocation = subOrFunctionLocation;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);
		}
	}
}