using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Scripting;
using System.Text;
using Dlrsoft.VBScript.Parser;

namespace Dlrsoft.VBScript.Compiler
{
    public class VBScriptSourceMapper : ISourceMapper
    {
        //Here the range is start line number and the end line number
        private List<SourceSpan> _spans = new List<SourceSpan>(); //use ArrayList so that we can use the binary search method
        private IDictionary<SourceSpan, DocSpan> _mappings = new Dictionary<SourceSpan, DocSpan>();
        static private SpanComparer _comparer = new SpanComparer();

        public VBScriptSourceMapper() {}

        public void AddMapping(SourceSpan generatedSpan, DocSpan span)
        {
            _spans.Add(generatedSpan);
            _mappings[generatedSpan] = span;
        }

        public DocSpan Map(Span span)
        {
            var start = new SourceLocation(span.Start.Index, span.Start.Line, span.Start.Column);
            var end = new SourceLocation(span.Finish.Index, span.Finish.Line, span.Finish.Column);
            return Map(new SourceSpan(start, end));
        }
        #region ISourceMapper Members

        public DocSpan Map(SourceSpan span)
        {
            int generatedStartLine = span.Start.Line;
            int index = _spans.BinarySearch(span, _comparer);
            SourceSpan containingSpan = (SourceSpan)_spans[index];
            DocSpan docSpan = _mappings[containingSpan];

            int rawStartIndex = docSpan.Span.Start.Index + span.Start.Index - containingSpan.Start.Index;
            int lineOffSet = generatedStartLine - containingSpan.Start.Line;
            int rawStartLine = docSpan.Span.Start.Line + lineOffSet;
            int rawStartColumn;
            if (lineOffSet == 0)
            {
                rawStartColumn = docSpan.Span.Start.Column + span.Start.Column;
            }
            else
            {
                rawStartColumn = span.Start.Column;
            }
            int lines = span.End.Line - generatedStartLine;
            int rawEndIndex = rawStartIndex + span.End.Index + span.Start.Index;
            int rawEndLine = rawStartLine + lines;
            int rawEndColumn;
            if (lines == 0)
            {
                rawEndColumn = rawStartColumn + span.End.Column - span.Start.Column;
            }
            else
            {
                rawEndColumn = span.End.Column;
            }
            return new DocSpan(docSpan.Uri,
                new SourceSpan(
                    new SourceLocation(rawStartIndex, rawStartLine, rawStartColumn),
                    new SourceLocation(rawEndIndex, rawEndLine, rawEndColumn)
                )
            );
        }
        #endregion
    }
}
