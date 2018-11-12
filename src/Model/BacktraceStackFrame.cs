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
        public string FunctionName;

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
            var stackFrame = new BacktraceJObject();
            stackFrame["funcName"] = FunctionName;
            stackFrame["line"] = Line;
            stackFrame["il"] = Il;
            stackFrame["metadata_token"] = MemberInfo;
            stackFrame["column"] = Column;
            stackFrame["address"] = ILOffset;
            
            //todo: source code information

            return stackFrame;
        }
        /// <summary>
        /// Library name where exception occurs
        /// </summary>
        [JsonProperty(PropertyName = "library")]
        public string Library;


        public BacktraceStackFrame()
        { }

        public BacktraceStackFrame(StackFrame frame, bool generatedByException)
        {
            if (frame == null || frame.GetMethod() == null)
            {
                return;
            }
            FunctionName = GetMethodName(frame);
            Line = frame.GetFileLineNumber();
            Il = frame.GetILOffset();
            ILOffset = Il;
            SourceCodeFullPath = frame.GetFileName();

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
            string methodName = method.Name;
            return methodName;
        }
    }
}
