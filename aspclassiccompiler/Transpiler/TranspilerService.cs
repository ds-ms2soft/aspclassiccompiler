using System.Collections.Generic;
using System.IO;
using Dlrsoft.VBScript.Parser;
using File = System.IO.File;

namespace Transpiler
{
	public class TranspilerService
	{
		private readonly string _sourceFolderBase;
		private readonly string _outputFolderBase;

		public TranspilerService(string sourceFolderBase, string outputFolderBase)
		{
			_sourceFolderBase = sourceFolderBase;
			_outputFolderBase = outputFolderBase;
		}

		public void Transpile(string relativeFilePath)
		{
			var input = Path.Combine(_sourceFolderBase, relativeFilePath);
			string output = Path.Combine(_outputFolderBase,
				Path.GetFileNameWithoutExtension(relativeFilePath) + ".Asp.vbhtml");
			using (var reader = File.OpenText(input))
			{
				var scanner = new Scanner(reader);
				var errorTable = new List<SyntaxError>();

				var block = new Parser().ParseScriptFile(scanner, errorTable);
				var transpiler = new MvcGenerator(block); //TODO: I will want this transpiler to have state beyond one file eventually, so it can process includes once only.
				transpiler.Transpile();
				using (var writer = File.OpenWrite(output))
				{
					//transpiler.
				}

			}

		}
	}
}
