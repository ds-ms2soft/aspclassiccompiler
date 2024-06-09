using System.Collections.Generic;
using System.Linq;
using Dlrsoft.Asp;
using Dlrsoft.VBScript.Compiler;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public class TranspileUnit
	{
		private readonly List<SyntaxError> _errorTable;

		private TranspileUnit(string absolutePath, ScriptBlock block, AspPageDom page, List<SyntaxError> errorTable)
		{
			_errorTable = errorTable;
			AbsolutePath = absolutePath;
			Block = block;
			Page = page;
		}
		
		public AspPageDom Page { get; }
		public ScriptBlock Block { get; }
		
		public string AbsolutePath { get; }

		public bool HasErrors => _errorTable.Any();
		public string IncludeClassName { get; set; }
		public IdentifierScope IncludeScope { get; set; }

		public static TranspileUnit Parse(string absolutePath)
		{
			var page = new AspPageDom();
			page.processPage(absolutePath);

			var s = new VBScriptStringContentProvider(page.Code, page.Mapper);
			var reader = s.GetReader();
			var scanner = new Scanner(reader);
			var errorTable = new List<SyntaxError>();

			var block = new Parser().ParseScriptFile(scanner, errorTable);
			

			return new TranspileUnit(absolutePath, block, page, errorTable);
		}
	}
}
