using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Transpiler
{
	public class RazorWriter: OutputWriter, IDisposable
	{
		public enum States
		{
			Literal,
			Code,
			Functions,
			SubOrFunctionBody,
			Disposing
		}

		private Stack<States> _state = new Stack<States>();
		public States CurrentState => _state.Peek();

		private readonly StreamWriter _underlying;

		public RazorWriter(StreamWriter underlying)
		{
			_underlying = underlying;
			_state.Push(States.Literal);
		}

		public override void WriteLiteral(string literal)
		{
			TransitionToState(States.Literal);
			_underlying.Write(literal);
		}

		//Note: could improve this to respect the initial indentation level of new code blocks (based on the tabs output in the last literal
		//Note: could improve this to collapse code blocks that are only separated by whitespace. Not sure how much I care, but it would look better. It would also change the rendered output, maybe for the better?
		public override void WriteCode(string code, bool onNewLine)
		{
			if (CurrentState == States.Literal && code.StartsWith("@"))
			{
				//render this inline and don't start a code block, ignore onNewLine
				WriteLiteral(code);
			}
			else if (CurrentState == States.Literal && String.IsNullOrWhiteSpace(code))
			{
				//ignore empty statement output
			}
			else if (CurrentState == States.SubOrFunctionBody)
			{
				if (onNewLine)
				{
					_underlying.Write(Environment.NewLine);
					_underlying.Write(GetIndentation());
				}

				if (code.StartsWith("@Html.Raw", StringComparison.OrdinalIgnoreCase)) //HtmlHelper not available in a function or sub, instead use our "Raw" base class method 
				{
					code = code.Substring("@Html.".Length);
				}

				_underlying.Write(code);
				if (code.StartsWith("End Function", StringComparison.OrdinalIgnoreCase) ||
				    code.StartsWith("End Sub", StringComparison.OrdinalIgnoreCase))
				{
					TransitionToState(States.Functions); //not in a method body any more.
				}
			}
			else 
			{

				if (code.StartsWith("Sub ", StringComparison.OrdinalIgnoreCase) ||
				    code.StartsWith("Function ", StringComparison.OrdinalIgnoreCase))
				{
					TransitionToState(States.Functions); //open a @functions section
					TransitionToState(States.SubOrFunctionBody); //now we are in the body (well we will be soon, and setting it now doesn't matter.
				}
				else
				{
					TransitionToState(States.Code);
				}

				if (onNewLine)
				{
					_underlying.Write(Environment.NewLine);
					_underlying.Write(GetIndentation());
				}

				_underlying.Write(code);
			}
		}

		private void TransitionToState(States newState)
		{
			var current = CurrentState;
			if (newState != current)
			{
				//Close the current thing
				switch (current)
				{
					case States.Literal:
						_state.Pop();
						break;
					case States.Code:
						_underlying.WriteLine(Environment.NewLine + "End Code");
						_state.Pop();
						break;
					case States.Functions:
						if (newState != States.SubOrFunctionBody)
						{
							_underlying.WriteLine(Environment.NewLine + "End Functions");
							_state.Pop();
						}
						break;
					case States.SubOrFunctionBody:
						_state.Pop();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				States? stateToSet = newState;
				switch (newState)
				{
					case States.Literal:
						break;
					case States.Code:
						_underlying.Write("@Code");
						_codeIndentationLevel = 1;
						break;
					case States.Functions:
						if (current != States.SubOrFunctionBody)
						{
							_underlying.Write("@Functions");
						}
						else
						{
							stateToSet = null;
						}
						_codeIndentationLevel = 1;
						break;
					case States.SubOrFunctionBody:
						break;
					case States.Disposing:
						stateToSet = null;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
				}

				if (stateToSet != null)
				{
					_state.Push(stateToSet.Value);
				}
			}
		}

		public void Dispose()
		{
			while (_state.Count > 0)
			{
				TransitionToState(States.Disposing); //This will close any block we are in.
			}
			_underlying?.Dispose();
		}
	}

}
