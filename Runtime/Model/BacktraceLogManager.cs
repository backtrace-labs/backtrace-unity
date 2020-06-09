using System.Collections.Concurrent;
using System.Text;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Backtrace Unity engine log manager
    /// </summary>
    internal class BacktraceLogManager
    {
        /// <summary>
        /// Unity message queue 
        /// </summary>
        private readonly ConcurrentQueue<BacktraceUnityMessage> _logQueue;

        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// Maximum number of logs that log manager can store.
        /// </summary>
        private readonly uint _limit;

        public BacktraceLogManager(uint numberOfLogs)
        {
            _limit = numberOfLogs;
            _logQueue = new ConcurrentQueue<BacktraceUnityMessage>();
        }

        /// <summary>
        /// Get log queue size
        /// </summary>
        public int Size
        {
            get
            {
                return _logQueue.Count;
            }
        }
        /// <summary>
        /// Enqueue new unity message
        /// </summary>
        /// <param name="message">Unity message</param>
        /// <param name="stacktrace">Unity Stack trace</param>
        /// <param name="type">Log type</param>
        public BacktraceUnityMessage Enqueue(string message, string stacktrace, LogType type)
        {
            var unityMessage = new BacktraceUnityMessage(message, stacktrace, type);
            if(_limit == 0)
            {
                return unityMessage;
            }

            _logQueue.Enqueue(unityMessage);
            lock (lockObject)
            {
                while (_logQueue.Count > _limit && _logQueue.TryDequeue(out BacktraceUnityMessage _)) ;
            }
            return unityMessage;
        }

        /// <summary>
        /// Generate source code lines based on unity log messages stored in log manager.
        /// </summary>
        /// <returns>Source code</returns>
        public string ToSourceCode()
        {
            var stringBuilder = new StringBuilder();
            foreach (var item in _logQueue)
            {
                stringBuilder.AppendLine(
                    string.Format(
                        "[{0}] {1}: {2}",
                        item.Date.ToLongDateString(), item.Type.ToString(), item.Message));

                if (!string.IsNullOrEmpty(item.StackTrace) 
                    && (item.Type == LogType.Exception || item.Type == LogType.Error))
                {
                    stringBuilder.AppendLine(item.StackTrace);
                }
            }
            return stringBuilder.ToString();
        }
    }
}
