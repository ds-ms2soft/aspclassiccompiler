using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dlrsoft.Asp;
using Dlrsoft.VBScript.Compiler;
using Dlrsoft.VBScript.Parser;
using File = Dlrsoft.VBScript.Parser.File;

namespace Transpiler
{
	public class TranspileUnit
	{
		public List<SyntaxError> ErrorTable { get; }

		private TranspileUnit(string absolutePath, ScriptBlock block, AspPageDom page, List<SyntaxError> errorTable)
		{
			ErrorTable = errorTable;
			AbsolutePath = absolutePath;
			Block = block;
			Page = page;
		}
		
		public AspPageDom Page { get; }
		public ScriptBlock Block { get; }
		
		public string AbsolutePath { get; }

		public bool HasErrors => ErrorTable.Any();
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
			
			foreach (var error in errorTable)
			{
				var docSpan = page.Mapper.Map(error.GeneratedSpan);
				var start = new Location(docSpan.Span.Start.Index, docSpan.Span.Start.Line, docSpan.Span.Start.Column);
				var end = new Location(docSpan.Span.End.Index, docSpan.Span.End.Line, docSpan.Span.End.Column); 
				error.SourceSpan = new Span(start, end);
			}
			return new TranspileUnit(absolutePath, block, page, errorTable);
		}
	}
}
