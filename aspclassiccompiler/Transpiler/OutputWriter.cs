using System;

namespace Transpiler
{
	public abstract class OutputWriter
	{
		protected int _codeIndentationLevel = 0;

		public IDisposable BeginBlock()
		{
			return new Indenter(this);
		}

		protected string GetIndentation() => _codeIndentationLevel > 0 ? new string('\t', _codeIndentationLevel) : "";

		public abstract void WriteLiteral(string text);
		public abstract void WriteCode(string text, bool onNewLine);

		private class Indenter : IDisposable
		{
			private readonly OutputWriter _writer;

			public Indenter(OutputWriter writer)
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