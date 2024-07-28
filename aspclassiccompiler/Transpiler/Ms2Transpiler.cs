using System;
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
			ConfigureIncludeProcessing(@"..\..\Includes", "Includes", "Includes.IncludesBase");
		}

		private static bool ShowOldBanner { get; set; }

		private static string OverrideVariableDeclaration(string varName)
		{
			if ("CompileTime_showOldBanner".Equals(varName, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			
			return varName;
		}

		protected override MvcGenerator MakeGeneratorForFile()
		{
			ShowOldBanner = false; //reset our state
			var generator = base.MakeGeneratorForFile();
			generator.AddToGlobalScope = globals =>
			{
				globals.Define("showOldBanner", "CompileTime_showOldBanner");
				globals.Define("MS2", null);
			};
			generator.OverrideVariableDeclaration = OverrideVariableDeclaration;
			generator.OverrideVariableAssign = (name, value) =>
			{
				if ("CompileTime_showOldBanner".Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					ShowOldBanner = bool.Parse(value);
					return true;
				}
				else
				{
					return false;
				}
			};
			return generator;
		}


		protected override void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope)
		{
			if (fullPath.EndsWith("\\_ms2Helper.asp", StringComparison.OrdinalIgnoreCase))
			{
				scope.Define("MS2", null);
			}
			else if (fullPath.EndsWith("\\_hack.asp", StringComparison.OrdinalIgnoreCase))
			{
				//Nothing I want to port
			}
			//else if (fullPath.EndsWith("\\session_context.asp", StringComparison.OrdinalIgnoreCase))
			//{
			//	//Just some global methods that we'll put in the page base classe
			//}
			else if (fullPath.EndsWith("\\conn.asp", StringComparison.OrdinalIgnoreCase))
			{
				//Just some global methods that we'll put in the page base classe
			}
			else if (fullPath.EndsWith("\\banner.asp", StringComparison.OrdinalIgnoreCase))
			{
				//I did some hacking to get it to transpile (global vars), and then I manually changed the class.
				//var include = EnsureIncludeTranspiled(fullPath);
				output.WriteCode($"New Includes.banner(Me, {ShowOldBanner})", true);
			}
			else
			{
				base.HandleServerSideInclude(fullPath, output, scope);
			}
		}
	}
}
