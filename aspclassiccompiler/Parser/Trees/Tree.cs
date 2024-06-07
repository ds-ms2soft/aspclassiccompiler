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
/// The root class of all trees.
/// </summary>
	public abstract class Tree
	{
		private readonly TreeType _Type;
		private readonly Span _Span;
		private Tree _Parent;
		private ReadOnlyCollection<Tree> _Children;

		/// <summary>
    /// The type of the tree.
    /// </summary>
		public TreeType Type
		{
			get
			{
				return _Type;
			}
		}

		/// <summary>
    /// The location of the tree.
    /// </summary>
    /// <remarks>
    /// The span ends at the first character beyond the tree
    /// </remarks>
		public Span Span
		{
			get
			{
				return _Span;
			}
		}

		/// <summary>
    /// The parent of the tree. Nothing if the root tree.
    /// </summary>
		public Tree Parent
		{
			get
			{
				return _Parent;
			}
		}

		/// <summary>
    /// The children of the tree.
    /// </summary>
		public ReadOnlyCollection<Tree> Children
		{
			get
			{
				if (_Children is null)
				{
					var ChildList = new List<Tree>();

					GetChildTrees(ChildList);
					_Children = new ReadOnlyCollection<Tree>(ChildList);
				}

				return _Children;
			}
		}

		/// <summary>
    /// Whether the tree is 'bad'.
    /// </summary>
		public virtual bool IsBad
		{
			get
			{
				return false;
			}
		}

		protected Tree(TreeType @type, Span span)
		{
			Debug.Assert(type >= TreeType.SyntaxError && type <= TreeType.File);
			_Type = type;
			_Span = span;
		}

		protected void SetParent(Tree child)
		{
			if (child is not null)
			{
				child._Parent = this;
			}
		}

		protected void SetParents<T>(IList<T> children) where T : Tree
		{
			if (children is not null)
			{
				foreach (Tree Child in children)
					SetParent(Child);
			}
		}

		protected static void AddChild(IList<Tree> childList, Tree child)
		{
			if (child is not null)
			{
				childList.Add(child);
			}
		}

		protected static void AddChildren<T>(IList<Tree> childList, ReadOnlyCollection<T> children) where T : Tree
		{
			if (children is not null)
			{
				foreach (Tree Child in children)
					childList.Add(Child);
			}
		}

		protected virtual void GetChildTrees(IList<Tree> childList)
		{
			// By default, trees have no children
		}
	}
}