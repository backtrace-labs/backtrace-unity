using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Backtrace stack trace
    /// </summary>
    internal class BacktraceStackTrace
    {
        /// <summary>
        /// Stack trace frames
        /// </summary>
        public readonly List<BacktraceStackFrame> StackFrames = new List<BacktraceStackFrame>();

        public BacktraceSourceCode SourceCode = null;

        /// <summary>
        /// Current exception
        /// </summary>
        private readonly Exception _exception;

         /// <summary>
        /// Report message
        /// </summary>
        private readonly string _message;

        /// <summary>
        /// Report stack trace
        /// </summary>
        private string _stackTrace = string.Empty;
        
        public BacktraceStackTrace(string message, Exception exception)
        {
            _message = message;
            _exception = exception;
            if (exception != null)
            {
                _stackTrace = exception.StackTrace;
            }
            Initialize();
        }

        private void Initialize()
        {
            bool generateExceptionInformation = _exception != null;
            if (_exception != null)
            {
                if (_exception is BacktraceUnhandledException)
                {
                    var current = _exception as BacktraceUnhandledException;
                    StackFrames.InsertRange(0, current.StackFrames);
                    SourceCode = current.SourceCode;
                    return;
                }
                else
                {
                    var exceptionStackTrace = new StackTrace(_exception, true);
                    var exceptionFrames = exceptionStackTrace.GetFrames();
                    SetStacktraceInformation(exceptionFrames, true);
                }
            }
            else
            {
                //initialize environment stack trace
                var stackTrace = new StackTrace(true);
                //reverse frame order
                var frames = stackTrace.GetFrames();
                SetStacktraceInformation(frames, generateExceptionInformation);
                if (StackFrames.Any())
                {
                    _stackTrace = string.Join("\n", StackFrames.Select(n => n.ToString()));
                }
            }
            CreateUnhandledExceptionLogInformation();
        }

        private void SetStacktraceInformation(StackFrame[] frames, bool generatedByException = false)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }
            int startingIndex = 0;
            foreach (var frame in frames)
            {
                var backtraceFrame = new BacktraceStackFrame(frame, generatedByException);
                if (backtraceFrame.InvalidFrame)
                {
                    continue;
                }
                StackFrames.Insert(startingIndex, backtraceFrame);
                startingIndex++;
            }
        }

        /// <summary>
        /// Assign source code information to first stack frame of unhandled exception report
        /// </summary>
        private void CreateUnhandledExceptionLogInformation()
        {
            // we can't assign source code to any stack frame.
            if (!StackFrames.Any())
            {
                return;
            }

            SourceCode = new BacktraceSourceCode()
            {
                Text = string.Format("Unity exception information\nMessage: {0}\nStack trace: {1}", _message, _stackTrace)
            };
            // assign log information to first stack frame
            if (StackFrames.Count == 0)
            {
                return;
            }
            StackFrames.First().SourceCode = SourceCode.Id.ToString();
        }
    }
}
