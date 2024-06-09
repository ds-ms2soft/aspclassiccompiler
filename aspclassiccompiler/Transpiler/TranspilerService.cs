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

		/// <summary>
		/// Returns the number of files with errors (which won't be transpiled).
		/// </summary>
		public int ParseAllFiles()
		{
			_unitsByPath = Directory.GetFiles(_sourceFolderBase, "*.asp", SearchOption.AllDirectories).ToDictionary(path => path, TranspileUnit.Parse,StringComparer.OrdinalIgnoreCase);
			return _unitsByPath.Count(unit => unit.Value.HasErrors);
		}

		public void IdentifyIncludes()
		{
			_includePagesByPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var unit in _unitsByPath.Values.Where(tu => !tu.HasErrors))
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

		public void TranspileIncludes(string relativeOutputFolder, string namespaceRoot, string baseClassName)
		{
			foreach (var path in _includePagesByPath)
			{
				if (!_unitsByPath.TryGetValue(path, out var unit))
				{
					_unitsByPath[path] = unit = TranspileUnit.Parse(path);
				}
				var subFolders = Path.GetDirectoryName(path).Substring(_sourceFolderBase.Length).Split(Path.DirectorySeparatorChar);
				var transpiler = new MvcGenerator();

				string output = Path.Combine(_outputFolderBase, relativeOutputFolder, String.Join(Path.DirectorySeparatorChar.ToString(), subFolders), 
					Path.GetFileNameWithoutExtension(path) + ".vb");

				Directory.CreateDirectory(Path.GetDirectoryName(output));

				using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (var writer = new StreamWriter(outFile))
				using (var classWriter = new IncludeClassWriter(writer))
				{
					classWriter.Namespace = namespaceRoot + String.Join("", subFolders.Select(part => "." + part));
					classWriter.BaseClass = baseClassName;
					classWriter.ClassName = Path.GetFileNameWithoutExtension(new FileInfo(path).Name); //Filename controls the case
					var scope = transpiler.Transpile(unit.Block, classWriter, unit.Page.Literals);
					//TODO: associate this scope with the path so we can resolve the variables in pages that include this
				}
			}
		}

		public void TranspileNonIncludePages()
		{}

		public void Transpile(string relativeFilePath)
		{
			string output = Path.Combine(_outputFolderBase,
				Path.GetFileNameWithoutExtension(relativeFilePath) + ".Asp.vbhtml");

			var unit = TranspileUnit.Parse(Path.Combine(_sourceFolderBase, relativeFilePath));
				
				var transpiler = new MvcGenerator();
				
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
