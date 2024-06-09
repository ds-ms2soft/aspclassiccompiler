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
				var output = MakeNewFileOutputPath(Path.Combine(_outputFolderBase, relativeOutputFolder), path, ".vb", out var subFolders);

				var transpiler = new MvcGenerator();

				using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (var writer = new StreamWriter(outFile))
				using (var classWriter = new IncludeClassWriter(writer))
				{
					classWriter.Namespace = namespaceRoot + String.Join("", subFolders.Select(part => "." + part));
					classWriter.BaseClass = baseClassName;
					classWriter.ClassName = Path.GetFileNameWithoutExtension(new FileInfo(path).Name); //Filename controls the case
					var scope = transpiler.Transpile(unit.Block, classWriter, unit.Page.Literals);
					unit.IncludeClassName = String.Join(".", classWriter.Namespace, classWriter.ClassName);
					unit.IncludeScope = scope;
				}
			}
		}

		/// <summary>
		/// Assumes that path is a full path to a file under _sourceFolderBase.
		/// Figures out which folders it's nested in under _sourceFolderBase and replicates that under the rootOutputFolder.
		/// Creates output directories as needed. Returns the new output file path, and outputs the subfolders we found, which might be needed for namespaces.
		/// </summary>
		private string MakeNewFileOutputPath(string rootOutputFolder, string path, string newExtension, out string[] subFolders)
		{
			subFolders = Path.GetDirectoryName(path).Substring(_sourceFolderBase.Length).Split(Path.DirectorySeparatorChar);
				
			var output = Path.Combine(rootOutputFolder, String.Join(Path.DirectorySeparatorChar.ToString(), subFolders), 
				Path.GetFileNameWithoutExtension(path) + newExtension);

			Directory.CreateDirectory(Path.GetDirectoryName(output));
			return output;
		}

		/// <summary>
		/// Transpiles any page that isn't an include and that parsed without error.
		/// </summary>
		public int TranspileValidPages()
		{
			var count = 0;
			foreach (var kvp in _unitsByPath.Where(kvp => !_includePagesByPath.Contains(kvp.Key) && !kvp.Value.HasErrors))
			{
				var path = kvp.Key;
				var unit = kvp.Value;
				try
				{
					TranspileSingle(path, unit);
				}
				catch (Exception ex)
				{
					throw new Exception($"Failed to transpile: {path}", ex);
				}

				count++;
			}

			return count;
		}

		public void TranspileSingle(string relativeFilePath)
		{
			var path = Path.Combine(_sourceFolderBase, relativeFilePath);
			var unit = TranspileUnit.Parse(path);

			TranspileSingle(path, unit);
		}

		public void TranspileSingle(string filePath, TranspileUnit unit)
		{
			var output = MakeNewFileOutputPath(_outputFolderBase, filePath, ".Asp.Vbhtml", out _);

			var transpiler = new MvcGenerator()
			{
				HandleServerSideInclude = HandleServerSideInclude
			};

			using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
			using (var writer = new StreamWriter(outFile))
			{
				var razorWriter = new RazorWriter(writer);
				transpiler.Transpile(unit.Block, razorWriter, unit.Page.Literals);
			}
		}

		private void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope)
		{
			if (!OverrideHandleServerSideInclude(fullPath, output, scope))
			{
				if (!_unitsByPath.TryGetValue(fullPath, out var include))
				{
					throw new Exception($"Missing a referenced include file: {fullPath}");
				}

				var variableName = "_" + include.IncludeClassName.Split('.').Last();
				output.WriteCode($"Dim {variableName} = New {include.IncludeClassName}(Page)", true);
				scope.MapToVariable(include.IncludeScope, variableName);
			}
		}

		/// <summary>
		/// Return true if you've handled it.
		/// </summary>
		public Func<string, OutputWriter, IdentifierScope, bool> OverrideHandleServerSideInclude { get; set; } =
			(s, writer, arg3) => false;
	}
}
