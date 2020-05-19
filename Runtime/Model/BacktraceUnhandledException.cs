using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        public string Classifier { get; set; }

        private readonly string _stacktrace;
        public override string StackTrace
        {
            get
            {
                return _stacktrace;
            }
        }

        /// <summary>
        /// Unhandled exception stack frames
        /// </summary>
        public List<BacktraceStackFrame> StackFrames = new List<BacktraceStackFrame>();

        public BacktraceUnhandledException(string message, string stacktrace) : base(message)
        {
            _message = message;

            if (string.IsNullOrEmpty(stacktrace))
            {
                _stacktrace = new StackTrace(0, true).ToString();
            }
            else
            {
                _stacktrace = stacktrace.Trim();
                ConvertStackFrames();
            }
            TrySetClassifier();

        }

        /// <summary>
        /// Convert Unity error log message to Stack trace that Backtrace uses
        /// in an exception report. Method below support default Unity and Android stack trace.
        /// </summary>
        private void ConvertStackFrames()
        {
            var frames = _stacktrace.Split('\n');
            for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                var frame = frames[frameIndex];
                if (string.IsNullOrEmpty(frame))
                {
                    continue;
                }

                string frameString = frame.Trim();

                // validate if stack trace has exception header 
                int methodNameEndIndex = frameString.IndexOf(')');
                if (methodNameEndIndex == -1 && frameIndex == 0)
                {
                    if (string.IsNullOrEmpty(_message))
                    {
                        _message = frameString;
                    }
                    _header = true;
                    continue;
                }

                //methodname index should be greater than 0 AND '(' should be before ')'
                if (methodNameEndIndex < 1 && frameString[methodNameEndIndex - 1] != '(')
                {
                    //invalid stack frame
                    return;
                }
                
                var stackFrame = 
                    frameString.StartsWith("0x")
                        ? SetNativeStackTraceInformation(frameString)
                        : frameString.IndexOf('(', methodNameEndIndex + 1) > -1
                            ? SetDefaultStackTraceInformation(frameString)
                            : SetAndroidStackTraceInformation(frameString);

                StackFrames.Add(stackFrame);

            }
        }

        private BacktraceStackFrame SetNativeStackTraceInformation(string frameString)
        {
            var stackFrame = new BacktraceStackFrame();
            stackFrame.FunctionName = frameString;
            return stackFrame;
        }

        /// <summary>
        /// Try to convert Android stack frame string to Backtrace stack frame
        /// </summary>
        /// <param name="frameString">Android stack frame</param>
        /// <returns>Backtrace stack frame</returns>
        private BacktraceStackFrame SetAndroidStackTraceInformation(string frameString)
        {
            // validate if stack trace is from Android 
            // try parse method and line number available in the function parameter
            var parameterStart = frameString.LastIndexOf('(') + 1;
            var parameterEnd = frameString.LastIndexOf(')');

            var stackFrame = new BacktraceStackFrame();
            if (parameterStart != -1 && parameterEnd != -1 && parameterEnd - parameterStart > 1)
            {
                stackFrame.FunctionName = frameString.Substring(0, parameterStart - 1);
                var possibleSourceCodeInformation = frameString.Substring(parameterStart, parameterEnd - parameterStart);

                var sourceCodeInformation = possibleSourceCodeInformation.Split(':');
                if (sourceCodeInformation.Length == 2)
                {
                    stackFrame.Library = sourceCodeInformation[0];
                    stackFrame.Line = int.Parse(sourceCodeInformation[1]);
                }
                else if (frameString.StartsWith("java.lang") || possibleSourceCodeInformation == "Unknown Source")
                {
                    stackFrame.Library = possibleSourceCodeInformation;
                }
            }

            return stackFrame;
        }

        /// <summary>
        /// Try to convert defalt unity stack frame to Backtrace stack frame
        /// </summary>
        /// <param name="frameString">Unity stack frame</param>
        /// <param name="sourceInformationStartIndex"></param>
        /// <returns></returns>
        private BacktraceStackFrame SetDefaultStackTraceInformation(string frameString)
        {
            // find method parameters
            int methodNameEndIndex = frameString.IndexOf(')');

            // detect source code information - format : 'at (...)'

            // find source code start based on method parameter start index
            int sourceInformationStartIndex = frameString.IndexOf('(', methodNameEndIndex + 1);

            // get source code information substring
            int sourceStringLength = frameString.Length - sourceInformationStartIndex;
            string sourceString = frameString.Trim()
                    .Substring(sourceInformationStartIndex, sourceStringLength);

            int lineNumberSeparator = sourceString.LastIndexOf(':') + 1;
            int endLineNumberSeparator = sourceString.LastIndexOf(')') - lineNumberSeparator;

            var result = new BacktraceStackFrame()
            {
                FunctionName = frameString.Substring(0, methodNameEndIndex + 1).Trim()
            };

            if (endLineNumberSeparator > 0 && lineNumberSeparator > 0)
            {
                string lineNumberString = sourceString.Substring(lineNumberSeparator, endLineNumberSeparator);
                int.TryParse(lineNumberString, out result.Line);
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
                result.Library = (substring == null ? string.Empty : substring.Trim());

                if (!string.IsNullOrEmpty(result.Library))
                {
                    var testString = string.Copy(result.Library);
                    testString = testString.Replace("0", string.Empty);
                    if (testString.Length <= 2)
                    {
                        result.Library = null;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Detect exception type (classifier) by using error message.
        /// We will try to set classifier based on two patterns:
        ///  1 - ExceptionClassifier: message
        ///  2 - ExceptionClassifier....
        ///  3 - AndroidJavaException: ExceptionClassifier: ....
        ///  in both situation exception classifier must end with 'Exception' string.
        /// </summary>
        private void TrySetClassifier()
        {
            Classifier = "BacktraceUnhandledException";
            if (string.IsNullOrEmpty(_message))
            {
                return;
            }
            const string exceptionPrefix = "Exception";
            const string androidExceptionPrefix = "AndroidJavaException";


            if (_message.EndsWith(exceptionPrefix))
            {
                Classifier = _message.Split(' ').Last();
                return;
            }

            var messageParts = _message.Split(':');
            var guessedClassifier = messageParts[0].Trim();
            if (!string.IsNullOrEmpty(guessedClassifier) && guessedClassifier.EndsWith(exceptionPrefix))
            {
                // handle Android Java exception real classifier
                if (guessedClassifier == androidExceptionPrefix
                    && guessedClassifier.Length > 1
                    && messageParts[1].EndsWith(exceptionPrefix))
                {
                    Classifier = messageParts[1].Trim();
                }
                else
                {
                    Classifier = guessedClassifier;
                }
                return;
            }
        }
    }
}