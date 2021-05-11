using Backtrace.Unity.Model.Session;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    public interface IBacktraceSession
    {
        /// <summary>
        /// This list contains the unique events which will be sent whenever Send is triggered (manually or automatically). 
        /// By default, this list contains the guid attribute. This list is mutable, which means the default can be removed
        /// and custom unique events can be added. Typical values to add are UserID, SteamID and other attributes that uniquely 
        /// identify an user. This list is persistent, meaning that events will not be removed upon Send() (like for summation events).
        /// For non-standard unique events, server side configuration needs to be done. 
        /// Please refer to the <see href="https://support.backtrace.io">online documentation</see>.
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

        /// <summary>
        /// Trigger a manual send, will send all outgoing messages currently queued.
        /// </summary>
        void Send();
        bool AddUniqueEvent(string attributeName);
        bool AddUniqueEvent(string attributeName, IDictionary<string, string> attributes);

        /// <summary>
        /// Adds a summation event to the outgoing queue.
        /// </summary>
        /// See <see cref="IBacktraceMetricsClient.Send(string, IDictionary)"/>.
        /// <param name="metricGroupName">The name of the metric group to be incremented. This metric group must be configured on server side as well, please refer to the <see href = "https://support.backtrace.io" > online documentation</see>.</param>
        /// <returns>true if added successfully, otherwise false.</returns>
        bool AddSessionEvent(string metricsGroupName);

        /// <summary>
        /// Adds a summation event to the outgoing queue.
        /// </summary>
        /// <param name="metricGroupName">The name of the metric group to be incremented. This metric group must be configured on server side as well, please refer to the <see href = "https://support.backtrace.io" > online documentation</see>.</param>
        /// <param name="attributes">Custom attributes to add. Will be merged with the default attributes, with attribute values provided here overriding any defaults.</param>
        /// <returns>true if added successfully, otherwise false.</returns>
        bool AddSessionEvent(string metricsGroupName, IDictionary<string, string> attributes);
        void Tick(float time);
    }
}
