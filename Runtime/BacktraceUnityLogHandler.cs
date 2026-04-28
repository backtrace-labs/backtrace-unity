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
            var capturedByBacktrace = false;
            try
            {
                if (_client != null)
                {
                    capturedByBacktrace = _client.TryCaptureUnityLogHandlerException(
                        exception,
                        context);
                }
            }
            catch
            {
                capturedByBacktrace = false;
            }

            if (capturedByBacktrace && _client != null)
            {
                try
                {
                    _client.SuppressNextUnityLogReport(exception, LogType.Exception);
                }
                catch
                {
                    // Do not allow the Backtrace duplicate-suppression path to interfere
                    // with Unity's own logging.
                }
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
