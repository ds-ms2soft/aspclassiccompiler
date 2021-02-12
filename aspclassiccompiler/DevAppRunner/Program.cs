using System.IO;
using System.Text.RegularExpressions;
using Dlrsoft.Asp;

namespace DevAppRunner
{
	class Program
	{
		static void Main(string[] args)
		{
			const string pagePath = @"C:\Work\MS2\Source\TDMS\TCDS.Web\tdetail.asp";
				// @"C:\Work\MS2\Source\TDMS\TCDS.Web\ajax\tcds_tdetail_gcs.asp";
			var host = new AspHost(new AspHostConfiguration() {});
			AspPageDom page = new AspPageDom();
			//TODO: define some handlers for includes
			page.ProcessCode = ProcessCode;
			page.processPage(pagePath);
			//TODO: Can I process the code to pull out functions and page global variables?
			//TODO write to new file in new location
			string viewPath = @"C:\Work\MS2\Source\TDMS\TCDS.Web\Views\Home\" +
			                  Path.GetFileNameWithoutExtension(pagePath) + "_New.vbhtml";
			File.WriteAllText(viewPath, page.Code);
		}

		private static string ProcessCode(string codeBlock)
		{
			//if (codeBlock.Contains("DataConn.execute(sqlFCode)"))
			//{
			//	System.Diagnostics.Debugger.Break();
			//}
			//Remove "SET" keyword.
			codeBlock = Regex.Replace(codeBlock, @"(?<=[\s\n\t\r]+|^)set\s+(?=[a-zA-Z0-9]+\s*=)", "", RegexOptions.IgnoreCase);
			//Replace Response.Write with @Html.Raw NOTE: this only works for some code, not code within a @Function block
			codeBlock = Regex.Replace(codeBlock, @"response\.write\s*\((?<content>.*?)\)(?=\s*\r?\n)", "@Html.Raw($1)",
				RegexOptions.IgnoreCase);
			//One version for calls that already have surrounding (), and another for ones that don't (could make one fancy balancing regex, but this is easy)
			codeBlock = Regex.Replace(codeBlock, @"response\.write\s*(?<content>.*?)(?=\s*\r?\n)", "@Html.Raw($1)",
				RegexOptions.IgnoreCase);
			//TODO: Maybe translate "SET variable = nothing" to some sort of conditional dispose?
			//TODO: detected a comment that prevents "End Code" from working and insert a line break: @Code end if 'DisplaySection("notes") End Code
			return codeBlock;
		}
	}
}
