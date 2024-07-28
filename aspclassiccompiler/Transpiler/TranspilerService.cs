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
		private string _includeBaseClassName;
		private string _includeFileOutputFolder;
		private string _includeNamespaceRoot;

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
			var all = Directory.GetFiles(_sourceFolderBase, "*.asp", SearchOption.AllDirectories)
				.Where(x => Path.GetExtension(x) == ".asp"); //filter out .aspx
			
			
			_unitsByPath = all.ToDictionary(path => path, TranspileUnit.Parse,StringComparer.OrdinalIgnoreCase);
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
						path = Path.GetFullPath(path);
						_includePagesByPath.Add(path);
					}
				});
			}
		}

		public void ConfigureIncludeProcessing(string relativeOutputFolder, string namespaceRoot, string baseClassName)
		{
			_includeFileOutputFolder = Path.GetFullPath(Path.Combine(_outputFolderBase, relativeOutputFolder));
			_includeNamespaceRoot = namespaceRoot;
			_includeBaseClassName = baseClassName;
		}

		public TranspileUnit EnsureIncludeTranspiled(string fullPath)
		{
			try
			{
				if (!_unitsByPath.TryGetValue(fullPath, out var unit))
				{
					_unitsByPath[fullPath] = unit = TranspileUnit.Parse(fullPath);
				}

				if (unit.IncludeClassName == null)
				{
					var output = MakeNewFileOutputPath(_includeFileOutputFolder, fullPath,
						".vb", out var subFolders);
					//Note: we need to transpile the file, even if we already have file output, (from a prior run),
					//because we need to know the scope of the variables and functions defined in the include.
					var transpiler = MakeGeneratorForFile();

					using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite,
						       FileShare.ReadWrite))
					using (var writer = new StreamWriter(outFile))
					using (var classWriter = new IncludeClassWriter(writer))
					{
						classWriter.Namespace = _includeNamespaceRoot +
						                        String.Join("", subFolders.Select(part => "." + part));
						classWriter.BaseClass = _includeBaseClassName;
						classWriter.ClassName =
							Path.GetFileNameWithoutExtension(new FileInfo(fullPath).Name); //Filename controls the case
						var scope = transpiler.Transpile(unit.Block, classWriter, unit.Page.Literals);
						unit.IncludeClassName = String.Join(".", classWriter.Namespace, classWriter.ClassName);
						unit.IncludeScope = scope;
					}
					SetLastWrite(output, fullPath); //Set this to match the input, even if we can't use it as an optimization to prevent processing again.
				}
				return unit;
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to transpile include: {fullPath}", ex);
			}
		}

		private static bool IsOutputCurrent(string input, string output)
		{
			return File.Exists(output) && File.GetLastWriteTime(input) == File.GetLastWriteTime(output);
		}

		private static void SetLastWrite(string input, string output)
		{
			File.SetLastWriteTime(output, File.GetLastWriteTime(input));
		}

		/// <summary>
		/// Assumes that path is a full path to a file under _sourceFolderBase.
		/// Figures out which folders it's nested in under _sourceFolderBase and replicates that under the rootOutputFolder.
		/// Creates output directories as needed. Returns the new output file path, and outputs the subfolders we found, which might be needed for namespaces.
		/// </summary>
		private string MakeNewFileOutputPath(string rootOutputFolder, string path, string newExtension, out string[] subFolders)
		{
			subFolders = Path.GetDirectoryName(path)
				.Substring(_sourceFolderBase.Length)
				.TrimStart(Path.DirectorySeparatorChar)
				.Split(new []{Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries);
			
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

		public virtual void TranspileSingle(string filePath, TranspileUnit unit)
		{
			var output = MakeNewFileOutputPath(_outputFolderBase, filePath, ".Asp.Vbhtml", out _);

			if (!IsOutputCurrent(filePath, output))
			{
				var transpiler = new MvcGenerator()
				{
					HandleServerSideInclude = HandleServerSideInclude
				};

				using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (var writer = new StreamWriter(outFile))
				{
					using (var razorWriter = new RazorWriter(writer))
					{
						transpiler.Transpile(unit.Block, razorWriter, unit.Page.Literals);
					}
				}
				SetLastWrite(filePath, output);
			}
		}

		protected virtual void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope)
		{
			HandleServerSideInclude(fullPath, output, scope, null);
		}

		protected void HandleServerSideInclude(string fullPath, OutputWriter output, IdentifierScope scope, string initializer)
		{
			var include = EnsureIncludeTranspiled(fullPath);

			var variableName = "_" + include.IncludeClassName.Split('.').Last();
			output.WriteCode($"Dim {variableName} = New {include.IncludeClassName}{initializer ?? "(Me)"}", true);
			scope.MapToVariable(include.IncludeScope, variableName);
		}

		protected virtual MvcGenerator MakeGeneratorForFile() => new MvcGenerator()
		{
			HandleServerSideInclude = HandleServerSideInclude
		};
	}
}
