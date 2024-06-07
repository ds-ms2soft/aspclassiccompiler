using System;
using System.Collections;
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
	/// A collection of a particular type of trees
	/// </summary>
	/// <typeparam name="T">The type of tree the collection contains.</typeparam>
	public abstract class TreeCollection<T> : Tree, IList<T> where T : Tree
	{

		private ReadOnlyCollection<T> _Trees;

		protected TreeCollection(TreeType @type, IList<T> trees, Span span) : base(type, span)
		{
			Debug.Assert(type >= TreeType.ArgumentCollection && type <= TreeType.DeclarationCollection);

			if (trees is null)
			{
				_Trees = new ReadOnlyCollection<T>(new List<T>());
			}
			else
			{
				_Trees = new ReadOnlyCollection<T>(trees);
				SetParents(trees);
			}
		}

		private void Add(T item)
		{
			throw new NotSupportedException();
		}

		void ICollection<T>.Add(T item) => Add(item);

		private void Clear()
		{
			throw new NotSupportedException();
		}

		void ICollection<T>.Clear() => Clear();

		public bool Contains(T item)
		{
			return _Trees.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_Trees.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				return _Trees.Count;
			}
		}

		private bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		bool ICollection<T>.IsReadOnly { get => IsReadOnly; }

		private bool Remove(T item)
		{
			throw new NotSupportedException();
		}

		bool ICollection<T>.Remove(T item) => Remove(item);

		public IEnumerator<T> GetEnumerator()
		{
			return _Trees.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return _Trees.IndexOf(item);
		}

		private void Insert(int index, T item)
		{
			throw new NotSupportedException();
		}

		void IList<T>.Insert(int index, T item) => Insert(index, item);

		public T get_Item(int index)
		{
			return _Trees[index];
		}

		private T get_IListItem(int index)
		{
			return _Trees[index];
		}

		private void set_IListItem(int index, T value)
		{
			throw new NotSupportedException();
		}

		T IList<T>.this[int index]
		{
			get => get_IListItem(index);
			set => set_IListItem(index, value);
		}
		
		private void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		

		void IList<T>.RemoveAt(int index) => RemoveAt(index);

		private IEnumerator IEnumerableGetEnumerator()
		{
			return _Trees.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => IEnumerableGetEnumerator();

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChildren(childList, _Trees);
		}
	}
}