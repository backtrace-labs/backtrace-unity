using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        public LogType Type { get; set; } = LogType.Exception;

        /// <summary>
        /// Unhandled exception stack frames
        /// </summary>
        public readonly List<BacktraceStackFrame> StackFrames = new List<BacktraceStackFrame>();


        public BacktraceUnhandledException(string message, string stacktrace) : base(message)
        {
            _message = message;
            _stacktrace = stacktrace;
            if (!string.IsNullOrEmpty(stacktrace))
            {
                ConvertStackFrames();
            }

            if (string.IsNullOrEmpty(stacktrace) || StackFrames.Count == 0)
            {
                // make sure that for this kind of exception, this exception message will be always the same
                // error message might be overriden by ConvertStackFrames method.
                _message = message;
                var backtraceStackTrace = new BacktraceStackTrace(null);
                StackFrames = backtraceStackTrace.StackFrames;
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
                if (methodNameEndIndex == -1)
                {
                    // apply error message
                    if (frameIndex == 0)
                    {
                        if (string.IsNullOrEmpty(_message))
                        {
                            _message = frameString;
                        }
                        _header = true;
                        continue;
                    }
                    else
                    {
                        //invalid stack frame
                        continue;
                    }
                }

                //methodname index should be greater than 0 AND '(' should be before ')'
                if (methodNameEndIndex < 1 && frameString[methodNameEndIndex - 1] != '(')
                {
                    //invalid stack frame
                    return;
                }

                BacktraceStackFrame stackFrame = null;
                if (frameString.StartsWith("0x"))
                {
                    stackFrame = SetNativeStackTraceInformation(frameString);
                }
                else if (frameString.StartsWith("#"))
                {
                    stackFrame = SetJITStackTraceInformation(frameString);
                }
                else if (frameString.IndexOf('(', methodNameEndIndex + 1) > -1)
                {
                    stackFrame = SetDefaultStackTraceInformation(frameString);
                }
                else
                {
                    stackFrame = SetAndroidStackTraceInformation(frameString);
                }

                // if function name is null - try to apply default parser
                if (stackFrame == null || string.IsNullOrEmpty(stackFrame.FunctionName))
                {
                    stackFrame = new BacktraceStackFrame()
                    {
                        FunctionName = frameString
                    };
                }

                StackFrames.Add(stackFrame);


            }
        }

        /// <summary>
        /// Try to convert JIT stack trace
        /// </summary>
        /// <param name="frameString">JIT stack frame</param>
        /// <returns>Backtrace stack frame</returns>
        private BacktraceStackFrame SetJITStackTraceInformation(string frameString)
        {

            var stackFrame = new BacktraceStackFrame();
            stackFrame.StackFrameType = Types.BacktraceStackFrameType.Native;
            if (!frameString.StartsWith("#"))
            {
                //handle sitaution when we detected jit stack trace
                // but jit stack trace doesn't start with #
                stackFrame.FunctionName = frameString;
                return stackFrame;
            }

            frameString = frameString.Substring(frameString.IndexOf(' ')).Trim();
            const string monoJitPrefix = "(Mono JIT Code)";
            var monoPrefixIndex = frameString.IndexOf(monoJitPrefix);
            if (monoPrefixIndex != -1)
            {
                frameString = frameString.Substring(monoPrefixIndex + monoJitPrefix.Length).Trim();
            }

            const string managedWraperPrefix = "(wrapper managed-to-native)";
            var managedWraperIndex = frameString.IndexOf(managedWraperPrefix);
            if (managedWraperIndex != -1)
            {
                frameString = frameString.Substring(managedWraperIndex + managedWraperPrefix.Length).Trim();
            }

            // right now we outfiltered all known prefixes 
            // we should have only function name with parameters

            // filter parameters, if we can't use full frameString as function name
            var parametersStart = frameString.IndexOf('(');
            var parametersEnd = frameString.IndexOf(')');
            if (parametersStart != -1 && parametersEnd != -1 && parametersEnd > parametersStart)
            {
                stackFrame.FunctionName = frameString.Substring(0, parametersStart).Trim();
            }
            else
            {
                stackFrame.FunctionName = frameString;
            }
            return stackFrame;

        }

        /// <summary>
        /// Try to convert native stack frame
        /// </summary>
        /// <param name="frameString">Native stack frame</param>
        /// <returns>Backtrace stack frame</returns>
        private BacktraceStackFrame SetNativeStackTraceInformation(string frameString)
        {
            var stackFrame = new BacktraceStackFrame();
            stackFrame.StackFrameType = Types.BacktraceStackFrameType.Native;
            // parse address
            var addressSubstringIndex = frameString.IndexOf(' ');
            stackFrame.Address = frameString.Substring(0, addressSubstringIndex);
            var indexPointer = addressSubstringIndex + 1;

            // parse library
            if (frameString[indexPointer] == '(')
            {
                indexPointer = indexPointer + 1;
                var libraryNameSubstringIndex = frameString.IndexOf(')', indexPointer);
                stackFrame.Library = frameString.Substring(indexPointer, libraryNameSubstringIndex - indexPointer);
                indexPointer = libraryNameSubstringIndex + 2;
            }

            stackFrame.FunctionName = frameString.Substring(indexPointer);
            //cleanup function name
            if (stackFrame.FunctionName.StartsWith("(wrapper managed-to-native)"))
            {
                stackFrame.FunctionName = stackFrame.FunctionName.Replace("(wrapper managed-to-native)", string.Empty).Trim();
            }

            if (stackFrame.FunctionName.StartsWith("(wrapper runtime-invoke)"))
            {
                stackFrame.FunctionName = stackFrame.FunctionName.Replace("(wrapper runtime-invoke)", string.Empty).Trim();
            }

            // try to find source code information
            int sourceCodeStartIndex = stackFrame.FunctionName.IndexOf('[');
            int sourceCodeEndIndex = stackFrame.FunctionName.IndexOf(']');
            if (sourceCodeStartIndex != -1 && sourceCodeEndIndex != -1)
            {
                sourceCodeStartIndex = sourceCodeStartIndex + 1;
                var sourceCodeInformation = stackFrame.FunctionName.Substring(
                    sourceCodeStartIndex,
                    sourceCodeEndIndex - sourceCodeStartIndex);

                var sourceCodeParts = sourceCodeInformation.Split(new char[] { ':' }, 2);
                if (sourceCodeParts.Length == 2)
                {
                    int.TryParse(sourceCodeParts[1], out stackFrame.Line);
                    stackFrame.Library = sourceCodeParts[0];
                    stackFrame.FunctionName = stackFrame.FunctionName.Substring(sourceCodeEndIndex + 2);
                }
            }

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
            stackFrame.StackFrameType = Types.BacktraceStackFrameType.Android;
            if (parameterStart != -1 && parameterEnd != -1 && parameterEnd - parameterStart > 1)
            {
                stackFrame.FunctionName = frameString.Substring(0, parameterStart - 1);
                var possibleSourceCodeInformation = frameString.Substring(parameterStart, parameterEnd - parameterStart);

                var sourceCodeInformation = possibleSourceCodeInformation.Split(':');
                if (sourceCodeInformation.Length == 2)
                {
                    stackFrame.Library = sourceCodeInformation[0];
                    int.TryParse(sourceCodeInformation[1], out stackFrame.Line);
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
            const string wrapperPrefix = "(wrapper remoting-invoke-with-check)";
            if (frameString.StartsWith(wrapperPrefix))
            {
                frameString = frameString.Replace(wrapperPrefix, string.Empty);
            }
            // find method parameters
            int methodNameEndIndex = frameString.IndexOf(')');

            // detect source code information - format : 'at (...)'

            // find source code start based on method parameter start index
            int sourceInformationStartIndex = frameString.IndexOf('(', methodNameEndIndex + 1);
            if (sourceInformationStartIndex == -1)
            {
                return new BacktraceStackFrame()
                {
                    FunctionName = frameString,
                    StackFrameType = Types.BacktraceStackFrameType.Dotnet
                };
            }

            // get source code information substring
            int sourceStringLength = frameString.Length - sourceInformationStartIndex;
            string sourceString = frameString.Trim()
                    .Substring(sourceInformationStartIndex, sourceStringLength);

            int lineNumberSeparator = sourceString.LastIndexOf(':') + 1;
            int endLineNumberSeparator = sourceString.LastIndexOf(')') - lineNumberSeparator;

            var result = new BacktraceStackFrame()
            {
                FunctionName = frameString.Substring(0, methodNameEndIndex + 1).Trim(),
                StackFrameType = Types.BacktraceStackFrameType.Dotnet
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
            Classifier = "error";
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
            }
        }
    }
}