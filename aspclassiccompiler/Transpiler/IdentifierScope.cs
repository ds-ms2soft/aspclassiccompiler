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
		private readonly string _scopePrefix;
		public IdentifierScope ParentScope { get; }

		private readonly List<string> _undefinedIdentifiers = new List<string>();

		private readonly Dictionary<string, string> _identifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, string> GlobalIdentifiers = new Dictionary<string, string>
		{
			{ "Server", "Server" }, { "Request", "Request" }, { "Response", "Response" }, {"Session", "Session"},
			{ "Application", "Application" }, { "Err", "Err" }, { "nErr", "nErr" }, { "Now", "DateTime.Now" }
		};

		public IdentifierScope(IdentifierScope parentScope, string scopePrefix = null)
		{
			_scopePrefix = scopePrefix != null ? scopePrefix + "." : null;
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

		public void Define(string name, string actual = null)
		{
			_identifiers[name] = actual ?? name;
		}

		public string GetIdentifier(string name, bool allowUndefined)
			=> GetIdentifier(name, allowUndefined, true);

		private string GetIdentifier(string name, bool defineIfMissing, bool throwIfNotFound)
		{
			if (_identifiers.TryGetValue(name, out var identifier))
			{
				return identifier;
			}
			else if (VBScriptGenerator.IsBuiltInConstants(name) || VBScriptGenerator.IsBuiltInFunction(name))
			{
				return name;
			}
			else
			{
				string rv = null;
				if (ParentScope != null)
				{
					rv = ParentScope.GetIdentifier(name, false, false);
				}

				if (rv == null)
				{
					if (defineIfMissing)
					{
						_undefinedIdentifiers.Add(name);
						Define(name);
						rv = name;
					}
					else if (throwIfNotFound)
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
	}
}
