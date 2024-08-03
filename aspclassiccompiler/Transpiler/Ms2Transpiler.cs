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

		private static readonly HashSet<string> _elevateVariableToCompileTimeConstant = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"showOldBanner",
		};

		private Dictionary<string, string> _compileTimeVariableValues;

		private static string OverrideVariableDeclaration(string varName, IdentifierScope scope)
		{
			if (scope.IsGlobal && _elevateVariableToCompileTimeConstant.Contains(varName))
			{
				return null;
			}
			
			return varName;
		}

		protected override MvcGenerator MakeGeneratorForFile()
		{
			_compileTimeVariableValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var generator = base.MakeGeneratorForFile();
			generator.AddToGlobalScope = globals =>
			{
				foreach (var name in _elevateVariableToCompileTimeConstant)
				{
					globals.Define(name);
				}

				foreach (var ported in new[]{ "Array", "IsNull" })
				{
					globals.Define(ported, "ClassicAspPort." + ported);
				}
				globals.Define("MS2", null);
			};
			generator.OverrideVariableDeclaration = OverrideVariableDeclaration;
			generator.OverrideVariableAssign = (name, value, scope) =>
			{
				if (scope.IsGlobal && _elevateVariableToCompileTimeConstant.Contains(name))
				{
					_compileTimeVariableValues[name] = value;
					return true;
				}
				else
				{
					return false;
				}
			};
			return generator;
		}


		protected override void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope, IncludeClassWriter fromInclude)
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
					new IncludeFileConstructorParameter { Name = "rst", Type = "Object", DefaultIfMissing = "Nothing" },

				});
			}
			else if (fullPath.EndsWith("\\banner.asp", StringComparison.OrdinalIgnoreCase))
			{
				//I did some hacking to get it to transpile (global vars), and then I manually changed the class.
				//var include = EnsureIncludeTranspiled(fullPath);
				output.WriteCode($"New Includes.banner(Me, {(_compileTimeVariableValues.TryGetValue("ShowOldBanner", out var v) ? v : "false")})", true);
			}
			else
			{
				base.HandleServerSideInclude(fullPath, output, scope, fromInclude);
			}
		}
	}
}
