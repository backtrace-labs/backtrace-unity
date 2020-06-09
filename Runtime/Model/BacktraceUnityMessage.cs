using System;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    internal class BacktraceUnityMessage
    {
        public readonly DateTime Date;
        public string Message;
        public string StackTrace;
        public LogType Type;

        public BacktraceUnityMessage(string message, string stacktrace, LogType type)
        {
            Message = message;
            StackTrace = stacktrace;
            Type = type;
        }
        public bool IsUnhandledException()
        {
            return ((Type == LogType.Exception || Type == LogType.Error)
                && !string.IsNullOrEmpty(Message));
        }
    }
}
