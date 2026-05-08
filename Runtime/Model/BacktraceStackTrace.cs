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

        private readonly Exception _exception;
        private readonly bool _allowEnvironmentStackFallback;

        public BacktraceStackTrace(Exception exception)
        {
            _exception = exception;
            _allowEnvironmentStackFallback = true;
            Initialize();
        }

        private BacktraceStackTrace(
            Exception exception,
            bool allowEnvironmentStackFallback)
        {
            _exception = exception;
            _allowEnvironmentStackFallback = allowEnvironmentStackFallback;
            Initialize();
        }

        internal static BacktraceStackTrace CreateWithoutEnvironmentFallback(
            Exception exception)
        {
            return new BacktraceStackTrace(
                exception,
                allowEnvironmentStackFallback: false);
        }

        private void Initialize()
        {
            var generateExceptionInformation = _exception != null;
            if (_exception != null)
            {
                var unhandledException = _exception as BacktraceUnhandledException;
                if (unhandledException != null)
                {
                    StackFrames.InsertRange(0, unhandledException.StackFrames);
                    return;
                }

                var exceptionStackTrace = new StackTrace(_exception, true);
                var exceptionFrames = exceptionStackTrace.GetFrames();
                if (exceptionFrames == null || exceptionFrames.Length == 0)
                {
                    if (!_allowEnvironmentStackFallback)
                    {
                        return;
                    }
                    exceptionFrames = new StackTrace(true).GetFrames();
                }
                SetStacktraceInformation(exceptionFrames, true);
                return;
            }

            if (!_allowEnvironmentStackFallback)
            {
                return;
            }

            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
            SetStacktraceInformation(frames, generateExceptionInformation);
        }

        private void SetStacktraceInformation(
            StackFrame[] frames,
            bool generatedByException = false)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }
            var startingIndex = 0;
            foreach (var frame in frames)
            {
                var backtraceFrame = new BacktraceStackFrame(
                    frame,
                    generatedByException);
                if (backtraceFrame.InvalidFrame)
                {
                    continue;
                }
                backtraceFrame.StackFrameType =
                    Types.BacktraceStackFrameType.Dotnet;
                StackFrames.Insert(startingIndex, backtraceFrame);
                startingIndex++;
            }
        }
    }
}
