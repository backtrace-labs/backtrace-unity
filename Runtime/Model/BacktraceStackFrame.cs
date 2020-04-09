using Backtrace.Unity.Json;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Backtrace.Unity.Model
{
    public class BacktraceStackFrame
    {
        /// <summary>
        /// Function where exception occurs
        /// </summary>
        public string FunctionName;

        /// <summary>
        /// Line number in source code where exception occurs
        /// </summary>
        public int Line;

        /// <summary>
        /// IL Offset
        /// </summary>
        public int? Il;

        /// <summary>
        /// PBD Unique identifier
        /// </summary>
        public int? MemberInfo;


        /// <summary>
        /// Full path to source code where exception occurs
        /// </summary>
        public string SourceCodeFullPath;

        /// <summary>
        /// Column number in source code where exception occurs
        /// </summary>
        public int Column;

        /// <summary>
        /// Address of the stack frame
        /// </summary>
        public int? ILOffset;

        /// <summary>
        /// Source code file name where exception occurs
        /// </summary>
        public string SourceCode;

        /// <summary>
        /// Assembly name
        /// </summary>
        public string Assembly;

        /// <summary>
        /// Invalid stack frame
        /// </summary>
        public bool InvalidFrame { get; set; }
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
                ["library"] = Library,
                ["assembly"] = Assembly
            };
            //todo: source code information

            return stackFrame;
        }

        /// <summary>
        /// Library name where exception occurs
        /// </summary>

        public string Library;


        public BacktraceStackFrame()
        { }

        public BacktraceStackFrame(StackFrame frame, bool generatedByException)
        {
            if (frame == null)
            {
                InvalidFrame = true;
                return;
            }
            var method = frame.GetMethod();
            if (method == null)
            {
                InvalidFrame = true;
                return;
            }
            SourceCodeFullPath = frame.GetFileName();

            FunctionName = GetMethodName(method);
            Line = frame.GetFileLineNumber();
            Il = frame.GetILOffset();
            ILOffset = Il;
            Assembly = method.DeclaringType.Assembly.GetName().Name ?? "unknown";
            Library = string.IsNullOrEmpty(SourceCodeFullPath) ? method.DeclaringType.ToString() : SourceCodeFullPath;

            SourceCode = generatedByException
                    ? Guid.NewGuid().ToString()
                    : string.Empty;

            Column = frame.GetFileColumnNumber();
            try
            {
                MemberInfo = method.MetadataToken;
            }
            catch (InvalidOperationException)
            {
                //metadata token in some situations can throw Argument Exception. Plase check property definition to leran more about this behaviour
            }
            InvalidFrame = false;
        }



        /// <summary>
        /// Generate valid name for current stack frame.
        /// </summary>
        /// <returns>Valid method name in stack trace</returns>
        private string GetMethodName(MethodBase method)
        {
            var methodName = method.Name.StartsWith(".") ? method.Name.Substring(1, method.Name.Length - 1) : method.Name;
            string fullMethodName = string.Format("{0}.{1}()", method.DeclaringType == null ? null : method.DeclaringType.ToString(), methodName);
            return fullMethodName;
        }
    }
}
