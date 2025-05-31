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
		 * Maybe identify all undefined variables and update all pages to use OPTION EXPLICIT
		 * C:\source\TDMS\TCDS.Web\peakhour-data.asp is a pain because it uses variables from the page that includes it. Needs to be hoisted to inputs I guess, but it's TMC to I'm ignoring for now.
		 */
		//Left off here: Tdetail is ported, but can't be run. Wire up the route, and then start implementing the proxy methods? Also need to fix Server.CreateObject calls
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

			var count = service.TranspileValidPages("C:\\source\\TDMS\\TCDS.Web\\peakhour.asp", @"C:\source\TDMS\TCDS.Web\rpt_peakhour.asp"
				,@"C:\source\TDMS\TCDS.Web\rpt_tdetail_signal.asp" //used 3 times in the past 18 months, by oakland only
				, @"C:\source\TDMS\TCDS.Web\admin\f_docupload.asp" //uses upload.asp which is hard to port
				, @"C:\source\TDMS\TCDS.Web\admin\f_importfileupload.asp" //uses upload.asp which is hard to port
				, @"C:\source\TDMS\TCDS.Web\Admin\f_importfolder_parse.asp" //references a TMC file, used only 1 (by van for MORC), so likely dead.
				, @"C:\source\TDMS\TCDS.Web\Admin\f_process_jamar.asp" //not used in 18 months
				, @"C:\source\TDMS\TCDS.Web\Admin\f_tmcassigndetail.asp" //Skip TMC file.
				);
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
		[TestCase("C:\\source\\TDMS\\TCDS.Web\\tdetail_tcls.asp")]
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
