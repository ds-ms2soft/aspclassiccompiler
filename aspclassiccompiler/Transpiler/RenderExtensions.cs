using System;
using System.Linq;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public static class RenderExtensions
	{
		public static string Render(this SimpleName sn, IdentifierScope scope, bool allowUndefined)
			=> scope.GetIdentifier(sn.Name, allowUndefined);

		public static string Render(this Expression exp, IdentifierScope scope, bool allowUndefinedVariables = false)
		{
			if (exp is StringLiteralExpression sl)
			{
				return $"\"{sl.Literal}\"";
			}
			else if (exp is DateLiteralExpression dt)
			{
				return "DateTime.Parse(\"" + dt.Value + "\")";
			}
			else if (exp is LiteralExpression lit)
			{
				return lit.Value.ToString();
			}
			else if (exp is SimpleNameExpression sne)
			{
				return sne.Name.Render(scope, allowUndefinedVariables);
			}
			else if (exp is CallOrIndexExpression cie)
			{
				return cie.TargetExpression.Render(scope) + "(" + cie.Arguments.Render(scope) + ")";
			}
			else if (exp is QualifiedExpression qe)
			{
				//This handles cases where we want to support a qualifier being removed (i.e. defined as null)
				return String.Join(".", new[]{qe.Qualifier.Render(scope), qe.Name.Name}.Where(name => !String.IsNullOrEmpty(name)));
			}
			else if (exp is BinaryOperatorExpression binary)
			{
				return
					$"{binary.LeftOperand.Render(scope)} {binary.Operator.Render()} {binary.RightOperand.Render(scope)}";
			}
			else if (exp is ParentheticalExpression paren)
			{
				if (paren.Children.Count != 1)
				{
					throw new NotImplementedException();
				}
				return $"({(paren.Children[0] as Expression).Render(scope)})";
			}
			else if (exp is UnaryOperatorExpression unary)
			{
				return unary.Operator.Render() + " " + unary.Operand.Render(scope);
			}
			else if (exp is NothingExpression nothing)
			{
				return "Nothing";
			}
			else if (exp is null)
			{
				if (scope is IdentifierScopeWithBlock)
				{
					return "";
				}
				else
				{
					throw new NotSupportedException("Null identifier found outside of With block.");
				}
			}
			else
			{
				throw new NotImplementedException($"Can't render: {exp.GetType().Name}");
			}
		}

		public static string Render(this ParameterCollection parameters, IdentifierScope innerScope)
		{
			return parameters != null ? String.Join(", ", parameters.Select(parm => parm.Render(innerScope))) : null;
		}

		public static string Render(this Parameter parm, IdentifierScope innerScope)
		{
			var rv = "ByRef "; //VBS is byref by default

			if (parm.Modifiers != null && parm.Modifiers.Count > 0)
			{
				if (parm.Modifiers.Count != 1)
				{
					throw new NotImplementedException("Haven't supported more than one parameter modifier.");
				}

				switch (parm.Modifiers.get_Item(0).ModifierType)
				{
					case ModifierTypes.ByRef:
					rv = "ByRef ";
					break;
					case ModifierTypes.ByVal:
						rv = "ByVal ";
						break;
					default:
						throw new NotImplementedException(
							"Unknown modifier: " + parm.Modifiers.get_Item(0).ModifierType);
				}
			}
			if (parm.VariableName.ArrayType != null 
			    || (parm.Attributes != null && parm.Attributes.Count > 0)
			    || (parm.Initializer != null))
			{
				throw new NotImplementedException();
			}

			var name = parm.VariableName.Name.Name;
			innerScope.Define(name);

			return rv + name;
		}

		public static string Render(this ArgumentCollection args, IdentifierScope scope)
		 => args != null ? String.Join(", ", args.Select(arg => arg.Render(scope))) : null;

		public static string Render(this Argument arg, IdentifierScope scope)
		{
			if (arg.Name != null)
			{
				throw new NotImplementedException("Named arguments not implemented");
			}
			return arg.Expression.Render(scope);
		}

		public static string Render(this OperatorType @operator)
		{
			switch (@operator)
			{
				case OperatorType.None:
					return null;
				case OperatorType.Plus:
				case OperatorType.UnaryPlus:
					return "+";
				case OperatorType.Concatenate:
					return "&";
				case OperatorType.Negate:
				case OperatorType.Minus:
					return "-";
				case OperatorType.Not:
					return "Not";
				case OperatorType.Multiply:
					return "*";
				case OperatorType.Divide:
				case OperatorType.IntegralDivide:
					return "/";
				case OperatorType.Or:
					return "Or";
				case OperatorType.OrElse:
					return "OrElse";
				case OperatorType.And:
					return "And";
				case OperatorType.AndAlso:
					return "AndAlso";
				case OperatorType.Equals:
					return "=";
				case OperatorType.NotEquals:
					return "<>";
				case OperatorType.LessThan:
					return "<";
				case OperatorType.LessThanEquals:
					return "<=";
				case OperatorType.GreaterThan:
					return ">";
				case OperatorType.GreaterThanEquals:
					return ">=";
				case OperatorType.Modulus:
					return "Mod";
				default:
					throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null);
			}
		}

		public static string Render(this CaseClauseCollection clauses, IdentifierScope scope)
		{
			return clauses != null ? String.Join(", ", clauses.Select(parm => parm.Render(scope))) : null;
		}

		public static string Render(this CaseClause clause, IdentifierScope scope)
		{
			//this is probably not complete
			if (clause is ComparisonCaseClause comp)
			{
				return comp.ComparisonOperator.Render() + " " + comp.Operand.Render(scope);
			}
			else if (clause is RangeCaseClause range)
			{
				return range.RangeExpression.Render(scope);
			}
			else
			{
				throw new NotImplementedException($"Haven't done render for {clause.GetType().Name}");
			}
			
		}
	}
}