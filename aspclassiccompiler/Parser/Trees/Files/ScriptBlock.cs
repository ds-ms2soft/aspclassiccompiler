using System.Collections.Generic;

namespace Dlrsoft.VBScript.Parser
{
	// LC Represent a script file
	public class ScriptBlock : Tree
	{

		private readonly StatementCollection _statements;

		/// <summary>
    /// The declarations in the file.
    /// </summary>
		public StatementCollection Statements
		{
			get
			{
				return _statements;
			}
		}

		/// <summary>
    /// Constructs a new file parse tree.
    /// </summary>
    /// <param name="statements">The statements in the file.</param>
    /// <param name="span">The location of the tree.</param>
		public ScriptBlock(StatementCollection statements, Span span) : base(TreeType.ScriptBlock, span)
		{

			SetParent(statements);

			_statements = statements;
		}

		protected override void GetChildTrees(IList<Tree> childList)
		{
			AddChild(childList, _statements);
		}
	}
}