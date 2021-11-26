using System.Collections.Generic;
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
        internal readonly Queue<string> LogQueue;

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
            LogQueue = new Queue<string>();
        }

        /// <summary>
        /// Get log queue size
        /// </summary>
        public int Size
        {
            get
            {
                return LogQueue.Count;
            }
        }

        /// <summary>
        /// Validate if log manager is enabled. Log manager might be disabled
        /// if Log count is equal to 0.
        /// </summary>
        public bool Disabled
        {
            get
            {
                return _limit == 0;
            }
        }

        /// <summary>
        /// Enqueue user message
        /// </summary>
        /// <param name="report">Backtrace reprot</param>
        /// <returns>Message stored in the log manager</returns>
        public bool Enqueue(BacktraceReport report)
        {
            return Enqueue(new BacktraceUnityMessage(report));

        }

        /// <summary>
        /// Enqueue new unity message
        /// </summary>
        /// <param name="message">Unity message</param>
        /// <param name="stackTrace">Unity Stack trace</param>
        /// <param name="type">Log type</param>
        public bool Enqueue(string message, string stackTrace, LogType type)
        {
            return Enqueue(new BacktraceUnityMessage(message, stackTrace, type));
        }

        /// <summary>
        /// Enqueue new unity message
        /// </summary>
        public bool Enqueue(BacktraceUnityMessage unityMessage)
        {
            if (Disabled)
            {
                return false;
            }
            lock (lockObject)
            {
                LogQueue.Enqueue(unityMessage.ToString());

                while (LogQueue.Count > _limit)
                {
                    LogQueue.Dequeue();
                }
            }
            return true;
        }

        /// <summary>
        /// Generate source code lines based on unity log messages stored in log manager.
        /// </summary>
        /// <returns>Source code</returns>
        public string ToSourceCode()
        {
            var stringBuilder = new StringBuilder();

            var logs = LogQueue.ToArray();
            foreach (var log in logs)
            {
                stringBuilder.AppendLine(log);
            }
            return stringBuilder.ToString();
        }
    }
}