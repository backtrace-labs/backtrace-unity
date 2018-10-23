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
        public string FunctionName { get; set; }

        /// <summary>
        /// Line number in source code where exception occurs
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// IL Offset
        /// </summary>
        public int? Il { get; set; }

        /// <summary>
        /// PBD Unique identifier
        /// </summary>
        public int? MemberInfo { get; set; }


        /// <summary>
        /// Full path to source code where exception occurs
        /// </summary>
        public string SourceCodeFullPath { get; set; }

        /// <summary>
        /// Column number in source code where exception occurs
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Address of the stack frame
        /// </summary>
        public int? ILOffset { get; set; }

        /// <summary>
        /// Source code file name where exception occurs
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// Library name where exception occurs
        /// </summary>
        public string Library { get; set; }

        internal Assembly FrameAssembly { get; set; }

        public BacktraceStackFrame()
        { }

        public BacktraceStackFrame(StackFrame frame, bool generatedByException, bool reflectionMethodName = true)
        {
            if (frame == null || frame.GetMethod() == null)
            {
                return;
            }
            FunctionName = GetMethodName(frame, reflectionMethodName);
            Line = frame.GetFileLineNumber();
            Il = frame.GetILOffset();
            ILOffset = Il;
            SourceCodeFullPath = frame.GetFileName();

            Debug.WriteLine("[BacktraceStackFrame]::BacktraceStackFrame - getting assembly");
            FrameAssembly = frame.GetMethod().DeclaringType?.Assembly;
            Library = FrameAssembly?.GetName()?.Name ?? "unknown";
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
        private string GetMethodName(StackFrame frame, bool reflectionMethodName)
        {
            var method = frame.GetMethod();
            string methodName = method.Name;
            if (!reflectionMethodName)
            {
                return methodName;
            }
            return methodName;
        }
    }
}
