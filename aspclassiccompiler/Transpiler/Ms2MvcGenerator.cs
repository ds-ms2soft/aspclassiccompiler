﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Transpiler
{
	/// <summary>
	/// Ms2 specific overrides for code generation
	/// </summary>
	public class Ms2MvcGenerator: MvcGenerator
	{
		private Regex DataConnExecute = new Regex(@"DataConn.Execute(?<args>\(.*\))", RegexOptions.IgnoreCase);

		private static readonly HashSet<string> _elevateVariableToCompileTimeConstant = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"showOldBanner",
			"ref_path",
		};

		public readonly Dictionary<string, string> CompileTimeVariableValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		protected override string OverrideVariableDeclaration(string name, IdentifierScope scope)
		{
			if (scope.IsGlobal && _elevateVariableToCompileTimeConstant.Contains(name))
			{
				return null;
			}
			return base.OverrideVariableDeclaration(name, scope);
		}

		protected override void AddToGlobalScope(IdentifierScope scope)
		{
			foreach (var name in _elevateVariableToCompileTimeConstant)
			{
				scope.Define(name);
			}

			foreach (var ported in new[] { "Array", "IsNull", "Minute", "Hour" })
			{
				scope.Define(ported, "ClassicAspPort." + ported);
			}
			scope.Define("MS2", null);
			scope.Define("DataConn");
			scope.Define("MM_CONN_STRING", "CustomViewPage.ConnectionString");
			scope.Define("ExecuteWithSessionContext", "CustomViewPage.ExecuteWithSessionContext");
			scope.Define("ExecuteQuery");
			foreach (var name in new string[]{"adBoolean"
				         ,"adSmallInt"
				         ,"adInteger"
				         ,"adTinyInt"
				         ,"adUnsignedTinyInt"
				         ,"adUnsignedSmallInt"
				         ,"adUnsignedInt"
				         ,"adBigInt"
				         ,"adUnsignedBigInt"
				         ,"adSingle"
				         ,"adDouble"
				         ,"adCurrency"
				         ,"adDecimal"
				         ,"adVarNumeric"
				         ,"adDate"
				         ,"adDBDate"
				         ,"adDBTime"
				         ,"adDBTimeStamp"
				         ,"adFileTime"
				         ,"adBSTR"
				         ,"adChar"
				         ,"adWChar"
				         ,"adNumeric"
				         ,"adUserDefined"
				         ,"adPropVariant"
				         ,"adVarChar"
				         ,"adLongVarChar"
				         ,"adVarWChar"
				         ,"adLongVarWChar"
				         ,"adBinary"
				         ,"adVarBinary"
				         ,"adLongVarBinary"
				         ,"adEmpty"
				         ,"adIDispatch"
				         ,"adError"
				         ,"adVariant"
				         ,"adIUnknown"
				         ,"adGUID"
				         ,"adChapter"})
			{
				scope.Define(name, "Includes.functions.sql_datatypes." + name);
			}
			base.AddToGlobalScope(scope);
		}

		protected override void GenerateAssignExpr(bool isUndefined, string variable, string value, IdentifierScope scope)
		{
			if (value?.StartsWith("DataConn") == true)
			{
				var match = DataConnExecute.Match(value);
				if (match.Success)
				{
					Output.WriteCode($"{(isUndefined ? "Dim " : "")}{variable} = {(IsIncludeFile? "HostPage.": "")}OpenRecordSet{match.Groups["args"].Value}", true);
				}
				//else ignore
			}
			else if (scope.IsGlobal && _elevateVariableToCompileTimeConstant.Contains(variable))
			{
				CompileTimeVariableValues[variable] = value;
				//Don't write it out
			}
			else
			{
				base.GenerateAssignExpr(isUndefined, variable, value, scope);
			}
			
		}
	}
}
