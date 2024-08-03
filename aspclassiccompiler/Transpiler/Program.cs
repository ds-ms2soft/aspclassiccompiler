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

		[Test]
		public void OutputInvalid()
		{
			var service = new Ms2Transpiler();

			var errorCount = service.ParseAllFiles();
			Console.WriteLine($"Invalid pages: {errorCount}");
			service.VisitInvalid((path, unit) =>
			{
				Console.WriteLine(path);
				foreach (var error in unit.ErrorTable)
				{
					Console.WriteLine(error.ToString());
				}
			});
		}

		[TestCase("C:\\source\\TDMS\\TCDS.Web\\tdetail.asp")]
		public void ParseOne(string path)
		{
			var unit = TranspileUnit.Parse(path);
			Assert.That(unit.HasErrors, Is.False);
		}
	}
}
