using System;

namespace Transpiler
{
	public abstract class OutputWriter
	{
		public int CodeIndentationLevel { get; protected set; }= 0;

		public IDisposable BeginBlock()
		{
			return new Indenter(this);
		}

		protected string GetIndentation() => CodeIndentationLevel > 0 ? new string('\t', CodeIndentationLevel) : "";

		public abstract void WriteLiteral(string text);
		public abstract void WriteCode(string text, bool onNewLine);

		private class Indenter : IDisposable
		{
			private readonly OutputWriter _writer;

			public Indenter(OutputWriter writer)
			{
				_writer = writer;
				_writer.CodeIndentationLevel++;
			}

			public void Dispose()
			{
				_writer.CodeIndentationLevel--;
			}
		}
	}
}