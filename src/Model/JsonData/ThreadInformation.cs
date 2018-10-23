using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using System.Collections.Generic;
using System.Threading;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Get an information about single thread passed in constructor
    /// </summary>
    public class ThreadInformation
    {
        /// <summary>
        /// Thread Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Denotes whether a thread is a faulting thread 
        /// </summary>
        public bool Fault { get; private set; }


        internal IEnumerable<BacktraceStackFrame> Stack = new List<BacktraceStackFrame>();

        /// <summary>
        /// Create new instance of ThreadInformation
        /// </summary>
        /// <param name="threadName">Thread name</param>
        /// <param name="fault">Denotes whether a thread is a faulting thread - in most cases main thread</param>
        /// <param name="stack">Exception stack information</param>
        public ThreadInformation(string threadName, bool fault, IEnumerable<BacktraceStackFrame> stack)
        {
            Stack = stack ?? new List<BacktraceStackFrame>();
            Name = threadName;
            Fault = fault;
        }

        /// <summary>
        /// Create new instance of ThreadInformation
        /// </summary>
        /// <param name="thread">Thread to analyse</param>
        /// <param name="stack">Exception stack information</param>
        /// <param name="currentThread">Is current thread flag</param>
        public ThreadInformation(Thread thread, IEnumerable<BacktraceStackFrame> stack, bool currentThread = false)
            : this(
                 threadName: thread.GenerateValidThreadName().ToLower(),
                 fault: currentThread, //faulting thread = current thread
                 stack: stack)
        { }
    }
}
