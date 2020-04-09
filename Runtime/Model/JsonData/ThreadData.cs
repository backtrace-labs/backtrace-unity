using Backtrace.Unity.Extensions;
using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Generate information about appliaction threads
    /// </summary>
    public class ThreadData
    {
        /// <summary>
        /// All collected application threads information
        /// </summary>
        public Dictionary<string, ThreadInformation> ThreadInformations = new Dictionary<string, ThreadInformation>();

        /// <summary>
        /// Application Id for current thread. This value is used in mainThreadSection in output JSON file
        /// </summary>
        internal string MainThread = string.Empty;

        /// <summary>
        /// Create instance of ThreadData class to collect information about used threads
        /// </summary>
        internal ThreadData(IEnumerable<BacktraceStackFrame> exceptionStack)
        {

            var current = Thread.CurrentThread;
            //get current thread id
            string generatedMainThreadId = current.GenerateValidThreadName().ToLower();

            ThreadInformations[generatedMainThreadId] = new ThreadInformation(current, exceptionStack, true);
            //set currentThreadId
            MainThread = generatedMainThreadId;
        }
        private ThreadData() { }

        public BacktraceJObject ToJson()
        {
            var threadData = new BacktraceJObject();
            foreach (var threadInfo in ThreadInformations)
            {
                threadData[threadInfo.Key] = threadInfo.Value.ToJson();
            }
            return threadData;
        }
    }
}