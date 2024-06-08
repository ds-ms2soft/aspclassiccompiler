using System;
using System.Collections.Generic;
using System.Linq;
using Dlrsoft.Asp;
using Dlrsoft.VBScript.Compiler;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public class TranspileUnit
	{
		private TranspileUnit(string absolutePath, ScriptBlock block, AspPageDom page)
		{
			AbsolutePath = absolutePath;
			Block = block;
			Page = page;
		}
		
		public AspPageDom Page { get; }
		public ScriptBlock Block { get; }

		public string AbsolutePath { get; }

		public static TranspileUnit Parse(string absolutePath)
		{
			var page = new AspPageDom();
			page.processPage(absolutePath);

			var s = new VBScriptStringContentProvider(page.Code, page.Mapper);
			var reader = s.GetReader();
			var scanner = new Scanner(reader);
			var errorTable = new List<SyntaxError>();

			var block = new Parser().ParseScriptFile(scanner, errorTable);
			if (errorTable.Any())
			{
				throw new Exception("Failed to parse file");
			}

			return new TranspileUnit(absolutePath, block, page);
		}
	}
}
