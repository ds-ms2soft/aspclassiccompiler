using NUnit.Framework;
using System;
using System.Text.RegularExpressions;

namespace Transpiler
{
	[TestFixture] //just for convenience while developing
	public class Program
	{
		static void Main(string[] args)
		{
			new Program().TranspileAll();
		}

		private static Ms2Transpiler BuildService()
		{
			var service = new Ms2Transpiler();
			service.IgnoreFile(new Regex("dynamic_array.asp$", RegexOptions.IgnoreCase))
				.IgnoreFile(new Regex(@"Session\\MS2DotNetSession.asp", RegexOptions.IgnoreCase));
			return service;
		}
		/*
		 * TODO list:
		 *
		 * -Convert ADO commands to proxy objects
		 * -security.vb /ref_path isn't properly being hoisted (asset_archive_delete.asp)");
		 * -Handle some include files that are actually ASP, but don't have asp extensions (C:\source\TDMS\TCDS.Web\ajax\functions.asp.js)
		 * -Some of those includes don't have ASP code, but do have HTML code (script tags) (C:\source\TDMS\TCDS.Web\ajax\functions_ttds.js) We could just transpile those to, or could convert them to normal JS script includes?
		 */
		[Test]
		public void TranspileAll()
		{
			var service = BuildService();

			var errorCount = service.ParseAllFiles();

			if (errorCount > 0)
			{
				Console.WriteLine("Errors found in files:");
				foreach (var error in service.GetErrors())
				{
					Console.WriteLine(error);
				}
			}

			service.IdentifyIncludes();

			var count = service.TranspileValidPages();
			Console.WriteLine($"{count} valid pages transpiled.");
			Console.WriteLine($"Files with errors: {errorCount}");
		}

		[Test]
		public void OutputInvalid()
		{
			var service = BuildService();

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
