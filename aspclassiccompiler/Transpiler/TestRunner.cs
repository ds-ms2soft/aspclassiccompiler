using System;
using System.IO;
using NUnit.Framework;

namespace Transpiler
{
	[NUnit.Framework.TestFixture]
	public class TestRunner
	{
		[TestCase("components\\FileSystem.asp")]
		[TestCase("Database\\SimpleQuery.asp")]
		[TestCase("Components\\BrowserCap.asp")]
		[TestCase("Simple\\Functions.asp")]
		[TestCase("Simple\\Looping.asp")]
		[TestCase("Simple\\Variables.asp")]
		[TestCase("test\\imports.asp")] //Left off here
		public void TranspileOne(string fileName)
		{
			var folder = Path.GetDirectoryName(fileName);
			var service = new TranspilerService(@"C:\Work\aspclassiccompiler\aspclassiccompiler\AspWebApp\" + folder,
				@"C:\Work\aspclassiccompiler\aspclassiccompiler\MvcTestApp\Views\Home\" + folder);

			service.TranspileSingle(Path.GetFileName(fileName));
		}

		[Test]
		public void DoAll()
		{
			var service = new TranspilerService(@"C:\Work\aspclassiccompiler\aspclassiccompiler\AspWebApp\",
				@"C:\Work\aspclassiccompiler\aspclassiccompiler\MvcTestApp\Views\Home\");

			var errorCount = service.ParseAllFiles();
			service.IdentifyIncludes();
			
			foreach (var page in service.IncludePages)
			{
				Console.WriteLine(page);
			}

			service.TranspileIncludes(@"..\..\Includes", "Includes", "Includes.IncludesBase");

			var count = service.TranspileValidPages();
			Console.WriteLine($"{count} valid pages transpiled.");
			Console.WriteLine($"Files with errors: {errorCount}");
		}
	}
}
