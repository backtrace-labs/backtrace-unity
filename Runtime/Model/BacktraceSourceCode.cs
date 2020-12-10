using Backtrace.Unity.Json;
using System;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Source code panel - in Unity integration, Backtrace-Unity stores unity Engine
    /// logs in the Source Code integration.
    /// </summary>
    public class BacktraceSourceCode
    {
        internal static string SOURCE_CODE_PROPERTY = "main";
        /// <summary>
        /// Default source code type
        /// </summary>
        public readonly string Type = "Text";
        /// <summary>
        /// Default source code title
        /// </summary>
        public readonly string Title = "Log File";

        /// <summary>
        /// Required source code option -  we don't want to hightlight any line
        /// </summary>
        public readonly bool HighlightLine = false;

        /// <summary>
        /// Unity engine text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Convert Source code integration into JSON object
        /// </summary>
        /// <returns>Source code BacktraceJObject</returns>
        internal BacktraceJObject ToJson()
        {
            var json = new BacktraceJObject();
            var sourceCode = new BacktraceJObject();
            sourceCode["id"] = SOURCE_CODE_PROPERTY;
            sourceCode["type"] = Type;
            sourceCode["title"] = Title;
            sourceCode["highlightLine"] = HighlightLine;
            sourceCode["text"] = Text;

            json[SOURCE_CODE_PROPERTY] = sourceCode;
            return json;
        }
    }
}
