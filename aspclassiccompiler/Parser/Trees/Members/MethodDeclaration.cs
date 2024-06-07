﻿using System.Collections.Generic;
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
/// A parse tree for a Sub, Function or constructor declaration.
/// </summary>
	public abstract class MethodDeclaration : SignatureDeclaration
	{

		private readonly NameCollection _ImplementsList;
		private readonly NameCollection _HandlesList;
		private readonly StatementCollection _Statements;
		private readonly EndBlockDeclaration _EndDeclaration;

		/// <summary>
    /// The list of implemented members.
    /// </summary>
		public NameCollection ImplementsList
		{
			get
			{
				return _ImplementsList;
			}
		}

		/// <summary>
    /// The events that the declaration handles.
    /// </summary>
		public NameCollection HandlesList
		{
			get
			{
				return _HandlesList;
			}
		}

		/// <summary>
    /// The statements in the declaration.
    /// </summary>
		public StatementCollection Statements
		{
			get
			{
				return _Statements;
			}
		}

		/// <summary>
    /// The end block declaration, if any.
    /// </summary>
		public EndBlockDeclaration EndDeclaration
		{
			get
			{
				return _EndDeclaration;
			}
		}

		protected MethodDeclaration(TreeType @type, AttributeBlockCollection attributes, ModifierCollection modifiers, Location keywordLocation, SimpleName name, TypeParameterCollection typeParameters, ParameterCollection parameters, Location asLocation, AttributeBlockCollection resultTypeAttributes, TypeName resultType, NameCollection implementsList, NameCollection handlesList, StatementCollection statements, EndBlockDeclaration endDeclaration, Span span, IList<CommentType> comments) : base(type, attributes, modifiers, keywordLocation, name, typeParameters, parameters, asLocation, resultTypeAttributes, resultType, span, comments)
		{

			Debug.Assert(type == TreeType.SubDeclaration || type == TreeType.FunctionDeclaration || type == TreeType.ConstructorDeclaration || type == TreeType.OperatorDeclaration);


			SetParent(statements);
			SetParent(endDeclaration);
			SetParent(handlesList);
			SetParent(implementsList);

			_ImplementsList = implementsList;
			_HandlesList = handlesList;
			_Statements = statements;
			_EndDeclaration = endDeclaration;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			base.GetChildTrees(childList);
			AddChild(childList, ImplementsList);
			AddChild(childList, HandlesList);
			AddChild(childList, Statements);
			AddChild(childList, EndDeclaration);
		}
	}
}