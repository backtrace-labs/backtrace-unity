using Backtrace.Unity.Json;
using System;

namespace Backtrace.Unity.Model
{
    public class BacktraceSourceCode
    {
        public string Id = Guid.NewGuid().ToString();
        public readonly string Type = "Text";
        public readonly string Title = "Log File";

        public bool HighlightLine = false;
        public string Text { get; set; }

        public BacktraceSourceCode()
        {
            Type = "Text";
            Title = "Log File";
        }

        internal BacktraceJObject ToJson()
        {
            var json = new BacktraceJObject();
            var sourceCode = new BacktraceJObject();
            sourceCode["id"] = Id;
            sourceCode["type"] = Type;
            sourceCode["title"] = Title;
            sourceCode["highlightLine"] = HighlightLine;
            sourceCode["text"] = Text;

            json[Id.ToString()] = sourceCode;
            return json;
        }
    }
}
