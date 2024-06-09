using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dlrsoft.Asp;
using Dlrsoft.VBScript.Parser;
using File = System.IO.File;

namespace Transpiler
{
	public class TranspilerService
	{
		private readonly string _sourceFolderBase;
		private readonly string _outputFolderBase;

		private Dictionary<string, TranspileUnit> _unitsByPath;
		private HashSet<string> _includePagesByPath;

		public IEnumerable<string> IncludePages => _includePagesByPath ?? (IEnumerable<string>)Array.Empty<string>();

		public TranspilerService(string sourceFolderBase, string outputFolderBase)
		{
			_sourceFolderBase = sourceFolderBase;
			_outputFolderBase = outputFolderBase;
		}

		public void ParseAllFiles()
		{
			_unitsByPath = Directory.GetFiles(_sourceFolderBase, "*.asp", SearchOption.AllDirectories).ToDictionary(path => path, TranspileUnit.Parse,StringComparer.OrdinalIgnoreCase);
		}

		public void IdentifyIncludes()
		{
			_includePagesByPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var unit in _unitsByPath.Values)
			{
				unit.Block.VisitAll<CallStatement>(exp =>
				{
					if (exp.Matches(AspPageDom.ServerSideInclude))
					{
						var path = ((StringLiteralExpression)exp.Arguments.First().Expression).Literal;
						_includePagesByPath.Add(path);
					}
				});
			}
		}

		public void ConvertIncludes(string outputFolderBase)
		{

		}

		public void TranspileNonIncludePages()
		{}
		public void Transpile(string relativeFilePath)
		{
			string output = Path.Combine(_outputFolderBase,
				Path.GetFileNameWithoutExtension(relativeFilePath) + ".Asp.vbhtml");

			var unit = TranspileUnit.Parse(Path.Combine(_sourceFolderBase, relativeFilePath));
				
				var transpiler = new MvcGenerator(); //TODO: I will want this transpiler to have state beyond one file eventually, so it can process includes once only.
				
				using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (var writer = new StreamWriter(outFile))
				{
					var razorWriter = new RazorWriter(writer);
					transpiler.Transpile(unit.Block, razorWriter, unit.Page.Literals);
				}
		}

		private void HandleServerSideInclude(string fullPath, RazorWriter output, IdentifierScope scope)
		{
			if (!OverrideHandleServerSideInclude(fullPath, output, scope))
			{
				
			}
		}

		/// <summary>
		/// Return true if you've handled it.
		/// </summary>
		public Func<string, RazorWriter, IdentifierScope, bool> OverrideHandleServerSideInclude { get; set; } =
			(s, writer, arg3) => false;
	}
}
