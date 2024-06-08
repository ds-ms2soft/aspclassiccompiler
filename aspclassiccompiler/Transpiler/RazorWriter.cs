using System;
using System.IO;

namespace Transpiler
{
	internal class RazorWriter
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
			_underlying.WriteLine(literal);
		}

		public void WriteCode(string code, bool onNewLine)
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
