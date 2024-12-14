using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
				.IgnoreFile(new Regex(@"Session\\MS2DotNetSession.asp", RegexOptions.IgnoreCase))
				.IgnoreFile(new Regex(@"\\obj\\", RegexOptions.IgnoreCase));
			return service;
		}
		/*
		 * TODO list:
		 *
		 * -Convert ADO commands to proxy objects
		 * -security.vb /ref_path isn't properly being hoisted (asset_archive_delete.asp)");
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
		public void ListAllNonIncludePages()
		{
			var service = BuildService();

			service.ParseAllFiles();
			service.IdentifyIncludes();

			service.VisitAll(tuple =>
			{
				var (path, unit, isInclude) = tuple;
				if (!isInclude)
				{
					Console.WriteLine(",('" + path.Replace("C:\\source\\TDMS\\TCDS.Web\\", "/TCDS/").Replace("\\", "/") + "')");
				}
			});
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
		[TestCase("C:\\source\\TDMS\\TCDS.Web\\default.asp")]
		public void TranspileOne(string path)
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
			service.TranspileSingle(path, TranspileUnit.Parse(path));
		}

		[TestCase("C:\\source\\TDMS\\TCDS.Web\\search_fields_functions.asp")]
		[TestCase("C:\\source\\TDMS\\TCDS.Web\\phv.asp")]
		public void TranspileInclude(string path)
		{
			var service = BuildService();

			service.ParseAllFiles();

			service.IdentifyIncludes();
			service.EnsureIncludeTranspiled(path, new List<IncludeFileConstructorParameter>());
		}
	}
}
