using Backtrace.Newtonsoft.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backtrace.Unity.Model
{
    public class BacktraceSourceCode
    {
        public string Id = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Text";
        public string Title { get; set; } = "Log File";

        public bool HighlightLine = false;
        public string Text { get; set; }

        internal BacktraceJObject ToJson()
        {
            var json = new BacktraceJObject();
            json[Id.ToString()] = new BacktraceJObject()
            {
                ["id"] = Id,
                ["type"] = Type,
                ["title"] = Title,
                ["highlightLine"] = HighlightLine,
                ["text"] = Text,
            };

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
                Text = rawSourceCode.Value<string>("text"),
                Title = rawSourceCode.Value<string>("title"),
                Type = rawSourceCode.Value<string>("type")
            };
            return sourceCode;
        }
    }
}
