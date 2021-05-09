using Backtrace.Unity.Model.Session;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    public interface IBacktraceSession
    {
        /// <summary>
        /// List of unique events stored in the BacktraceSession class
        /// </summary>
        LinkedList<UniqueEvent> UniqueEvents { get; }

        /// <summary>
        /// Default unique event name that will be generated on the application startup
        /// </summary>
        string DefaultUniqueEventName { get; set; }
        /// <summary>
        /// Maximum number of events in store. If number of events in store hit the limit
        /// BacktraceSession instance will send data to Backtrace.
        /// </summary>
        uint MaximumEvents { get; set; }

        /// <summary>
        /// Submission url
        /// </summary>
        string SubmissionUrl { get; set; }

        /// <summary>
        /// Send startup event to Backtrace
        /// </summary>
        void SendStartupEvent();
        void Send();
        bool AddUniqueEvent(string attributeName);
        bool AddSessionEvent(string sessionEvent);
        bool AddUniqueEvent(string attributeName, IDictionary<string, string> attributes);
        bool AddSessionEvent(string sessionEvent, IDictionary<string, string> attributes);
        void Tick(float time);
    }
}
