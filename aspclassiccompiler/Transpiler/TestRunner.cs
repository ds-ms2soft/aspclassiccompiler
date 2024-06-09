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
		[TestCase("Simple\\Includes.asp")]
		[TestCase("Components\\BrowserCap.asp")]
		public void TranspileOne(string fileName)
		{
			var folder = Path.GetDirectoryName(fileName);
			var service = new TranspilerService(@"C:\Work\aspclassiccompiler\aspclassiccompiler\AspWebApp\" + folder,
				@"C:\Work\aspclassiccompiler\aspclassiccompiler\MvcTestApp\Views\Home\");

			service.Transpile(Path.GetFileName(fileName));
		}

		[Test]
		public void DoAll()
		{
			var service = new TranspilerService(@"C:\Work\aspclassiccompiler\aspclassiccompiler\AspWebApp\",
				@"C:\Work\aspclassiccompiler\aspclassiccompiler\MvcTestApp\Views\Home\");

			var errorCount = service.ParseAllFiles();
			Console.WriteLine($"Files with errors: {errorCount}");
			service.IdentifyIncludes();

			foreach (var page in service.IncludePages)
			{
				Console.WriteLine(page);
			}

			service.TranspileIncludes(@"..\..\Includes", "Includes", "Includes.IncludesBase");
		}
	}
}
