﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	/// <summary>
	/// This class hard codes some very specific MS2 for both transpiling includes and then handling them in pages.
	/// </summary>
	public class Ms2Transpiler: TranspilerService
	{
		private const string InputFolder = @"C:\source\TDMS\TCDS.Web";
		private const string OutputFolder = @"C:\source\TDMS\TCDS.Web\Views\Home\";

		public Ms2Transpiler(): base(InputFolder, OutputFolder)
		{
			ConfigureIncludeProcessing(@"..\..\Includes", "Includes", "Includes.IncludesBase", "CustomViewPage");
		}

		protected override MvcGenerator MakeGeneratorForFile()
		{
			var generator = new Ms2MvcGenerator();

			generator.HandleServerSideInclude = (s, writer, scope) =>
				HandleServerSideInclude(s, writer, scope, null, generator);
			
			return generator;
		}

		protected override void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope,
			IncludeClassWriter fromInclude)
		{
			HandleServerSideInclude(fullPath, output, scope, fromInclude, null);
		}

		protected override void DefineScopeForIncludeFile(IdentifierScope includeScope)
		{
			foreach (var name in new[] { "Server", "Request", "Response", "Session", "Application", "OpenRecordSet" })
			{
				//Forwards to the page object.
				includeScope.Define(name, "HostPage." + name);
			}
			base.DefineScopeForIncludeFile(includeScope);
		}

		protected void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope, IncludeClassWriter fromInclude, Ms2MvcGenerator generator)
		{
			if (fullPath.EndsWith("\\_ms2Helper.asp", StringComparison.OrdinalIgnoreCase))
			{
				scope.Define("MS2", null);
				scope.Define("headerModules", "Ms2Helper.HeaderModules");
			}
			else if (fullPath.EndsWith("\\functions\\sql_datatypes.asp", StringComparison.OrdinalIgnoreCase))
			{
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
			}
			else if (fullPath.EndsWith("\\security.asp", StringComparison.OrdinalIgnoreCase))
			{
				HandleServerSideInclude(fullPath, output, scope, fromInclude, new[]
				{
					new IncludeFileConstructorParameter { Name = "ref_path", Type = "string", DefaultIfMissing = "\"\"", 
						StaticValue = generator?.CompileTimeVariableValues.TryGetValue("ref_path", out var ref_path) == true ? ref_path : null
					}
				});
			}
			else if (fullPath.EndsWith("\\_hack.asp", StringComparison.OrdinalIgnoreCase))
			{
				//Nothing I want to port
			}
			//else if (fullPath.EndsWith("\\session_context.asp", StringComparison.OrdinalIgnoreCase))
			//{
			//	//Just some global methods that we'll put in the page base class
			//}
			else if (fullPath.EndsWith("\\conn.asp", StringComparison.OrdinalIgnoreCase))
			{
				//Just some global methods that we'll put in the page base class
			}
			else if (fullPath.EndsWith("\\nav.asp", StringComparison.OrdinalIgnoreCase))
			{
				HandleServerSideInclude(fullPath, output, scope, fromInclude, new[]
				{
					new IncludeFileConstructorParameter { Name = "page", Type = "string", DefaultIfMissing = "\"\"" },
					new IncludeFileConstructorParameter { Name = "is_map_update_all", Type = "Boolean", DefaultIfMissing = "False" },
					new IncludeFileConstructorParameter { Name = "is_map_update", Type = "Boolean", DefaultIfMissing = "False" },
					new IncludeFileConstructorParameter { Name = "num_records", Type = "Long", DefaultIfMissing = "0" },
					new IncludeFileConstructorParameter { Name = "is_new", Type = "Boolean", DefaultIfMissing = "False" },
					new IncludeFileConstructorParameter { Name = "is_edit", Type = "Boolean", DefaultIfMissing = "False" },
					new IncludeFileConstructorParameter { Name = "rst", Type = "Object", DefaultIfMissing = "Nothing" }
				});
			}
			else if (fullPath.EndsWith("\\banner.asp", StringComparison.OrdinalIgnoreCase))
			{
				//I did some hacking to get it to transpile (global vars), and then I manually changed the class.
				//var include = EnsureIncludeTranspiled(fullPath);
				output.WriteCode($"New Includes.banner(Me, {(generator?.CompileTimeVariableValues?.TryGetValue("ShowOldBanner", out var v) == true ? v : "false")})", true);
			}
			else
			{
				base.HandleServerSideInclude(fullPath, output, scope, fromInclude);
			}
		}
	}
}
