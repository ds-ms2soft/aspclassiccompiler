using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dlrsoft.Asp;
using VB = Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public class MvcGenerator
	{
		private VB.ScriptBlock _script;

		protected OutputWriter Output;

		private IReadOnlyList<string> _literals;

		public IdentifierScope Transpile(VB.ScriptBlock script, OutputWriter output, IReadOnlyList<string> literals, Action<IdentifierScope> defineExtraIdentifiers = null)
		{
			_script = script;
			Output = output;
			_literals = literals;
			var globalScope = IdentifierScope.MakeGlobal();
			AddToGlobalScope(globalScope);
			var localScope = new IdentifierScope(globalScope);
			defineExtraIdentifiers?.Invoke(localScope);
			AddSubAndMethodDeclarationsToScope(_script.Statements, localScope);
			Process(_script.Statements, localScope, false);
			return localScope;
		}

		protected virtual void AddToGlobalScope(IdentifierScope scope)
		{
		}

		private void AddSubAndMethodDeclarationsToScope(VB.StatementCollection statements, IdentifierScope scope)
		{
			foreach (VB.Statement statement in statements)
			{
				if (statement is VB.MethodDeclaration md) //SubDeclaration is a derived class, so this handles both.
				{
					scope.Define(md.Name.Name);
				}
			}
		}
		private void Process(VB.StatementCollection statements, IdentifierScope scope, bool beginBlock)
		{
			var block = beginBlock ? Output.BeginBlock() : null;
			try
			{
				foreach (VB.Statement statement in statements ?? Array.Empty<VB.Statement>().AsEnumerable())
				{
					Process(statement, scope);
				}
			}
			finally
			{
				block?.Dispose();
			}
		}

		private void Process(VB.Statement expr, IdentifierScope scope)
		{
			//if (expr is VB.ImportsDeclaration)
			//{
			//	return GenerateImportExpr((VB.ImportsDeclaration)expr, scope);
			//}
			//else if (expr is VB.NameImport)
			//{
			//	return GenerateImportExpr((VB.NameImport)expr, scope);
			//}
			//else if (expr is VB.AliasImport)
			//{
			//	return GenerateImportExpr((VB.AliasImport)expr, scope);
			//}
			if (expr is VB.CallStatement statement)
			{
				ProcessCall(statement, scope);
			}

			else if (expr is VB.LocalDeclarationStatement declaration)
			{
				ProcessDeclaration(declaration, scope);
			}
			else if (expr is VB.MethodDeclaration functionDeclaration)
			{
				GenerateMethodExpr(functionDeclaration, scope);
			}
			////else if (expr is SymplLambdaExpr)
			////{
			////    return GenerateLambdaExpr((SymplLambdaExpr)expr, scope);
			////}
			//else if (expr is VB.CallOrIndexExpression)
			//{
			//	return GenerateCallOrIndexExpr((VB.CallOrIndexExpression)expr, scope);
			//}
			//else if (expr is VB.SimpleNameExpression)
			//{
			//	return GenerateIdExpr((VB.SimpleNameExpression)expr, scope);
			//}
			////else if (expr is SymplQuoteExpr)
			////{
			////    return GenerateQuoteExpr((SymplQuoteExpr)expr, scope);
			////}
			//else if (expr is VB.NothingExpression)
			//{
			//	return Expression.Constant(null);
			//}
			//else if (expr is VB.LiteralExpression)
			//{
			//	return Expression.Constant(((VB.LiteralExpression)expr).Value);
			//}
			else if (expr is VB.AssignmentStatement assign)
			{
				GenerateAssignExpr(assign, scope);
			}
			////else if (expr is SymplEqExpr)
			////{
			////    return GenerateEqExpr((SymplEqExpr)expr, scope);
			////}
			else if (expr is VB.IfBlockStatement @if)
			{
				GenerateIfExpr(@if, scope);
			}
			//else if (expr is VB.LineIfStatement)
			//{
			//	return GenerateIfExpr((VB.LineIfStatement)expr, scope);
			//}
			//else if (expr is VB.QualifiedExpression)
			//{
			//	return GenerateDottedExpr((VB.QualifiedExpression)expr, scope);
			//}
			//else if (expr is VB.NewExpression)
			//{
			//	return GenerateNewExpr((VB.NewExpression)expr, scope);
			//}
			else if (expr is VB.ForBlockStatement @for)
			{
				GenerateForBlockExpr(@for, scope);
			}
			else if (expr is VB.ForEachBlockStatement forEach)
			{
				GenerateForEachBlockExpr(forEach, scope);
			}
			else if (expr is VB.WhileBlockStatement @while)
			{
				GenerateWhileBlockExpr(@while, scope);
			}
			else if (expr is VB.DoBlockStatement @do)
			{
				GenerateDoBlockExpr(@do, scope);
			}
			else if (expr is VB.WithBlockStatement with)
			{
				GenerateWithBlockExpr(with, scope);
			}
			else if (expr is VB.ExitStatement exit)
			{
				GenerateBreakExpr(exit, scope);
			}
			////else if (expr is SymplEltExpr)
			////{
			////    return GenerateEltExpr((SymplEltExpr)expr, scope);
			////}
			//else if (expr is VB.BinaryOperatorExpression)
			//{
			//	return GenerateBinaryExpr((VB.BinaryOperatorExpression)expr, scope);
			//}
			//else if (expr is VB.UnaryOperatorExpression)
			//{
			//	return GenerateUnaryExpr((VB.UnaryOperatorExpression)expr, scope);
			//}
			else if (expr is VB.SelectBlockStatement blockStatement)
			{
				GenerateSelectBlockExpr(blockStatement, scope);
			}
			//else if (expr is VB.BlockStatement)
			//{
			//	return GenerateBlockExpr(((VB.BlockStatement)expr).Statements, scope);
			//}
			else if (expr is VB.EmptyStatement)
			{
				Output.WriteCode(Environment.NewLine + Environment.NewLine, false);
			}
			else if (expr is VB.OptionDeclaration)
			{
				//scope.ModuleScope.IsOptionExplicitOn = true;
				//return Expression.Empty();
				//TODO: just ignore this?
			}
			//else if (expr is VB.ParentheticalExpression)
			//{
			//	return GenerateExpr(((VB.ParentheticalExpression)expr).Operand, scope);
			//}
			else if (expr is VB.ReDimStatement redimStatement)
			{
				GenerateRedimExpr(redimStatement, scope);
			}
			else if (expr is VB.OnErrorStatement errorStatement)
			{
				GenerateOnErrorStatement(errorStatement);
			}
			//else if (expr is ExpressionExpression)
			//{
			//	return ((ExpressionExpression)expr).Expression;
			//}
			else
			{
				throw new NotImplementedException($"{expr.GetType().FullName} is not yet implemented.");
				//VBScriptSyntaxError error = new VBScriptSyntaxError(
				//	scope.ModuleScope.Name,
				//	SourceUtil.ConvertSpan(expr.Span),
				//	(int)VB.SyntaxErrorType.NotImplemented,
				//	string.Format("{0} is not yet implemented.", expr.GetType().FullName)
				//);

				//scope.ModuleScope.Errors.Add(error);
				//return Expression.Default(typeof(object));
			}
		}

		public Action<string, OutputWriter, IdentifierScope> HandleServerSideInclude { get; set; } = (s, writer, arg3) =>
			throw new NotImplementedException($"Provide HandleServerSideInclude (include: {s})");

		public bool IsIncludeFile { get; set; }

		private void ProcessCall(VB.CallStatement statement, IdentifierScope scope)
		{
			if (statement.Matches("Response.Write") || 
			    (statement.Matches("Write") && scope is IdentifierScopeWithBlock with && with.Source.Equals("Response", StringComparison.OrdinalIgnoreCase)))
			{
				if (statement.MatchesArgument(0, test =>
				    {
					    return test.Expression.Matches<VB.CallOrIndexExpression>(
						    cie => cie.TargetExpression.Matches<VB.SimpleNameExpression>(sne =>
							    sne.Matches("literals")));
				    }))
				{
					var arg0 = (VB.CallOrIndexExpression)statement.Arguments.ElementAt(0).Expression;
					var index = ((VB.IntegerLiteralExpression)arg0.Arguments.ElementAt(0).Expression).Literal;
					Output.WriteLiteral(_literals[index]);
				}
				else
				{
					//translate this to Html.Raw instead.
					Output.WriteCode($"@Html.Raw({statement.Arguments.Render(scope)})", true);
				}
			}
			else if (statement.Matches(AspPageDom.ServerSideInclude))
			{
				var path = Path.GetFullPath(((VB.StringLiteralExpression)statement.Arguments.First().Expression).Literal);
				HandleServerSideInclude(path, Output, scope);
			}
			else
			{
				Output.WriteCode($"{statement.TargetExpression.Render(scope)}({statement.Arguments.Render(scope)})", true);
			}
		}

		public void ProcessDeclaration(VB.LocalDeclarationStatement stmt, IdentifierScope scope)
		{
			if (stmt.Modifiers.Count != 1)
			{
				throw new NotImplementedException("Unexpected modifiers");
			}

			if (stmt.VariableDeclarators.Count != 1)
			{
				throw new NotImplementedException("Unexpected declarators");
			}

			var declare = stmt.VariableDeclarators.ElementAt(0);
			string line = "";
			
			bool isFirstVariable = true;
			foreach (VB.VariableName v in declare.VariableNames)
			{
				var originalName = v.Name.Name;
				var name = OverrideVariableDeclaration(originalName, scope);
				if (name != null)
				{
					if (!isFirstVariable)
					{
						line += ", ";
					}
					else
					{
						isFirstVariable = false;
					}

					scope.Define(v.Name.Name);
					if (originalName != name)
					{
						scope.Define(originalName, name);
					}
					line += name;

					if (declare.Initializer is VB.ExpressionInitializer ei)
					{
						line += " = " + ei.Expression.Render(scope);
					}

					if (v.ArrayType != null)
					{
						line += $"({v.ArrayType.Arguments.Render(scope)})";
					}
				}
			}

			if (line.Length > 0)
			{
				line = stmt.Modifiers.ElementAt(0) + " " + line;
				Output.WriteCode(line, true);
			}
		}

		protected virtual string OverrideVariableDeclaration(string name, IdentifierScope scope) => name;

		protected virtual void GenerateAssignExpr(VB.AssignmentStatement expr, IdentifierScope scope)
		{
			using var newVariables = scope.WithVariableDefinitionHandling();
			
			var variable =
				expr.TargetExpression.Render(scope,
					IdentifierScope.UndefinedHandling.AllowAndDefine); //Define because we are an assignment
			var value = expr.SourceExpression.Render(scope);
			var isNewVariable = newVariables.WasDefined(variable);

			GenerateAssignExpr(isNewVariable, variable, value, scope);
		}

		protected virtual void GenerateAssignExpr(bool isUndefined, string variable, string value, IdentifierScope scope)
		{
			Output.WriteCode((isUndefined ? "Dim " : "") + variable + " = " + value, true);
		}

		private void GenerateForBlockExpr(VB.ForBlockStatement forBlock,
			IdentifierScope scope)
		{
			if (forBlock.StepExpression != null || forBlock.ControlVariableDeclarator != null)
			{
				throw new NotImplementedException();
			}
			
			var innerScope = new IdentifierScope(scope);
			Output.WriteCode($"For {forBlock.ControlExpression.Render(innerScope, IdentifierScope.UndefinedHandling.AllowAndDefine)} = {forBlock.LowerBoundExpression.Render(innerScope)} To {forBlock.UpperBoundExpression.Render(innerScope)}", true);

			Process(forBlock.Statements, innerScope, true);

			Output.WriteCode("Next", true);
		}

		private void GenerateDoBlockExpr(VB.DoBlockStatement doBlock,
			IdentifierScope scope)
		{
			void outputBody()
			{
				Process(doBlock.Statements, scope, true);
			}

			if (doBlock.Expression != null)
			{
				if (doBlock.IsWhile) //do while ... loop
				{
					Output.WriteCode($"Do while {doBlock.Expression.Render(scope)}", true);
					outputBody();
					Output.WriteCode("Loop", true);
				}
				else // do until ... loop
				{
					Output.WriteCode("Do", true);
					outputBody();
					Output.WriteCode($"Loop Until {doBlock.Expression.Render(scope)}", true);
				}
			}
			else
			{
				throw new NotImplementedException("Unimplemented loop, need an example");
				/*
				if (doBlock.EndStatement.IsWhile) //do ... loop while
				{
					_output.WriteCode("Do", true);
					outputBody();
					_output.WriteCode($"Loop While {doBlock.Expression.Render(scope)}", true);
				}
				else // do until ... loop
				{
					_output.WriteCode("Do", true);
					outputBody();
					_output.WriteCode($"Until {doBlock.Expression.Render(scope)}", true);
				}
				*/
			}
		}

		public void GenerateWhileBlockExpr(VB.WhileBlockStatement whileBlock,
			IdentifierScope scope)
		{
			Output.WriteCode($"While {whileBlock.Expression.Render(scope)}", true);
			Process(whileBlock.Statements, scope, true);
			Output.WriteCode("End While", true);
		}

		public void GenerateWithBlockExpr(VB.WithBlockStatement withBlock,
			IdentifierScope scope)
		{
			var source = withBlock.Expression.Render(scope);
			Output.WriteCode($"With {source}", true);
			var innerScope = new IdentifierScopeWithBlock(source, scope);
			Process(withBlock.Statements, innerScope, true);
			Output.WriteCode("End With", true);
		}

		private void GenerateMethodExpr(VB.MethodDeclaration method, IdentifierScope scope)
		{
			string name = method.Name.Name;
			//Don't need this, because we need to be added to scope before we are generated: scope.Define(name);
			if (method.ResultType != null)
			{
				throw new NotImplementedException();
			}

			string keyword;
			if (method is VB.FunctionDeclaration func)
			{
				keyword = "Function";
			}
			else if (method is VB.SubDeclaration sub)
			{
				keyword = "Sub";
			}
			else
			{
				throw new NotImplementedException();
			}

			var methodScope = new IdentifierScope(scope);
			Output.WriteCode($"{keyword} {name}({method.Parameters.Render(methodScope)})", true);
			Process(method.Statements, methodScope, true);
			Output.WriteCode($"End {keyword}", true);
		}

		private void GenerateIfExpr(VB.IfBlockStatement ifBlock, IdentifierScope scope)
		{
			void writeIf(string type, VB.Expression testExpression, VB.StatementCollection body)
			{
				Output.WriteCode($"{type} {testExpression.Render(scope)} Then", true);
				Process(body, scope, true);
			}

			writeIf("If", ifBlock.Expression, ifBlock.Statements);

			if (ifBlock.ElseIfBlockStatements != null)
			{
				foreach (VB.ElseIfBlockStatement elseIf in ifBlock.ElseIfBlockStatements)
				{
					writeIf("ElseIf", elseIf.ElseIfStatement.Expression, elseIf.Statements);
				}
			}

			if (ifBlock.ElseBlockStatement != null)
			{
				Output.WriteCode("Else", true);
				Process(ifBlock.ElseBlockStatement.Statements, scope, true);
			}
			Output.WriteCode($"End If", true);
		}

		private void GenerateOnErrorStatement(VB.OnErrorStatement onError)
		{
			switch (onError.OnErrorType)
			{
				case VB.OnErrorType.Next:
					Output.WriteCode("On Error Resume Next", true);
					break;
				case VB.OnErrorType.Zero:
				case VB.OnErrorType.MinusOne:
					Output.WriteCode("On Error Goto 0", true);
					break;
				default:
					throw new ArgumentOutOfRangeException($"Unimplemented error type: {onError.OnErrorType}");
			}
		}

		private void GenerateRedimExpr(VB.ReDimStatement redim, IdentifierScope scope)
		{
			var code = "ReDim ";
			if (redim.IsPreserve)
			{
				code += "Preserve ";
			}

			bool isFirst = true;
			foreach (VB.CallOrIndexExpression variable in redim.Variables)
			{
				if (!isFirst)
				{
					code += ", ";
				}

				isFirst = false;

				code += variable.TargetExpression.Render(scope) + $"({variable.Arguments.Render(scope)})";
			}

			Output.WriteCode(code, true);
		}

		private void GenerateSelectBlockExpr(VB.SelectBlockStatement selectBlock,
			IdentifierScope scope)
		{
			Output.WriteCode("Select Case " + selectBlock.Expression.Render(scope), true);
			using (var _ = Output.BeginBlock())
			{
				if (selectBlock.CaseBlockStatements != null)
				{
					foreach (VB.CaseBlockStatement @case in selectBlock.CaseBlockStatements)
					{
						Output.WriteCode("Case " + @case.CaseStatement.CaseClauses.Render(scope), true);
						Process(@case.Statements, scope, true);
					}
				}
				if (selectBlock.CaseElseBlockStatement != null)
				{
					Output.WriteCode("Case Else", true);
					Process(selectBlock.CaseElseBlockStatement.Statements, scope, true);
				}
			}
			Output.WriteCode("End Select", true);
		}

		private void GenerateBreakExpr(VB.ExitStatement expr,
			IdentifierScope scope)
		{
			switch (expr.ExitType)
			{
				case Dlrsoft.VBScript.Parser.BlockType.Function:
					Output.WriteCode("Exit Function", true);
					break;
				case Dlrsoft.VBScript.Parser.BlockType.Sub:
					Output.WriteCode("Exit Sub", true);
					break;
				case Dlrsoft.VBScript.Parser.BlockType.Do:
				case Dlrsoft.VBScript.Parser.BlockType.For:
				//	_output.WriteCode("Break", true);
//					break;
				default:
					throw new NotImplementedException();
			}
		}

		public void GenerateForEachBlockExpr(VB.ForEachBlockStatement forBlock,
			IdentifierScope scope)
		{
			var innerScope = new IdentifierScope(scope);
			Output.WriteCode($"For Each {forBlock.ControlExpression.Render(innerScope, IdentifierScope.UndefinedHandling.AllowAndDefine)} In {forBlock.CollectionExpression.Render(innerScope)}", true);

			Process(forBlock.Statements, innerScope, true);

			Output.WriteCode("Next", true);
		}
	}
	/*
	public static Expression GenerateImportExpr(VB.ImportsDeclaration importDesc, AnalysisScope scope)
	{
		if (!scope.IsModule)
		{
			throw new InvalidOperationException(
				"Import expression must be a top level expression.");
		}
		List<Expression> exprs = new List<Expression>();
		foreach (VB.Import statement in importDesc.ImportMembers)
		{
			Expression expr = GenerateExpr(statement, scope);
			exprs.Add(expr);
		}
		return Expression.Block(exprs);
	}

	public static Expression GenerateImportExpr(VB.NameImport import,
												AnalysisScope scope)
	{
		if (!scope.IsModule)
		{
			throw new InvalidOperationException(
				"Import expression must be a top level expression.");
		}
		return Expression.Call(
			typeof(RuntimeHelpers).GetMethod("VBScriptImport"),
			scope.RuntimeExpr,
			scope.ModuleExpr,
			Expression.Constant(new string[] {
				((VB.SimpleName)((VB.NamedTypeName)import.TypeName).Name).Name
			}),
			Expression.Constant(new string[]{}),
			Expression.Constant(new string[]{}));
	}

	public static Expression GenerateImportExpr(VB.AliasImport import,
												 AnalysisScope scope)
	{
		if (!scope.IsModule)
		{
			throw new InvalidOperationException(
				"Import expression must be a top level expression.");
		}

		string alias = ((VB.SimpleName)import.Name).Name;
		VB.NamedTypeName namedTypeName = (VB.NamedTypeName)import.AliasedTypeName;
		List<string> names = new List<string>();
		string[] typeNames;
		if (namedTypeName.Name is VB.QualifiedName)
		{
			VB.QualifiedName qualifiedName = (VB.QualifiedName)namedTypeName.Name;
			typeNames = new string[] {qualifiedName.Name.Name};

			VB.Name name = qualifiedName.Qualifier;
			while (name is VB.QualifiedName)
			{
				names.Insert(0, ((VB.QualifiedName)name).Name.Name);
				name = ((VB.QualifiedName)name).Qualifier;
			}
			names.Insert(0, ((VB.SimpleName)name).Name);
		}
		else
		{
			typeNames = new string[] { };
			names.Insert(0, ((VB.SimpleName)namedTypeName.Name).Name);
		}

		return Expression.Call(
			typeof(RuntimeHelpers).GetMethod("VBScriptImport"),
			scope.RuntimeExpr,
			scope.ModuleExpr,
			Expression.Constant(names.ToArray()),
			Expression.Constant(typeNames),
			Expression.Constant(new string[] {alias})
		);
	}

	// Returns a dynamic InvokeMember or Invoke expression, depending on the
	// Function expression.
	//
	public static Expression GenerateCallStmtExpr(
			VB.CallStatement expr, AnalysisScope scope)
	{
		return GenerateCallOrIndexExpr(
			new VB.CallOrIndexExpression(
				expr.TargetExpression,
				expr.Arguments,
				expr.Span
			),
			scope
		);
	}

	// Returns a chain of GetMember and InvokeMember dynamic expressions for
	// the dotted expr.
	//
	public static Expression GenerateDottedExpr(VB.QualifiedExpression expr,
												AnalysisScope scope)
	{
		Expression curExpr = null;
		if (expr.Qualifier != null)
		{
			curExpr = GenerateExpr(expr.Qualifier, scope);
		}
		else
		{
			curExpr = scope.NearestWithExpression;
		}

		curExpr = Expression.Dynamic(
			scope.GetRuntime()
				 .GetGetMemberBinder(expr.Name.Name),
			typeof(object),
			curExpr
		);
		//    }
		//    else if (e is SymplFunCallExpr)
		//    {
		//        var call = (SymplFunCallExpr)e;
		//        List<Expression> args = new List<Expression>();
		//        args.Add(curExpr);
		//        args.AddRange(call.Arguments.Select(a => GenerateExpr(a, scope)));

		//        curExpr = Expression.Dynamic(
		//            // Dotted exprs must be simple invoke members, a.b.(c ...)
		//            scope.GetRuntime().GetInvokeMemberBinder(
		//                new InvokeMemberBinderKey(
		//                    ((SymplIdExpr)call.Function).IdToken.Name,
		//                    new CallInfo(call.Arguments.Length))),
		//            typeof(object),
		//            args
		//        );
		//    }
		//    else
		//    {
		//        throw new InvalidOperationException(
		//            "Internal: dotted must be IDs or Funs.");
		//    }
		//}
		return curExpr;
	}

	public static Expression GenerateQualifiedNameExpr(VB.QualifiedName expr,
												AnalysisScope scope)
	{
		Expression curExpr;
		if (expr.Qualifier is VB.SimpleName)
			curExpr = GenerateSimpleNameExpr((VB.SimpleName)expr.Qualifier, scope);
		else
			curExpr = GenerateQualifiedNameExpr((VB.QualifiedName)expr.Qualifier, scope);

		return Expression.Dynamic(
			scope.GetRuntime()
				 .GetGetMemberBinder(expr.Name.Name),
			typeof(object),
			curExpr
		);
	}

	public static Expression GenerateCallOrIndexExpr(VB.CallOrIndexExpression expr, AnalysisScope scope)
	{
		List<Expression> args = GenerateArgumentList(expr.Arguments, scope);
		if (expr.TargetExpression is VB.SimpleNameExpression)
		{
			string name = ((VB.SimpleNameExpression)expr.TargetExpression).Name.Name;
			//Call the built-in function it is one
			if (IsBuiltInFunction(name))
			{
				int argCount = expr.Arguments == null? 0 : expr.Arguments.Count;
				args.Insert(0, Expression.Constant(new TypeModel(typeof(BuiltInFunctions))));
				return Expression.Dynamic(
						scope.GetRuntime().GetInvokeMemberBinder(
							new InvokeMemberBinderKey(
								name,
								new CallInfo(argCount))
						),
						typeof(object),
						args
					);
			}
			else if (scope.FunctionTable.Contains(name))
			{
				var fun = GenerateSimpleNameExpr(((VB.SimpleNameExpression)expr.TargetExpression).Name, scope);
				List<Type> argTypes = new List<Type>();
				foreach (Expression arg in args)
				{
					argTypes.Add(arg.Type.MakeByRefType());
				}
				argTypes.Insert(0, typeof(object)); //delegate itself
				argTypes.Add(typeof(object)); //return type

				args.Insert(0, fun);
				// Use DynExpr so that I don't always have to have a delegate to call,
				// such as what happens with IPy interop.
				int argCount = expr.Arguments == null ? 0 : expr.Arguments.Count;

				return Expression.MakeDynamic(
					Microsoft.Scripting.Actions.DynamicSiteHelpers.MakeCallSiteDelegate(argTypes.ToArray()),
					scope.GetRuntime().GetInvokeBinder(new CallInfo(argCount)),
					args
					);
			}
			else
			{
				////If not a function, it must be an array
				args.Insert(0, GenerateSimpleNameExpr(((VB.SimpleNameExpression)expr.TargetExpression).Name, scope));
				return Expression.Dynamic(
					scope.GetRuntime().GetGetIndexBinder(
						new CallInfo(args.Count)
					),
					typeof(object),
					args
				);
			}
		}
		else if (expr.TargetExpression is VB.CallOrIndexExpression)
		{
			Expression objExpr = GenerateCallOrIndexExpr((VB.CallOrIndexExpression)expr.TargetExpression, scope);
			////What followed must be an array as VBScript does not have function return a delegate
			args.Insert(0, objExpr);
			return Expression.Dynamic(
				scope.GetRuntime().GetGetIndexBinder(
					new CallInfo(args.Count)
				),
				typeof(object),
				args
			);
		}
		else //Qualified Expression
		{
			VB.QualifiedExpression qualifiedExpr = (VB.QualifiedExpression)expr.TargetExpression;
			Expression objExpr;
			if (qualifiedExpr.Qualifier is VB.QualifiedExpression)
			{
				objExpr = GenerateDottedExpr(
					(VB.QualifiedExpression)qualifiedExpr.Qualifier,
					scope
				);
			}
			else if (qualifiedExpr.Qualifier is VB.SimpleNameExpression)
			{
				objExpr = GenerateSimpleNameExpr(((VB.SimpleNameExpression)qualifiedExpr.Qualifier).Name, scope);
			}
			else if (qualifiedExpr.Qualifier is VB.CallOrIndexExpression)
			{
				objExpr = GenerateCallOrIndexExpr((VB.CallOrIndexExpression)qualifiedExpr.Qualifier, scope);
			}
			else //null
			{
				objExpr = scope.NearestWithExpression;
				if (objExpr == null)
					throw new Exception("Missing With statement");
			}

			//LC 12/16/2009 Try the byref type
			List<Type> argTypes = new List<Type>();
			foreach (Expression arg in args)
			{
				argTypes.Add(arg.Type.MakeByRefType());
			}
			argTypes.Insert(0, typeof(object)); //target itself
			argTypes.Add(typeof(object)); //return type

			args.Insert(0, objExpr);

			// last expr must be an id
			var lastExpr = qualifiedExpr.Name;
			int argCount = 0;
			if (expr.Arguments != null)
				argCount = expr.Arguments.Count;

			return Expression.MakeDynamic(
				Microsoft.Scripting.Actions.DynamicSiteHelpers.MakeCallSiteDelegate(argTypes.ToArray()),
				scope.GetRuntime().GetInvokeMemberBinder(
					new InvokeMemberBinderKey(
						lastExpr.Name,
						new CallInfo(argCount))),
				args
			);

			//return Expression.Dynamic(
			//    scope.GetRuntime().GetInvokeMemberBinder(
			//        new InvokeMemberBinderKey(
			//            lastExpr.Name,
			//            new CallInfo(argCount))),
			//    typeof(object),
			//    args
			//);
		}
	}

	// Return an Expression for referencing the ID.  If we find the name in the
	// scope chain, then we just return the stored ParamExpr.  Otherwise, the
	// reference is a dynamic member lookup on the root scope, a module object.
	//
	public static Expression GenerateIdExpr(VB.SimpleNameExpression expr,
											AnalysisScope scope)
	{
		string name = expr.Name.Name;

		if (IsBuiltInFunction(name) || scope.FunctionTable.Contains(name))
		{
			return GenerateCallOrIndexExpr(
				new VB.CallOrIndexExpression(
					expr,
					null,
					expr.Span),
				scope
			);
		}
		else
		{
			return GenerateSimpleNameExpr(expr.Name, scope);
		}
	}

	// GenerateLetStar returns a Block with vars, each initialized in the order
	// they appear.  Each var's init expr can refer to vars initialized before it.
	// The Block's body is the Let*'s body.
	//
	//public static Expression GenerateLetStarExpr(SymplLetStarExpr expr,
	//                                              AnalysisScope scope)
	//{
	//    var letscope = new AnalysisScope(scope, "let*");
	//    // Generate bindings.
	//    List<Expression> inits = new List<Expression>();
	//    List<ParameterExpression> varsInOrder = new List<ParameterExpression>();
	//    foreach (var b in expr.Bindings)
	//    {
	//        // Need richer logic for mvbind
	//        var v = Expression.Parameter(typeof(object), b.Variable.Name);
	//        varsInOrder.Add(v);
	//        inits.Add(
	//            Expression.Assign(
	//                v,
	//                Expression.Convert(GenerateExpr(b.Value, letscope), v.Type))
	//        );
	//        // Add var to scope after analyzing init value so that init value
	//        // references to the same ID do not bind to his uninitialized var.
	//        letscope.Names[b.Variable.Name.ToLower()] = v;
	//    }
	//    List<Expression> body = new List<Expression>();
	//    foreach (var e in expr.Body)
	//    {
	//        body.Add(GenerateExpr(e, letscope));
	//    }
	//    // Order of vars to BlockExpr don't matter semantically, but may as well
	//    // keep them in the order the programmer specified in case they look at the
	//    // Expr Trees in the debugger or for meta-programming.
	//    inits.AddRange(body);
	//    return Expression.Block(typeof(object), varsInOrder.ToArray(), inits);
	//}

	// GenerateBlockExpr returns a Block with the body exprs.
	//
	public static Expression GenerateBlockExpr(VB.StatementCollection stmts,
												AnalysisScope scope)
	{
		List<Expression> body = new List<Expression>();
		if (stmts != null)
		{
			foreach (VB.Statement s in stmts)
			{
				Expression stmt = MvcGenerator.GenerateExpr(s, scope);
				if (scope.VariableScope.IsOnErrorResumeNextOn)
				{
					stmt = WrapTryCatchExpression(stmt, scope);
				}
				Expression debugInfo = null;
				Expression clearDebugInfo = null;
				if (scope.GetRuntime().Trace && s is VB.Statement && !(s is VB.BlockStatement))
				{
					debugInfo = MvcGenerator.GenerateDebugInfo(s, scope, out clearDebugInfo);
					body.Add(debugInfo);
				}

				body.Add(stmt);

				if (clearDebugInfo != null)
				{
					body.Add(clearDebugInfo);
				}
			}
			//return Expression.Block(typeof(object), body);
			return Expression.Block(typeof(void), body);
		}
		else
		{
			//return Expression.Constant(null);
			return Expression.Empty();
		}
	}

	// GenerateQuoteExpr converts a list, literal, or id expr to a runtime quoted
	// literal and returns the Constant expression for it.
	//
	//public static Expression GenerateQuoteExpr(SymplQuoteExpr expr,
	//                                            AnalysisScope scope)
	//{
	//    return Expression.Constant(MakeQuoteConstant(
	//                                   expr.Expr, scope.GetRuntime()));
	//}

	//private static object MakeQuoteConstant(object expr, Sympl symplRuntime)
	//{
	//    if (expr is SymplListExpr)
	//    {
	//        SymplListExpr listexpr = (SymplListExpr)expr;
	//        int len = listexpr.Elements.Length;
	//        var exprs = new object[len];
	//        for (int i = 0; i < len; i++)
	//        {
	//            exprs[i] = MakeQuoteConstant(listexpr.Elements[i], symplRuntime);
	//        }
	//        return Cons._List(exprs);
	//    }
	//    else if (expr is IdOrKeywordToken)
	//    {
	//        return symplRuntime.MakeSymbol(((IdOrKeywordToken)expr).Name);
	//    }
	//    else if (expr is LiteralToken)
	//    {
	//        return ((LiteralToken)expr).Value;
	//    }
	//    else
	//    {
	//        throw new InvalidOperationException(
	//            "Internal: quoted list has -- " + expr.ToString());
	//    }
	//}

	//public static Expression GenerateEqExpr(SymplEqExpr expr,
	//                                        AnalysisScope scope)
	//{
	//    var mi = typeof(RuntimeHelpers).GetMethod("SymplEq");
	//    return Expression.Call(mi, Expression.Convert(
	//                                   GenerateExpr(expr.Left, scope),
	//                                   typeof(object)),
	//                           Expression.Convert(
	//                               GenerateExpr(expr.Right, scope),
	//                               typeof(object)));
	//}

	//public static Expression GenerateConsExpr(SymplConsExpr expr,
	//                                          AnalysisScope scope)
	//{
	//    var mi = typeof(RuntimeHelpers).GetMethod("MakeCons");
	//    return Expression.Call(mi, Expression.Convert(
	//                                   GenerateExpr(expr.Left, scope),
	//                                   typeof(object)),
	//                           Expression.Convert(
	//                               GenerateExpr(expr.Right, scope),
	//                               typeof(object)));
	//}

	//public static Expression GenerateListCallExpr(SymplListCallExpr expr,
	//                                              AnalysisScope scope)
	//{
	//    var mi = typeof(Cons).GetMethod("_List");
	//    int len = expr.Elements.Length;
	//    var args = new Expression[len];
	//    for (int i = 0; i < len; i++)
	//    {
	//        args[i] = Expression.Convert(GenerateExpr(expr.Elements[i], scope),
	//                                     typeof(object));
	//    }
	//    return Expression.Call(mi, Expression
	//                                   .NewArrayInit(typeof(object), args));
	//}

	public static Expression GenerateIfExpr(VB.LineIfStatement ifstmt, AnalysisScope scope)
	{
		Expression elseBlock = null;
		if (ifstmt.ElseStatements != null)
		{
			elseBlock = GenerateBlockExpr(ifstmt.ElseStatements, scope);
		}
		else
		{
			elseBlock = Expression.Empty();
		}

		Expression test = WrapBooleanTest(GenerateExpr(ifstmt.Expression, scope));
		Expression ifblock = GenerateBlockExpr(ifstmt.IfStatements, scope);

		return Expression.Condition(
				   test,
				   ifblock,
				   elseBlock,
				   typeof(void));
	}

	
	

	public static Expression GenerateNewExpr(VB.NewExpression expr,
											AnalysisScope scope)
	{

		VB.Name target = ((VB.NamedTypeName)expr.Target).Name;
		Expression targetExpr;
		if (target is VB.QualifiedName)
		{
			targetExpr = GenerateQualifiedNameExpr((VB.QualifiedName)target, scope);
		}
		else
		{
			targetExpr = GenerateSimpleNameExpr((VB.SimpleName)target, scope);
		}
		//args.Add(targetExpr);
		//args.AddRange(expr.Arguments.Select(a => GenerateExpr(a, scope)));
		List<Expression> args = GenerateArgumentList(expr.Arguments, scope);
		args.Insert(0, targetExpr);

		return Expression.Dynamic(
			scope.GetRuntime().GetCreateInstanceBinder(
								 new CallInfo(expr.Arguments.Count)),
			typeof(object),
			args
		);
	}

	public static Expression GenerateBinaryExpr(VB.BinaryOperatorExpression expr,
											   AnalysisScope scope)
	{

		// The language has the following special logic to handle And and Or
		// x And y == if x then y
		// x Or y == if x then x else (if y then y)
		ExpressionType op;
		switch(expr.Operator)
		{
			case VB.OperatorType.Concatenate:
				return Expression.Call(
					typeof(HelperFunctions).GetMethod("Concatenate"),
					RuntimeHelpers.EnsureObjectResult(GenerateExpr(expr.LeftOperand, scope)),
					RuntimeHelpers.EnsureObjectResult(GenerateExpr(expr.RightOperand, scope))
					);
			case VB.OperatorType.Plus:
				op = ExpressionType.Add;
				break;
			case VB.OperatorType.Minus:
				op = ExpressionType.Subtract;
				break;
			case VB.OperatorType.Multiply:
				op = ExpressionType.Multiply;
				break;
			case VB.OperatorType.Divide:
				op = ExpressionType.Divide;
				break;
			case VB.OperatorType.IntegralDivide:
				op = ExpressionType.Divide;
				break;
			case VB.OperatorType.Modulus:
				op = ExpressionType.Modulo;
				break;
			case VB.OperatorType.Equals:
				op = ExpressionType.Equal;
				break;
			case VB.OperatorType.NotEquals:
				op = ExpressionType.NotEqual;
				break;
			case VB.OperatorType.LessThan:
				op = ExpressionType.LessThan;
				break;
			case VB.OperatorType.GreaterThan:
				op = ExpressionType.GreaterThan;
				break;
			case VB.OperatorType.LessThanEquals:
				op = ExpressionType.LessThanOrEqual;
				break;
			case VB.OperatorType.GreaterThanEquals:
				op = ExpressionType.GreaterThanOrEqual;
				break;
			case VB.OperatorType.Is:
				op = ExpressionType.Equal;
				break;
			case VB.OperatorType.And:
				op = ExpressionType.And;
				break;
			case VB.OperatorType.Or:
				op = ExpressionType.Or;
				break;
			case VB.OperatorType.Xor:
				op = ExpressionType.ExclusiveOr;
				break;
			case VB.OperatorType.Power:
				op = ExpressionType.Power;
				break;
			default:
				throw new InvalidOperationException("Unknown binary operator " + expr.Operator);
		}
		return Expression.Dynamic(
			scope.GetRuntime().GetBinaryOperationBinder(op),
			typeof(object),
			GenerateExpr(expr.LeftOperand, scope),
			GenerateExpr(expr.RightOperand, scope)
		);
	}

	public static Expression GenerateUnaryExpr(VB.UnaryOperatorExpression expr,
											  AnalysisScope scope)
	{
		ExpressionType op;
		switch (expr.Operator)
		{
			case VB.OperatorType.Negate:
				op = ExpressionType.Negate;
				break;
			case VB.OperatorType.Not:
				op = ExpressionType.Not;
				break;
			default:
				throw new InvalidOperationException("Unknown unary operator " + expr.Operator);

		}
		return Expression.Dynamic(
			scope.GetRuntime().GetUnaryOperationBinder(op),
			typeof(object),
			GenerateExpr(expr.Operand, scope)
		);
	}

	private static void ensureBuiltinConstants()
	{
		lock (typeof(MvcGenerator))
			if (_builtinConstants == null)
			{
				_builtinConstants = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
				FieldInfo[] fis = typeof(BuiltInConstants).GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (FieldInfo fi in fis)
				{
					_builtinConstants.Add(fi.Name);
				}
			}
	}

	private static object getBuiltinConstant(string name)
	{
		FieldInfo fi = typeof(BuiltInConstants).GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
		return fi.GetValue(null);
	}

	private static void ensureBuiltinFunctions()
	{
		lock (typeof(MvcGenerator))
			if (_builtinFunctions == null)
			{
				_builtinFunctions = new Set<string>(StringComparer.InvariantCultureIgnoreCase);
				MethodInfo[] mis = typeof(BuiltInFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static);
				foreach (MethodInfo mi in mis)
				{
					_builtinFunctions.Add(mi.Name);
				}
			}
	}

	private static Expression GenerateSimpleNameExpr(VB.SimpleName simpleName, AnalysisScope scope)
	{
		string name = simpleName.Name;
		if (IsBuiltInConstants(name))
		{
			return Expression.Constant(getBuiltinConstant(name));
		}

		var param = FindIdDef(name, scope);
		if (param != null)
		{
			return param;
		}
		else
		{
			return Expression.Dynamic(
			   scope.GetRuntime().GetGetMemberBinder(name),
			   typeof(object),
			   scope.GetModuleExpr()
			);
		}
	}

	private static Expression FindIdDef(string name, AnalysisScope scope)
	{
		return FindIdDef(name, scope, false);
	}

	// FindIdDef returns the ParameterExpr for the name by searching the scopes,
	// or it returns None.
	//
	private static Expression FindIdDef(string name, AnalysisScope scope, bool generateIfNotFound)
	{
		var curscope = scope;
		name = name.ToLower();
		ParameterExpression res;
		while (curscope != null)
		{
			if (curscope.Names.TryGetValue(name, out res))
			{
				return res;
			}
			else
			{
				curscope = curscope.Parent;
			}
		}

		if (generateIfNotFound)
		{
			if (scope.ModuleScope.IsOptionExplicitOn)
				throw new InvalidOperationException(string.Format("Must declare variable {0}.", name));

			res = Expression.Parameter(typeof(object), name);
			scope.ModuleScope.Names.Add(name, res);
			return res;
		}

		if (scope == null)
		{
			throw new InvalidOperationException(
				"Got bad AnalysisScope chain with no module at end.");
		}

		return null;
	}

	private static List<Expression> GenerateArgumentList(VB.ArgumentCollection vbargs, AnalysisScope scope)
	{
		List<Expression> args = new List<Expression>();
		if (vbargs != null)
		{
			//args.AddRange(vbargs.Select(a => GenerateExpr(a.Expression, scope)));
			foreach (VB.Argument a in vbargs)
			{
				Expression argExp;
				if (a != null && a.Expression != null)
				{
					argExp = GenerateExpr(a.Expression, scope);
				}
				else
				{
					argExp = Expression.Constant(null);
				}
				args.Add(argExp);
			}
		}
		return args;
	}

	private static Expression GenerateSimpleNameAssignExpr(VB.SimpleNameExpression idExpr, Expression val, AnalysisScope scope)
	{
		string varName = idExpr.Name.Name;
		var param = FindIdDef(varName, scope, true);
		return Expression.Assign(
				   param,
				   Expression.Convert(val, param.Type));
	}

	private static LambdaExpression GenerateLambdaDef
	(VB.ParameterCollection parms, VB.StatementCollection body,
	 AnalysisScope scope, string name, bool isSub)
	{
		var funscope = new AnalysisScope(scope, name);
		var bodyscope = new AnalysisScope(funscope, name + " body");
		bodyscope.IsLambdaBody = true;
		funscope.IsLambda = true;  // needed for return support.
		var returnParameter = Expression.Parameter(typeof(object), name); //to support assign return value to func name
		if (isSub)
		{
			funscope.MethodExit = Expression.Label();
		}
		else
		{
			funscope.MethodExit = Expression.Label(typeof(object));
			bodyscope.Names[name] = returnParameter;
		}

		var paramsInOrder = new List<ParameterExpression>();
		int parmCount = 0;
		if (parms != null)
		{
			parmCount = parms.Count;
			foreach (var p in parms)
			{
				Type paramType;
				if (p.Modifiers != null && p.Modifiers.get_Item(0).ModifierType == Dlrsoft.VBScript.Parser.ModifierTypes.ByVal)
				{
					paramType = typeof(object);
				}
				else
				{
					paramType = typeof(object).MakeByRefType();
				}
				var pe = Expression.Parameter(paramType, p.VariableName.Name.Name);
				paramsInOrder.Add(pe);
				funscope.Names[p.VariableName.Name.Name.ToLower()] = pe;
			}
		}

		Expression bodyexpr = GenerateBlockExpr(body, bodyscope);

		// Set up the Type arg array for the delegate type.  Must include
		// the return type as the last Type, which is object for Sympl defs.
		var funcTypeArgs = new List<Type>();
		for (int i = 0; i < parmCount + 1; i++)
		{
			funcTypeArgs.Add(typeof(object));
		}

		Expression lastExpression;
		if (isSub)
		{
			//Return void
			lastExpression = Expression.Empty();
			//funcTypeArgs.Add(typeof(void));
		}
		else
		{
			lastExpression = returnParameter; //to return the function value
		}

		List<ParameterExpression> locals = new List<ParameterExpression>();
		foreach (ParameterExpression local in bodyscope.Names.Values)
		{
			locals.Add(local);
		}

		LambdaExpression lambda;
		//if (scope.GetRuntime().Debug)
		//{
		//    Expression registerRuntimeVariables = GenerateRuntimeVariablesExpression(bodyscope);

		//    lambda = Expression.Lambda(
		//           Expression.Label(
		//                funscope.MethodExit,
		//                Expression.Block(locals, registerRuntimeVariables, bodyexpr, lastExpression)
		//           ),
		//           paramsInOrder);
		//}
		//else
		//{
			lambda = Expression.Lambda(
				   Expression.Label(
						funscope.MethodExit,
						Expression.Block(locals, bodyexpr, lastExpression)
				   ),
				   paramsInOrder);
		//}
		//if (scope.GetRuntime().Debug)
		//{
		//    return scope.GetRuntime().DebugContext.TransformLambda(lambda);
		//}
		//else
		//{
			return lambda;
		//}
	}

	//public static Expression GenerateRuntimeVariablesExpression(AnalysisScope bodyscope)
	//{
	//    List<string> namesInScope = new List<string>();
	//    List<ParameterExpression> parametersInScope = new List<ParameterExpression>();
	//    bodyscope.GetVariablesInScope(namesInScope, parametersInScope);

	//    //Expression
	//    RuntimeVariablesExpression runtimeVariables = Expression.RuntimeVariables(parametersInScope);
	//    Expression traceHelper = getTraceHelper(bodyscope);
	//    Expression registerRuntimeVariables = Expression.Call(
	//        traceHelper,
	//        typeof(ITrace).GetMethod("RegisterRuntimeVariables"),
	//        Expression.Constant(namesInScope.ToArray()),
	//        runtimeVariables
	//    );
	//    return registerRuntimeVariables;
	//}

	private static Expression WrapBooleanTest(Expression expr)
	{
		var tmp = Expression.Parameter(typeof(object), "testtmp");
		return Expression.Block(
			new ParameterExpression[] { tmp },
			new Expression[]
					{Expression.Assign(tmp, Expression
											  .Convert(expr, typeof(object))),
					 Expression.Condition(
						 Expression.TypeIs(tmp, typeof(bool)),
						 Expression.Convert(tmp, typeof(bool)),
						 Expression.Call(typeof(BuiltInFunctions).GetMethod("CBool"), tmp))});
	}

	private static Expression ConvertToIntegerArrayExpression(List<Expression> args)
	{
		List<Expression> converted = new List<Expression>();
		foreach (Expression arg in args)
		{
			if (arg.Type == typeof(int))
			{
				converted.Add(arg);
			}
			else
			{
				converted.Add(Expression.Convert(arg, typeof(int)));
			}
		}
		//return converted;
		return Expression.NewArrayInit(typeof(int), converted);
	}

	internal static Expression WrapTryCatchExpression(Expression stmt, Dlrsoft.VBScript.Compiler.AnalysisScope scope)
	{
		ParameterExpression exception = Expression.Parameter(typeof(Exception));

		return Expression.TryCatch(
			Expression.Block(
				stmt,
				Expression.Empty()
			),
			Expression.Catch(
				exception,
				Expression.Call(
					typeof(HelperFunctions).GetMethod("SetError"),
					scope.ErrExpression,
					exception
				)
			)
		);
	}

	internal static Expression GenerateDebugInfo(VB.Tree stmt, AnalysisScope scope, out Expression clearDebugInfo)
	{
		ISourceMapper mapper = scope.ModuleScope.SourceMapper;
		DocSpan docSpan = mapper.Map(SourceUtil.ConvertSpan(stmt.Span));
		SourceLocation start = docSpan.Span.Start;
		SourceLocation end = docSpan.Span.End;
		SymbolDocumentInfo docInfo = scope.ModuleScope.GetDocumentInfo(docSpan.Uri);

		//Expression debugInfo = Expression.DebugInfo(docInfo, start.Line, start.Column, end.Line, end.Column);
		//clearDebugInfo = Expression.ClearDebugInfo(docInfo);
		//return debugInfo;

		Expression traceHelper = getTraceHelper(scope);

		Expression debugInfo = Expression.Call(
			traceHelper,
			typeof(ITrace).GetMethod("TraceDebugInfo"),
			Expression.Constant(docInfo.FileName),
			Expression.Constant(start.Line),
			Expression.Constant(start.Column),
			Expression.Constant(end.Line),
			Expression.Constant(end.Column)
		);
		clearDebugInfo = Expression.Empty();
		return debugInfo;
	}

	private static Expression getTraceHelper(AnalysisScope scope)
	{
		Expression traceHelper = scope.TraceExpression;
		return traceHelper;
	}

	// _findFirstLoop returns the first loop AnalysisScope or None.
	//
	private static AnalysisScope _findFirstScope(AnalysisScope scope, VB.BlockType blockType)
	{
		var curscope = scope;
		while (curscope != null)
		{
			switch (blockType)
			{
				case Dlrsoft.VBScript.Parser.BlockType.Sub:
				case Dlrsoft.VBScript.Parser.BlockType.Function:
					if (curscope.IsLambda)
					{
						return curscope;
					}
					break;
				case Dlrsoft.VBScript.Parser.BlockType.Do:
					if (curscope.IsDoLoop)
					{
						return curscope;
					}
					break;
				case Dlrsoft.VBScript.Parser.BlockType.For:
					if (curscope.IsForLoop)
					{
						return curscope;
					}
					break;
			}
			curscope = curscope.Parent;

		}
		return null;
	}

	/// <summary>
	/// Wrap around a DLR expression to inject into the VB expression tree
	/// </summary>
	class ExpressionExpression : VB.Expression
	{
		Expression _expression;

		public ExpressionExpression(Expression expr, VB.Span span)
			: base(VB.TreeType.AddressOfExpression, span)
		{
			_expression = expr;
		}

		public Expression Expression
		{
			get { return _expression; }
		}
	}*/
}
