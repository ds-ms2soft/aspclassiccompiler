using System.Collections.Generic;
// 
// Visual Basic .NET Parser
// 
// Copyright (C) 2005, Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// 

using System.Xml;

namespace Dlrsoft.VBScript.Parser
{

	public class ErrorXmlSerializer
	{
		private readonly XmlWriter Writer;

		public ErrorXmlSerializer(XmlWriter Writer)
		{
			this.Writer = Writer;
		}

		private void Serialize(Span Span)
		{
			Writer.WriteAttributeString("startLine", Span.Start.Line.ToString());
			Writer.WriteAttributeString("startCol", Span.Start.Column.ToString());
			Writer.WriteAttributeString("endLine", Span.Finish.Line.ToString());
			Writer.WriteAttributeString("endCol", Span.Finish.Column.ToString());
		}

		public void Serialize(SyntaxError SyntaxError)
		{
			Writer.WriteStartElement(SyntaxError.Type.ToString());
			Serialize(SyntaxError.GeneratedSpan);
			Writer.WriteString(SyntaxError.ToString());
			Writer.WriteEndElement();
		}

		public void Serialize(List<SyntaxError> SyntaxErrors)
		{
			foreach (SyntaxError SyntaxError in SyntaxErrors)
				Serialize(SyntaxError);
		}

	}
}