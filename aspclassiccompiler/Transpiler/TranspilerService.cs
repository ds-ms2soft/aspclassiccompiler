using System.Collections.Generic;
using System.IO;
using Dlrsoft.Asp;
using Dlrsoft.VBScript.Compiler;
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
		
				AspPageDom page = new AspPageDom();
				page.processPage(input);
			
				var s = new VBScriptStringContentProvider(page.Code, page.Mapper);
				var reader = s.GetReader();
				var scanner = new Scanner(reader);
				var errorTable = new List<SyntaxError>();

				var block = new Parser().ParseScriptFile(scanner, errorTable);
				var transpiler = new MvcGenerator(); //TODO: I will want this transpiler to have state beyond one file eventually, so it can process includes once only.
				
				using (var outFile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (var writer = new StreamWriter(outFile))
				{
					var razorWriter = new RazorWriter(writer);
					transpiler.Transpile(block, razorWriter, page.Literals);
					//transpiler.
				}
		}
	}
}
