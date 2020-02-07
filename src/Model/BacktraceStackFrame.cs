using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using System;
using System.Diagnostics;

namespace Backtrace.Unity.Model
{
    public class BacktraceStackFrame
    {
        /// <summary>
        /// Function where exception occurs
        /// </summary>
        [JsonProperty(PropertyName = "funcName")]
        public string FunctionName = "unknown";

        /// <summary>
        /// Line number in source code where exception occurs
        /// </summary>
        [JsonProperty(PropertyName = "line")]
        public int Line;

        /// <summary>
        /// IL Offset
        /// </summary>
        [JsonProperty(PropertyName = "il")]
        public int? Il;

        /// <summary>
        /// PBD Unique identifier
        /// </summary>
        [JsonProperty(PropertyName = "metadata_token")]
        public int? MemberInfo;


        /// <summary>
        /// Full path to source code where exception occurs
        /// </summary>
        [JsonIgnore]
        public string SourceCodeFullPath;

        /// <summary>
        /// Column number in source code where exception occurs
        /// </summary>
        [JsonProperty(PropertyName = "column")]
        public int Column;

        /// <summary>
        /// Address of the stack frame
        /// </summary>
        [JsonProperty(PropertyName = "address")]
        public int? ILOffset;

        /// <summary>
        /// Source code file name where exception occurs
        /// </summary>
        [JsonProperty(PropertyName = "sourceCode")]
        public string SourceCode;

        public BacktraceJObject ToJson()
        {
            var stackFrame = new BacktraceJObject
            {
                ["funcName"] = FunctionName,
                ["line"] = Line,
                ["il"] = Il,
                ["metadata_token"] = MemberInfo,
                ["column"] = Column,
                ["address"] = ILOffset,
                ["library"] = Library
            };
            //todo: source code information

            return stackFrame;
        }
        public static BacktraceStackFrame FromJson(string json)
        {
            var @object = BacktraceJObject.Parse(json);
            return new BacktraceStackFrame()
            {
                FunctionName = @object.Value<string>("funcName"),
                Line = @object.Value<int>("line"),
                Il = @object.Value<int?>("il"),
                MemberInfo = @object.Value<int?>("metadata_token"),
                Column = @object.Value<int>("column"),
                ILOffset = @object.Value<int?>("address"),
                SourceCode = @object.Value<string>("sourceCode"),
                Library = @object.Value<string>("library")
            };
        }
        /// <summary>
        /// Library name where exception occurs
        /// </summary>
        [JsonProperty(PropertyName = "library")]
        public string Library;


        internal static BacktraceStackFrame Deserialize(BacktraceJObject frame)
        {
            return new BacktraceStackFrame()
            {
                FunctionName = frame.Value<string>("funcName"),
                Line = frame.Value<int>("line"),
                Il = frame.Value<int?>("il"),
                MemberInfo = frame.Value<int?>("metadata_token"),
                Column = frame.Value<int>("column"),
                Library = frame.Value<string>("library"),
                ILOffset = frame.Value<int?>("address"),
            };
        }

        public BacktraceStackFrame()
        { }

        public BacktraceStackFrame(StackFrame frame, bool generatedByException)
        {
            if (frame == null || frame.GetMethod() == null)
            {
                return;
            }
            SourceCodeFullPath = frame.GetFileName();
            FunctionName = GetMethodName(frame);
            Line = frame.GetFileLineNumber();
            Il = frame.GetILOffset();
            ILOffset = Il;

            Library = string.IsNullOrEmpty(SourceCodeFullPath) ? frame.GetMethod()?.DeclaringType.ToString() : SourceCodeFullPath;

            SourceCode = generatedByException
                    ? Guid.NewGuid().ToString()
                    : string.Empty;
            Column = frame.GetFileColumnNumber();
            try
            {
                MemberInfo = frame.GetMethod()?.MetadataToken;
            }
            catch (InvalidOperationException)
            {
                //metadata token in some situations can throw Argument Exception. Plase check property definition to leran more about this behaviour
            }
        }



        /// <summary>
        /// Generate valid name for current stack frame.
        /// </summary>
        /// <returns>Valid method name in stack trace</returns>
        private string GetMethodName(StackFrame frame)
        {
            var method = frame.GetMethod();
            var methodName = method.Name.StartsWith(".") ? method.Name.Substring(1, method.Name.Length - 1) : method.Name;
            string fullMethodName = $"{method?.DeclaringType.ToString()}.{methodName}()";
            return fullMethodName;
        }
    }
}
