using System;
using System.IO;

namespace Transpiler
{
	public class RazorWriter
	{
		public enum States
		{
			Literal,
			Code
		}

		public States CurrentState { get; private set; } = States.Literal;
		private int _codeIndentationLevel = 0;

		private readonly StreamWriter _underlying;

		public RazorWriter(StreamWriter underlying)
		{
			_underlying = underlying;
		}

		public void WriteLiteral(string literal)
		{
			if (CurrentState != States.Literal)
			{
				_underlying.WriteLine(Environment.NewLine + "End Code");
				CurrentState = States.Literal;
			}
			_underlying.Write(literal);
		}

		//Note: could improve this to respect the initial indentation level of new code blocks (based on the tabs output in the last literal
		//Note: could improve this to collapse code blocks that are only separated by whitespace. Not sure how much I care, but it would look better. It would also change the rendered output, maybe for the better?
		public void WriteCode(string code, bool onNewLine)
		{
			if (CurrentState != States.Code && code.StartsWith("@"))
			{
				//render this inline and don't start a code block, ignore onNewLine
				WriteLiteral(code);
			}
			else
			{
				if (CurrentState != States.Code)
				{

					_underlying.Write("@Code");
					CurrentState = States.Code;
					_codeIndentationLevel = 1;
				}

				if (onNewLine)
				{
					_underlying.Write(Environment.NewLine);
					_underlying.Write(new String('\t', _codeIndentationLevel));
				}

				_underlying.Write(code);
			}
		}

		public IDisposable BeginBlock()
		{
			return new Indenter(this);
		}

		private class Indenter : IDisposable
		{
			private readonly RazorWriter _writer;

			public Indenter(RazorWriter writer)
			{
				_writer = writer;
				_writer._codeIndentationLevel++;
			}

			public void Dispose()
			{
				_writer._codeIndentationLevel--;
			}
		}
	}

}
