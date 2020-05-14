using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Backtrace.Unity.Model
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage", 
        "CA2237:Mark ISerializable types with serializable", 
        Justification = "Backtrace already implements own serialization to generate report")]
    public class BacktraceUnhandledException : Exception
    {
        private bool _header = false;
        public bool Header
        {
            get
            {
                return _header;
            }
        }

        private string _message;
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
                return _stacktrace;
            }
        }

        public string RawStackTrace;

        public List<BacktraceStackFrame> StackFrames = new List<BacktraceStackFrame>();

        public BacktraceUnhandledException(string message, string stacktrace): base(message)
        {
            _stacktrace = stacktrace;
            _message = message;
            RawStackTrace = RawStackTrace;
            ConvertStackFrames();

        }

        private void ConvertStackFrames()
        {
            if(string.IsNullOrEmpty(_stacktrace)) {
                return;
            }
            // frame format:
            // ClassName.MethodName () (at source/path/file.cs:fileLine)
            var frames = _stacktrace.Trim().Split('\n');
            foreach (var frame in frames)
            {
                string frameString = frame == null ? string.Empty : frame.Trim();
                int methodNameEndIndex = frameString.IndexOf(')');

                //because we didnt found 
                if (methodNameEndIndex == -1)
                {
                    if (!_header)
                    {
                        _header = true;
                        _message = frameString;
                        continue;
                    }
                    else
                    {
                        Trace.WriteLine("Detected invalid stack frame: " + frameString);
                    }
                }

                //methodname index should be greater than 0 AND '(' should be before ')'
                if (methodNameEndIndex < 1 && frameString[methodNameEndIndex - 1] != '(')
                {
                    //invalid stack frame
                    return;
                }
                //include ()
                string routingPaths = frameString.Substring(0, methodNameEndIndex + 1);
                var routingParams = routingPaths.Trim().Split('.');
                string methodPath = string.Empty;
                int fileLine = 0;

                int sourceInformationStartIndex = frameString.IndexOf('(', methodNameEndIndex + 1);
                if (sourceInformationStartIndex > -1)
                {
                    // -1 because we don't want additional ')' in the end of the string
                    int sourceStringLength = frameString.Length - sourceInformationStartIndex;
                    string sourceString =
                        frameString
                            .Trim()
                            .Substring(sourceInformationStartIndex, sourceStringLength);

                    int lineNumberSeparator = sourceString.LastIndexOf(':') + 1;
                    int endLineNumberSeparator = sourceString.LastIndexOf(')') - lineNumberSeparator;
                    if (endLineNumberSeparator > 0 && lineNumberSeparator > 0)
                    {
                        string lineNumberString = sourceString.Substring(lineNumberSeparator, endLineNumberSeparator);
                        int.TryParse(lineNumberString, out fileLine);
                    }

                    if (sourceString[0] == '(' && lineNumberSeparator != -1)
                    {
                        //avoid "at" or '('
                        int atSeparator = sourceString.StartsWith("(at")
                            ? 3
                            : 1;
                        int endLine = lineNumberSeparator == 0
                            ? sourceString.LastIndexOf(')') - atSeparator
                            : lineNumberSeparator - 1 - atSeparator;
                        var substring = sourceString.Substring(atSeparator, endLine);
                        methodPath = (substring == null ? string.Empty : substring.Trim());

                        if (!string.IsNullOrEmpty(methodPath))
                        {
                            var testString = string.Copy(methodPath);
                            testString = testString.Replace("0", string.Empty);
                            if (testString.Length <= 2)
                            {
                                methodPath = null;
                            }
                        }
                    }

                }
                StackFrames.Add(new BacktraceStackFrame()
                {

                    FunctionName = string.Join(".", routingParams),
                    Library = methodPath,
                    Line = fileLine
                });

            }
        }
    }
}
