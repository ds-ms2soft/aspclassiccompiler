using NUnit.Framework;
using System;

namespace Transpiler
{
	[TestFixture] //just for convenience while developing
	public class Program
	{
		static void Main(string[] args)
		{
			new Program().TranspileAll();
		}

		[Test]
		public void TranspileAll()
		{
			var service = new Ms2Transpiler();

			var errorCount = service.ParseAllFiles();
			service.IdentifyIncludes();

			var count = service.TranspileValidPages();
			Console.WriteLine($"{count} valid pages transpiled.");
			Console.WriteLine($"Files with errors: {errorCount}");
		}
	}
}
