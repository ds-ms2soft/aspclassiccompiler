using System.IO;
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
	}
}
