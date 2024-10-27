using System;
using System.Collections.Generic;
using Dlrsoft.VBScript.Compiler;

namespace Transpiler
{
	/// <summary>
	/// This class holds the identifiers (functions and variables) that are defined in the current scope (page, function, etc.)
	/// </summary>
	public class IdentifierScope
	{
		public enum UndefinedHandling
		{
			Throw = 0,
			Ignore = 1,
			AllowAndDefine = 2
		}

		public IdentifierScope ParentScope { get; }
		public bool IsGlobal => ParentScope == null;

		
		private readonly Dictionary<string, string> _identifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, string> GlobalIdentifiers = new Dictionary<string, string>
		{
			{ "Server", "Server" }, { "Request", "Request" }, { "Response", "Response" }, {"Session", "Session"},
			{ "Application", "Application" }, { "Err", "Err" }, { "nErr", "nErr" }, 
			{ "Now", "DateTime.Now" },
			{"Timer", "DateAndTime.Timer"}
		};

		public IdentifierScope(IdentifierScope parentScope)
		{
			ParentScope = parentScope;
		}

		public static IdentifierScope MakeGlobal()
		{
			var rv = new IdentifierScope(null);
			foreach (var keyValuePair in GlobalIdentifiers)
			{
				rv._identifiers.Add(keyValuePair.Key, keyValuePair.Value);
			}
			return rv;
		}

		public void Define(string name)
			=> Define(name, name);

		public virtual void Define(string name, string actual)
		{
			_identifiers[name] = actual;
		}

		public string GetIdentifier(string name, UndefinedHandling undefined = UndefinedHandling.Throw)
			// ReSharper disable once ConditionalTernaryEqualBranch
			=> GetIdentifier(name, undefined, out var found) ? found : found;

		private bool GetIdentifier(string name, UndefinedHandling undefined, out string found)
		{
			if (_identifiers.TryGetValue(name, out found))
			{
				return true;
			}
			else
			{
				var rv = false;
				if (ParentScope != null)
				{
					rv = ParentScope.GetIdentifier(name, UndefinedHandling.Ignore, out found);
				}

				if (!rv)
				{
					if (VBScriptGenerator.IsBuiltInConstants(name) || VBScriptGenerator.IsBuiltInFunction(name))
					{
						found = name;
						rv = true;
					}
				}

				if (!rv)
				{
					if (undefined == UndefinedHandling.Ignore)
					{
						//Name is used in a context that we can't validate. We want to do our scope search to support substitutions, but if not found, we'll just return the name.
						found = name;
						rv = false;
					}
					else if (_onUndefinedVariable != null)
					{
						_onUndefinedVariable?.Handle(this, name);
						rv = _identifiers.TryGetValue(name, out found);
					}
					else if (undefined == UndefinedHandling.AllowAndDefine)
					{
						Define(name);
						found = name;
						rv = true;
					}
					else
					{
						throw new NotSupportedException($"Identifier not found: {name}");
					}
				}

				return rv;
			}
		}

		/// <summary>
		/// Forwards a reference to anything in the child scope to reference the same identifier on the variable name given.
		/// </summary>
		public void MapToVariable(IdentifierScope childScope, string variableName)
		{
			foreach (var kvp in childScope._identifiers)
			{
				if (!GlobalIdentifiers.ContainsKey(kvp.Key))
				{
					Define(kvp.Key, $"{variableName}.{kvp.Value}");
				}
			}
		}

		public bool TryGetIdentifier(string paramName, out string scopedName)
			=> GetIdentifier(paramName, UndefinedHandling.Ignore, out scopedName);

		public VariableDefinitionHandling WithVariableDefinitionHandling(Action<IdentifierScope, string, Action> handle)
		{
			var old = _onUndefinedVariable;
			return _onUndefinedVariable = new VariableDefinitionHandling(handle, old, () => _onUndefinedVariable = old);
		}

		private VariableDefinitionHandling _onUndefinedVariable = null;

		public class VariableDefinitionHandling : IDisposable
		{
			private readonly Action<IdentifierScope, string, Action> _handle;
			private readonly VariableDefinitionHandling _prior;
			private readonly Action _onDispose;

			public VariableDefinitionHandling(Action<IdentifierScope, string, Action> handle, VariableDefinitionHandling prior, Action onDispose)
			{
				_handle = handle;
				_prior = prior;
				_onDispose = onDispose;
			}

			private void PassUp(IdentifierScope scope, string name)
			{
				if (_prior != null)
				{
					_prior.Handle(scope, name);
				}
				else
				{
					throw new Exception($"Unhandled undefined variable: {name}");
				}
			}

			public void Handle(IdentifierScope scope, string name)
			{
				_handle(scope, name, () => PassUp(scope, name));
			}

			public void Dispose()
			{
				_onDispose();
			}
		}
	}

	public class IdentifierScopeWithBlock: IdentifierScope
	{
		public string Source { get; }

		public IdentifierScopeWithBlock(string source, IdentifierScope parentScope) : base(parentScope)
		{
			Source = source;
		}

		public override void Define(string name, string actual)
		{
			throw new NotSupportedException();
		}
	}
}
