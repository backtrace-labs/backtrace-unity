using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Backtrace.Unity.Model
{
    public class BacktraceUnhandledException : Exception
    {
        private readonly string _message;
        public override string Message
        {
            get
            {
                return _message;
            }
        }

        private readonly string _stacktrace;
        public override string StackTrace
        {
            get
            {
                return base.StackTrace;
            }
        }

        public List<BacktraceStackFrame> StackFrames { get; private set; } = new List<BacktraceStackFrame>();

        public BacktraceUnhandledException(string message, string stacktrace)
        {
            if (string.IsNullOrEmpty(stacktrace))
            {
                stacktrace = new StackTrace(0, true).ToString();
            }
            _stacktrace = stacktrace;
            _message = message;
            ConvertStackFrames();
        }

        private void ConvertStackFrames()
        {
            // frame format:
            // ClassName.MethodName () (at source/path/file.cs:fileLine)
            var frames = _stacktrace.Trim().Split('\n');
            foreach (var frameString in frames)
            {
                int methodNameEndIndex = frameString.IndexOf(')');
                //methodname index should be greater than 0 AND '(' should be before ')'
                if (methodNameEndIndex < 1 && frameString[methodNameEndIndex - 1] != '(')
                {
                    //invalid stack frame
                    return;
                }
                string routingPaths = frameString.Substring(0, methodNameEndIndex - 1);
                var routingParams = routingPaths.Trim().Split('.');
                string methodPath = string.Empty;
                int fileLine = 0;

                int sourceInformationStartIndex = frameString.IndexOf('(', methodNameEndIndex + 1);
                if (sourceInformationStartIndex > -1)
                {
                    // -1 because we don't want additional ')' in the end of the string
                    int sourceStringLength = frameString.Length - sourceInformationStartIndex - 1;
                    string sourceString =
                        frameString.Substring(sourceInformationStartIndex, sourceStringLength);
                    var sourceInfo = sourceString.Split(':');
                    methodPath = sourceInfo[0].Substring(sourceInfo[0].IndexOf(' '));
                    int.TryParse(sourceInfo[1], out fileLine);
                }
                StackFrames.Add(new BacktraceStackFrame()
                {
                    FunctionName = routingParams[1],
                    Library = routingParams[0],
                    Line = fileLine,
                    SourceCode = methodPath
                });

            }
        }
    }
}
