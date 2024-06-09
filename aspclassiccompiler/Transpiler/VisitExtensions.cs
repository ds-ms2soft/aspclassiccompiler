using System;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public static class VisitExtensions
	{
		public static void VisitAll<T>(this Tree block, Action<T> visitor)
		{
			foreach (var child in block.Children)
			{
				if (child is T found)
				{
					visitor(found);
				}

				VisitAll(child, visitor);
			}
		}
	}
}
