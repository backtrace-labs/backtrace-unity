using System;
using System.Collections.Generic;
using System.Diagnostics;

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


        /// <summary>
        /// Current exception
        /// </summary>
        private readonly Exception _exception;

        public BacktraceStackTrace(Exception exception)
        {
            _exception = exception;
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
                }
                else
                {
                    var exceptionStackTrace = new StackTrace(_exception, true);
                    var exceptionFrames = exceptionStackTrace.GetFrames();
                    if (exceptionFrames == null || exceptionFrames.Length == 0)
                    {
                        exceptionFrames = new StackTrace(true).GetFrames();
                    }
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
            }
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
                backtraceFrame.StackFrameType = Types.BacktraceStackFrameType.Dotnet;
                StackFrames.Insert(startingIndex, backtraceFrame);
                startingIndex++;
            }
        }
    }
}
