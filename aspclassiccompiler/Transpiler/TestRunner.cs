using System.IO;
using NUnit.Framework;

namespace Transpiler
{
	[NUnit.Framework.TestFixture]
	public class TestRunner
	{
		[TestCase("components\\FileSystem.asp")]
		[TestCase("Database\\SimpleQuery.asp")]
		public void TranspileOne(string fileName)
		{
			var folder = Path.GetDirectoryName(fileName);
			var service = new TranspilerService(@"C:\Work\aspclassiccompiler\aspclassiccompiler\AspWebApp\" + folder,
				@"C:\Work\aspclassiccompiler\aspclassiccompiler\MvcTestApp\Views\Home\");

			service.Transpile(Path.GetFileName(fileName));
		}
	}
}
