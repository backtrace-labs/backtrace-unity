using System;
using UnityEngine;

namespace Backtrace.Unity
{
    internal sealed class BacktraceUnityLogHandler : ILogHandler
    {
        private readonly BacktraceClient _client;
        private readonly ILogHandler _innerLogHandler;

        internal ILogHandler InnerLogHandler
        {
            get { return _innerLogHandler; }
        }

        internal BacktraceUnityLogHandler(
            BacktraceClient client,
            ILogHandler innerLogHandler)
        {
            _client = client;
            _innerLogHandler = innerLogHandler;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            try
            {
                if (_client != null)
                {
                    _client.RecordUnityLogHandlerException(exception, context);
                }
            }
            catch
            {
                // Never allow Backtrace instrumentation to interfere with Unity logging.
            }

            if (_innerLogHandler != null)
            {
                _innerLogHandler.LogException(exception, context);
            }
        }

        public void LogFormat(
            LogType logType,
            UnityEngine.Object context,
            string format,
            params object[] args)
        {
            if (_innerLogHandler != null)
            {
                _innerLogHandler.LogFormat(logType, context, format, args);
            }
        }
    }
}
