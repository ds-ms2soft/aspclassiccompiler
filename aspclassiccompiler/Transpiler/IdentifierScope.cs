using System;
using System.Collections.Generic;

namespace Transpiler
{
	/// <summary>
	/// This class holds the identifiers (functions and variables) that are defined in the current scope (page, function, etc.)
	/// </summary>
	public class IdentifierScope
	{
		private readonly string _scopePrefix;
		public IdentifierScope ParentScope { get; }

		private readonly Dictionary<string, string> _identifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public IdentifierScope(IdentifierScope parentScope, string scopePrefix = null)
		{
			_scopePrefix = scopePrefix != null ? scopePrefix + "." : null;
			ParentScope = parentScope;
		}

		public static IdentifierScope MakeGlobal()
		{
			var rv = new IdentifierScope(null);
			rv.Define("Server");
			rv.Define("Request");
			rv.Define("Response");
			rv.Define("Now", "DateTime.Now");
			return rv;
		}

		public void Define(string name, string actual = null)
		{
			_identifiers[name] = actual ?? name;
		}

		public string GetIdentifier(string name)
		{
			if (_identifiers.TryGetValue(name, out var identifier))
			{
				return identifier;
			}
			else
			{
				throw new NotImplementedException("search for identifier.");
			}
		}
	}
}
