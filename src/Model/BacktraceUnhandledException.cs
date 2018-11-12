using System;
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

        public BacktraceUnhandledException(string message, string stacktrace)
        {
            if (string.IsNullOrEmpty(stacktrace))
            {
                stacktrace = new StackTrace(true).ToString();
            }
            _stacktrace = stacktrace;
            _message = message;
        }

    }
}
