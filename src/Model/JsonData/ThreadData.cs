using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        internal ThreadData(Assembly callingAssembly, IEnumerable<BacktraceStackFrame> exceptionStack)
        {
            //get all available process threads
            ProcessThreads();

            //get stack trace and infomrations about current thread
            GenerateCurrentThreadInformation(exceptionStack);
        }

        /// <summary>
        /// Generate information for current thread
        /// </summary>
        /// <param name="exceptionStack">Current BacktraceReport exception stack</param>
        private void GenerateCurrentThreadInformation(IEnumerable<BacktraceStackFrame> exceptionStack)
        {
            var current = Thread.CurrentThread;
            //get current thread id
            string generatedMainThreadId = current.GenerateValidThreadName().ToLower();

            ThreadInformations[generatedMainThreadId] = new ThreadInformation(current, exceptionStack, true);
            //set currentThreadId
            MainThread = generatedMainThreadId;
        }

        /// <summary>
        /// Generate list of process thread 
        /// </summary>
        private void ProcessThreads()
        {
            ProcessThreadCollection currentThreads = null;
            try
            {
                currentThreads = Process.GetCurrentProcess().Threads;
                if (currentThreads == null)
                {
                    return;
                }
            }
            catch
            {
                //handle UWP
                return;
            }
            foreach (ProcessThread thread in currentThreads)
            {
                if (thread == null)
                {
                    continue;
                }
                //you can't retrieve stack trace from processThread
                //you can't retrieve thread name from processThread 
                string threadId = thread.Id.ToString();
                ThreadInformations.Add(Guid.NewGuid().ToString(), new ThreadInformation(threadId, false, null));
            }
        }
    }
}