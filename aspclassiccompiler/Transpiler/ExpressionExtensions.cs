using System;
using System.Linq;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public static class ExpressionExtensions
	{
		public static bool Matches<T>(this Expression target, Func<T, bool> test) where T : Expression
			=> target is T casted && test(casted);

		public static bool Matches(this CallStatement statement, string expression)
		{
			var parts = expression.Split('.');

			var rv = true;
			for (var i = 0; i < parts.Length && rv; i++)
			{
				var name = parts[i];
				var target = statement.TargetExpression.Children.ElementAtOrDefault(i);
				rv = rv && ((target is SimpleNameExpression sne && sne.Matches(name)) ||
				            (target is SimpleName sn && sn.Matches(name)));
			}

			return rv;
		}

		public static bool MatchesArgument(this CallStatement statement, int index, Func<Argument, bool> test)
		{
			var arg = statement.Arguments.ElementAtOrDefault(index);
			return arg != null && test(arg);
		}

		public static bool Matches(this SimpleNameExpression sne, string name)
			=> sne.Name.Matches(name);

		public static bool Matches(this SimpleName sn, string name)
			=> sn.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
	}
}
