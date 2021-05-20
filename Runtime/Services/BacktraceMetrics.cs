using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Model.Metrics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Services
{
    internal sealed class BacktraceMetrics : IBacktraceMetrics
    {
        /// <summary>
        /// Default submission URL
        /// </summary>
        public const string DefaultSubmissionUrl = "https://events.backtrace.io/api";

        /// <summary>
        /// Default time interval in min
        /// </summary>
        public const uint DefaultTimeIntervalInMin = 30;

        /// <summary>
        /// Default time interval in sec
        /// </summary>
        public const uint DefaultTimeIntervalInSec = DefaultTimeIntervalInMin * 60;

        /// <summary>
        /// Default unique event name that will be generated on the application startup
        /// </summary>
        public const string DefaultUniqueEventName = "guid";

        /// <summary>
        /// Startup unique event name that will be generated on the application startup
        /// </summary>
        public string StartupUniqueEventName { get; set; } = DefaultUniqueEventName;

        /// <summary>
        /// Maximum number of events in store. If number of events in store hit the limit
        /// BacktraceMetrics instance will send data to Backtrace.
        /// </summary>
        public uint MaximumEvents { get; set; } = 350;

        /// <summary>
        /// Maximum time between requests
        /// </summary>
        public const int MaxTimeBetweenRequests = 5 * 60;

        /// <summary>
        /// Default maximum number of attemps
        /// </summary>
        public const int MaxNumberOfAttempts = 3;

        /// <summary>
        /// Unique events submission queue
        /// </summary>
        internal readonly UniqueEventsSubmissionQueue _uniqueEventSubmissionQueue;

        /// <summary>
        /// Summed events submission queue
        /// </summary>
        internal readonly SummedEventSubmissionQueue _summedEventSubmissionQueue;

        /// <summary>
        /// Startup event name that will be send on the application startup
        /// </summary>
        private const string StartupEventName = "Application Launches";

        /// <summary>
        /// Submission url
        /// </summary>
        public string SubmissionUrl
        {
            get
            {
                return RequestHandler.BaseUrl;
            }
            set
            {
                RequestHandler.BaseUrl = value;
            }
        }

        /// <summary>
        /// Determine if http client should ignore ssl validation
        /// </summary>
        public bool IgnoreSslValidation
        {
            get
            {
                return RequestHandler.IgnoreSslValidation;
            }
            set
            {
                RequestHandler.IgnoreSslValidation = value;
            }
        }

        /// <summary>
        /// List of unique events that will be added to next submission payload
        /// </summary>
        public LinkedList<UniqueEvent> UniqueEvents
        {
            get
            {
                return _uniqueEventSubmissionQueue.Events;
            }
        }

        /// <summary>
        /// List of summed events that will be added to next submission payload
        /// </summary>
        internal LinkedList<SummedEvent> SummedEvents
        {
            get
            {
                return _summedEventSubmissionQueue.Events;
            }
        }

        /// <summary>
        /// Time interval in ms that algorithm uses to automatically send data to Backtrace
        /// </summary>
        private readonly long _timeIntervalInSec;

        /// <summary>
        /// Last update time that will be updated after each update start
        /// </summary>
        private float _lastUpdateTime = 0;

        /// <summary>
        /// Http client
        /// </summary>
        internal IBacktraceHttpClient RequestHandler;

        /// <summary>
        /// Backtrace attribute provider - shared reference that will be reused by Backtrace client to generate attributes.
        /// BacktraceMetrics implementation uses attribute provider to generate attributes dynamically when user adds a summed events.
        /// </summary>
        private readonly AttributeProvider _attributeProvider;

        /// <summary>
        /// Lock object
        /// </summary>
        private object _object = new object();


        /// <summary>
        /// Create new Backtrace metrics instance
        /// </summary>
        /// <param name="attributeProvider">Backtrace client attribute provider</param>
        /// <param name="submissionBaseUrl">Submission base url</param>
        /// <param name="timeIntervalInSec">Update time interval in MS</param>
        /// <param name="token">Submission token</param>
        /// <param name="universeName">Universe name</param>
        public BacktraceMetrics(
            AttributeProvider attributeProvider,
            string submissionBaseUrl,
            long timeIntervalInSec,
            string token,
            string universeName) : this(new BacktraceHttpClient() { BaseUrl = submissionBaseUrl }, attributeProvider, timeIntervalInSec, token, universeName)
        { }

        /// <summary>
        /// Create new Backtrace metrics instance
        /// </summary>
        internal BacktraceMetrics(
            IBacktraceHttpClient httpClient,
            AttributeProvider attributeProvider,
            long timeIntervalInSec,
            string token,
            string universeName)
        {
            RequestHandler = httpClient;
            _attributeProvider = attributeProvider;
            _timeIntervalInSec = timeIntervalInSec;
            _uniqueEventSubmissionQueue = new UniqueEventsSubmissionQueue(universeName, token, RequestHandler, _attributeProvider);
            _summedEventSubmissionQueue = new SummedEventSubmissionQueue(universeName, token, RequestHandler, _attributeProvider);
        }

        /// <summary>
        /// Send startup event to Backtrace
        /// </summary>
        public void SendStartupEvent()
        {
            _uniqueEventSubmissionQueue.StartWithEvent(DefaultUniqueEventName);
            _summedEventSubmissionQueue.StartWithEvent(StartupEventName);
        }

        /// <summary>
        /// Backtrace metrics tick method that class implementation uses to send data to server based on the time interval conditions
        /// </summary>
        /// <param name="time">Current game time</param>
        public void Tick(float time)
        {
            lock (_object)
            {
                SendPendingSubmissionJobs(time);
            }
            if (_timeIntervalInSec == 0)
            {
                return;
            }
            lock (_object)
            {

                var intervalUpdate = (time - _lastUpdateTime) >= _timeIntervalInSec;
                var reachedEventLimit = MaximumEvents == Count() && MaximumEvents != 0;
                if (intervalUpdate == false && reachedEventLimit == false)
                {
                    // nothing more to update
                    return;
                }
                _lastUpdateTime = time;
            }
            Send();
        }

        /// <summary>
        /// Trigger a manual send, will send all outgoing messages currently queued.
        /// </summary>
        public void Send()
        {
            _uniqueEventSubmissionQueue.Send();
            _summedEventSubmissionQueue.Send();
        }
        /// <summary>
        /// Add unique event to next Backtrace Metrics request
        /// </summary>
        /// <param name="attributeName">attribute name</param>
        public bool AddUniqueEvent(string attributeName)
        {
            return AddUniqueEvent(attributeName, null);
        }

        /// <summary>
        /// Add unique event to next Backtrace Metrics request
        /// </summary>
        /// <param name="attributeName">attribute name</param>
        /// <param name="attributes">Event attributes</param>
        public bool AddUniqueEvent(string attributeName, IDictionary<string, string> attributes = null)
        {
            if (!ShouldProcessEvent(attributeName))
            {
                Debug.LogWarning("Skipping report: Reached store limit or event has empty name.");
                return false;
            }
            // add to event attributes, attribute provider attributes
            if (attributes == null)
            {
                attributes = new Dictionary<string, string>();
            }
            _attributeProvider.AddAttributes(attributes);

            // validate if unique event attribute is available and
            // prevent undefined attributes
            if (attributes.TryGetValue(attributeName, out string attributeValue) == false || string.IsNullOrEmpty(attributeValue))
            {
                Debug.LogWarning("Attribute name is not available in attribute scope. Please define attribute to set unique event.");
                return false;
            }
            // skip already defined unique events
            if (UniqueEvents.Any(n => n.Name == attributeName))
            {
                return false;
            }
            var @event = new UniqueEvent(attributeName, DateTimeHelper.Timestamp(), attributes);
            _uniqueEventSubmissionQueue.Events.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Get number of events available right now in store
        /// </summary>
        /// <returns>number of events in store</returns>
        public int Count()
        {
            return _uniqueEventSubmissionQueue.Count + _summedEventSubmissionQueue.Count;
        }

        /// <summary>
        /// Add summed event to next Backtrace Metrics request
        /// </summary>
        /// <param name="metricsGroupName">summed event name</param>
        public bool AddSummedEvent(string metricsGroupName)
        {
            return AddSummedEvent(metricsGroupName, null);
        }
        /// <summary>
        /// Add summed event to next Backtrace Metrics request
        /// </summary>
        /// <param name="metricsGroupName">Summed event name</param>
        /// <param name="attributes">Summed event attribute</param>
        public bool AddSummedEvent(string metricsGroupName, IDictionary<string, string> attributes = null)
        {
            if (!ShouldProcessEvent(metricsGroupName))
            {
                Debug.LogWarning("Skipping report: Reached store limit or event has empty name.");
                return false;
            }

            var @event = new SummedEvent(metricsGroupName, DateTimeHelper.Timestamp(), attributes);
            _summedEventSubmissionQueue.Events.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Check submission job failures and start submission for jobs that NextInvokeTime 
        /// is lower than time now.
        /// </summary>
        /// <param name="time">Current time</param>
        private void SendPendingSubmissionJobs(float time)
        {
            _uniqueEventSubmissionQueue.SendPendingEvents(time);
            _summedEventSubmissionQueue.SendPendingEvents(time);
        }

        /// <summary>
        /// Determine if Backtrace Metrics can add next event to store
        /// </summary>
        /// <param name="name">event name</param>
        /// <returns>True if we're able to add. Otherwise false.</returns>
        private bool ShouldProcessEvent(string name)
        {
            return !string.IsNullOrEmpty(name) && (MaximumEvents == 0 || (Count() + 1 <= MaximumEvents));
        }

    }
}
