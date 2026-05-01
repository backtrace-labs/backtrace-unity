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
        public LogType Type { get; set; }

        /// <summary>
        /// Unhandled exception stack frames
        /// </summary>
        public readonly List<BacktraceStackFrame> StackFrames;

        /// <summary>
        /// Returns information if the stack trace is from the native environment (non-Unity)
        /// </summary>
        internal bool NativeStackTrace
        {
            get;
            private set;
        }

        public BacktraceUnhandledException(string message, string stacktrace) : base(message)
        {
            Type = LogType.Exception;
            _message = message;
            _stacktrace = stacktrace;
            if (!string.IsNullOrEmpty(stacktrace))
            {
                IEnumerable<string> frames = _stacktrace.Split('\n');
                var stackFrameHeader = frames.ElementAt(0);
                var stackTraceMessage = GetStackTraceErrorMessage(stackFrameHeader);
                if (!string.IsNullOrEmpty(stackTraceMessage))
                {
                    _message = stackTraceMessage;
                    _header = true;
                    frames = frames.Skip(1);
                }

                StackFrames = ConvertStackFrames(frames);
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

        private string GetStackTraceErrorMessage(string beginningOfTheFrame)
        {
            beginningOfTheFrame = beginningOfTheFrame.Trim();
            // verify if the exception message has classifier
            var indexOfExceptionClassifier = beginningOfTheFrame.IndexOf("Exception:");
            if (indexOfExceptionClassifier != -1)
            {
                return beginningOfTheFrame;
            }
            // verify if the exception message looks like a stack frame based on the exception arguments
            if (beginningOfTheFrame.IndexOf('(') == -1 || beginningOfTheFrame.IndexOf(')') == -1)
            {
                return beginningOfTheFrame;
            }

            return string.Empty;

        }

        private List<BacktraceStackFrame> ConvertStackFrames(IEnumerable<string> frames)
        {
            var parser = new BacktraceRawStackTraceParser();
            var result = parser.ConvertStackFrames(frames);
            NativeStackTrace = parser.NativeStackTrace;
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
                    && messageParts.Length > 1
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
