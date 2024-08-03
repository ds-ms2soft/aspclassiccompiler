using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dlrsoft.VBScript.Parser;

namespace Transpiler
{
	public class IncludeClassWriter: OutputWriter, IDisposable
	{
		private readonly StreamWriter _underlying;
		private string _constructor = "Public Sub New(hostPage As System.Web.Mvc.WebViewPage";

		public IncludeClassWriter(StreamWriter underlying)
		{
			_underlying = underlying;
		}
		public string Namespace { get; set; }
		public string ClassName { get; set; }
		public string BaseClass { get; set; }

		public void AddConstructorParam(string name, string type)
		{
			_constructorBody.Add($"Me.{name} = {name}");
			_fields.Add($"Private {name} As {type}");
			_constructor += $", {name} As {type}";
		}

		public void Render()
		{
			WriteLineWithIndent("Option Explicit Off");
			WriteLineWithIndent("Option Infer On");
			WriteLineWithIndent("Option Strict Off\r\n");

			WriteLineWithIndent($"Namespace {Namespace}");
			using (var _ = BeginBlock())
			{
				WriteLineWithIndent($"Public Class {ClassName}");
				using (var _2 = BeginBlock())
				{
					WriteLineWithIndent($"Inherits {BaseClass}");
					WriteLineWithIndent("");

					_constructor += ")";
					WriteLineWithIndent(_constructor);
					using (var _3 = BeginBlock())
					{
						WriteLineWithIndent("MyBase.New(hostPage)");
						WriteLinesWithIndent(_constructorBody);
					}
					WriteLineWithIndent("End Sub" + Environment.NewLine);
					
					//member fields
					WriteLinesWithIndent(_fields);

					//subs and methods.
					WriteLinesWithIndent(_SubsAndFunctions);

				}
				WriteLineWithIndent($"End Class");
			}
			WriteLineWithIndent($"End Namespace");
		}

		//public IDisposable StartMethodOrSub(string name, string startExpression)
		private void WriteLinesWithIndent(IEnumerable<string> lines)
		{
			foreach (var line in lines)
			{
				WriteLineWithIndent(line);
			}
		}
		private void WriteLineWithIndent(string line)
		{
			_underlying.Write(GetIndentation());
			_underlying.WriteLine(line);
		}


		public override void WriteLiteral(string text)
		{
			if (!String.IsNullOrWhiteSpace(text)) //Maybe ignore whitespace literals?? Probably just spacing between tags?
			{
				WriteCode("Raw(\"" + text.Replace("\"", "\"\"") + "\")", true);
			}
		}

		private List<string> _fields = new List<string>();
		private List<string> _constructorBody = new List<string>();
		private List<string> _SubsAndFunctions = new List<string>();

		private bool _isInSubOrFunction = false;

		public override void WriteCode(string text, bool onNewLine)
		{
			//Convert the code and track our state
			if (text.StartsWith("Sub", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Function"))
			{
				text = "Public " + text;
				_isInSubOrFunction = true;
			}

			if (text.StartsWith("@Html.Raw", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring("@Html.".Length); //Translate to Raw method call.
			}

			if (_isInSubOrFunction)
			{
				_SubsAndFunctions.Add(GetIndentation() + text);
			}
			/*I don't think I want to elevate everything to a public field, but how do I handle actual variables that are being exposed from headers to their parents?
			 else if (text.StartsWith("Const", StringComparison.OrdinalIgnoreCase) ||
			         text.StartsWith("Dim", StringComparison.OrdinalIgnoreCase) ||
			         text.StartsWith("ReDim", StringComparison.OrdinalIgnoreCase))
			{
				_fields.Add("Public " + text);
			}*/
			else if (String.IsNullOrWhiteSpace(text))
			{
				//Ignore this, probably don't care
			}
			else
			{
				_constructorBody.Add(GetIndentation() + text);
			}
		}

		public void Dispose()
		{
			Render();
		}

		public void AddIncludeVariable(string variableName, string className, IEnumerable<IncludeFileConstructorParameter> extraParams)
		{
			if (extraParams?.Any() == true)
			{
				throw new NotImplementedException();
			}
			_fields.Add($"Public ReadOnly {variableName} As {className}");
			_constructorBody.Add($"Me.{variableName} = New {className}(hostPage)");
		}
	}
}
