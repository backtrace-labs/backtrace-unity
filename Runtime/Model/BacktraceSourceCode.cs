using Backtrace.Newtonsoft.Linq;
using System;
using System.Linq;
using System.Net.Mime;

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

        internal static BacktraceSourceCode Deserialize(JToken token)
        {
            if (token == null)
            {
                return null;
            }
            var data = token.FirstOrDefault();
            if (data == null)
            {
                return null;
            }
            var rawSourceCode = data.FirstOrDefault();
            var sourceCode = new BacktraceSourceCode()
            {
                Id = rawSourceCode.Value<string>("id"),
                Text = rawSourceCode.Value<string>("text")
            };
            return sourceCode;
        }
    }
}
