using Backtrace.Unity.Model.Attributes;
using Backtrace.Unity.Model.Metrics;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    public interface IBacktraceMetrics : IScopeAttributeProvider
    {
        /// <summary>
        /// This list contains the unique events which will be sent whenever Send is triggered (manually or automatically). 
        /// By default, this list contains the guid attribute. This list is mutable, which means the default can be removed
        /// and custom unique events can be added. Typical values to add are UserID, SteamID and other attributes that uniquely 
        /// identify an user. This list is persistent, meaning that events will not be removed upon Send() (like for summed events).
        /// For non-standard unique events, server side configuration needs to be done. 
        /// Please refer to the <see href="https://support.backtrace.io">online documentation</see>.
        /// </summary>
        //LinkedList<UniqueEvent> UniqueEvents { get; }

        /// <summary>
        /// Maximum number of summed events in store. If number of events in store hit the limit
        /// BacktraceMetrics instance will send data to Backtrace.
        /// </summary>
        uint MaximumSummedEvents { get; set; }

        /// <summary>
        /// Maximum number of unique events in store. If number of events in store hit the limit
        /// BacktraceMetrics instance will send data to Backtrace.
        /// </summary>
        uint MaximumUniqueEvents { get; set; }

        /// <summary>
        /// Unique events submission URL
        /// </summary>
        string UniqueEventsSubmissionUrl { get; set; }

        /// <summary>
        /// Summed events submission URL
        /// </summary>
        string SummedEventsSubmissionUrl { get; set; }

        /// <summary>
        /// Trigger a manual send, will send all outgoing messages currently queued.
        /// </summary>
        void Send();
        //bool AddUniqueEvent(string attributeName);
        //bool AddUniqueEvent(string attributeName, IDictionary<string, string> attributes);

        /// <summary>
        /// Adds a summed event to the outgoing queue.
        /// </summary>
        /// See <see cref="BacktraceClient.Metrics.Send(string, IDictionary)"/>.
        /// <param name="metricGroupName">The name of the metric group to be incremented. This metric group must be configured on server side as well, please refer to the <see href = "https://support.backtrace.io" > online documentation</see>.</param>
        /// <returns>true if added successfully, otherwise false.</returns>
        bool AddSummedEvent(string metricsGroupName);

        /// <summary>
        /// Adds a summed event to the outgoing queue.
        /// </summary>
        /// <param name="metricGroupName">The name of the metric group to be incremented. This metric group must be configured on server side as well, please refer to the <see href = "https://support.backtrace.io" > online documentation</see>.</param>
        /// <param name="attributes">Custom attributes to add. Will be merged with the default attributes, with attribute values provided here overriding any defaults.</param>
        /// <returns>true if added successfully, otherwise false.</returns>
        bool AddSummedEvent(string metricsGroupName, IDictionary<string, string> attributes);
    }
}
