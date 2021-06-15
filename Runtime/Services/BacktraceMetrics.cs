using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Model.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Services
{
    internal sealed class BacktraceMetrics : IBacktraceMetrics
    {
        /// <summary>
        /// Session Id
        /// </summary>
        public readonly Guid SessionId = Guid.NewGuid();

        /// <summary>
        /// Default submission URL
        /// </summary>
        public const string DefaultSubmissionUrl = "https://events.backtrace.io/api/";

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
        public const string DefaultUniqueAttributeName = "guid";

        /// <summary>
        /// Startup unique event name that will be generated on the application startup
        /// </summary>
        public string StartupUniqueAttributeName { get; set; } = DefaultUniqueAttributeName;

        /// <summary>
        /// Maximum number of unique events in store. If number of events in store hit the limit
        /// BacktraceMetrics instance will send data to Backtrace.
        /// </summary>
        public uint MaximumUniqueEvents
        {
            get
            {
                return _uniqueEventsSubmissionQueue.MaximumEvents;
            }
            set
            {
                _uniqueEventsSubmissionQueue.MaximumEvents = value;
            }
        }


        /// <summary>
        /// Maximum number of summed events in store. If number of events in store hit the limit
        /// BacktraceMetrics instance will send data to Backtrace.
        /// </summary>
        public uint MaximumSummedEvents
        {
            get
            {
                return _summedEventsSubmissionQueue.MaximumEvents;
            }
            set
            {
                _summedEventsSubmissionQueue.MaximumEvents = value;
            }
        }

        /// <summary>
        /// Maximum time between requests
        /// </summary>
        public const int MaxTimeBetweenRequests = 5 * 60;

        /// <summary>
        /// Default maximum number of attemps
        /// </summary>
        public const int MaxNumberOfAttempts = 3;

        /// <summary>
        /// Application session key
        /// </summary>
        internal const string ApplicationSessionKey = "application.session";

        /// <summary>
        /// Unique events submission queue
        /// </summary>
        internal readonly UniqueEventsSubmissionQueue _uniqueEventsSubmissionQueue;

        /// <summary>
        /// Summed events submission queue
        /// </summary>
        internal readonly SummedEventsSubmissionQueue _summedEventsSubmissionQueue;

        /// <summary>
        /// Startup event name that will be send on the application startup
        /// </summary>
        private const string StartupEventName = "Application Launches";

        /// <summary>
        /// Unique events submission URL
        /// </summary>
        public string UniqueEventsSubmissionUrl
        {
            get
            {
                return _uniqueEventsSubmissionQueue.SubmissionUrl;
            }
            set
            {
                _uniqueEventsSubmissionQueue.SubmissionUrl = value;
            }
        }

        /// <summary>
        /// Summed events submission URL
        /// </summary>
        public string SummedEventsSubmissionUrl
        {
            get
            {
                return _summedEventsSubmissionQueue.SubmissionUrl;
            }
            set
            {
                _summedEventsSubmissionQueue.SubmissionUrl = value;
            }
        }

        /// <summary>
        /// Determine if http client should ignore ssl validation
        /// </summary>
        public bool IgnoreSslValidation
        {
            set
            {
                _uniqueEventsSubmissionQueue.RequestHandler.IgnoreSslValidation = value;
                _summedEventsSubmissionQueue.RequestHandler.IgnoreSslValidation = value;
            }
        }

        /// <summary>
        /// List of unique events that will be added to next submission payload
        /// </summary>
        public LinkedList<UniqueEvent> UniqueEvents
        {
            get
            {
                return _uniqueEventsSubmissionQueue.Events;
            }
        }

        /// <summary>
        /// List of summed events that will be added to next submission payload
        /// </summary>
        internal LinkedList<SummedEvent> SummedEvents
        {
            get
            {
                return _summedEventsSubmissionQueue.Events;
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
        /// Backtrace attribute provider - shared reference that will be reused by Backtrace client to generate attributes.
        /// BacktraceMetrics implementation uses attribute provider to generate attributes dynamically when user adds a summed events.
        /// </summary>
        private readonly AttributeProvider _attributeProvider;

        /// <summary>
        /// Lock object
        /// </summary>
        private object _object = new object();

        /// <summary>
        /// Session id attribute
        /// </summary>
        private readonly string _sessionId;


        /// <summary>
        /// Create new Backtrace metrics instance
        /// </summary>
        /// <param name="attributeProvider">Backtrace client attribute provider</param>
        /// <param name="timeIntervalInSec">Update time interval in MS</param>
        /// <param name="uniqueEventsSubmissionUrl">Unique events submission URL</param>
        /// <param name="summedEventsSubmissionUrl">SummedEventsSubmissionUrl</param>
        public BacktraceMetrics(
            AttributeProvider attributeProvider,
            long timeIntervalInSec,
            string uniqueEventsSubmissionUrl,
            string summedEventsSubmissionUrl)
        {
            _attributeProvider = attributeProvider;
            _timeIntervalInSec = timeIntervalInSec;
            _uniqueEventsSubmissionQueue = new UniqueEventsSubmissionQueue(uniqueEventsSubmissionUrl, _attributeProvider);
            _summedEventsSubmissionQueue = new SummedEventsSubmissionQueue(summedEventsSubmissionUrl, _attributeProvider);
            _sessionId = SessionId.ToString();
        }

        /// <summary>
        /// Allows to override default http client for testing purposes.
        /// </summary>
        /// <param name="client">Http client implementation</param>
        internal void OverrideHttpClient(IBacktraceHttpClient client)
        {
            _uniqueEventsSubmissionQueue.RequestHandler = client;
            _summedEventsSubmissionQueue.RequestHandler = client;
        }

        /// <summary>
        /// Send startup event to Backtrace
        /// </summary>
        public void SendStartupEvent()
        {
            _uniqueEventsSubmissionQueue.StartWithEvent(DefaultUniqueAttributeName);
            _summedEventsSubmissionQueue.StartWithEvent(StartupEventName);
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
            bool shouldSendSummedEvents = false;
            bool shouldSendUniqueEvents = false;
            bool intervalUpdate = false;
            lock (_object)
            {

                intervalUpdate = (time - _lastUpdateTime) >= _timeIntervalInSec;
                shouldSendSummedEvents = _summedEventsSubmissionQueue.ReachedLimit();
                shouldSendUniqueEvents = _summedEventsSubmissionQueue.ReachedLimit();
                if (intervalUpdate == shouldSendSummedEvents == shouldSendUniqueEvents == false)
                {
                    // nothing more to update
                    return;
                }
                _lastUpdateTime = time;
            }
            if (intervalUpdate)
            {
                Send();
                return;
            }
            if (shouldSendSummedEvents)
            {
                _summedEventsSubmissionQueue.Send();
            }
            if (shouldSendUniqueEvents)
            {
                _summedEventsSubmissionQueue.Send();
            }
        }

        /// <summary>
        /// Trigger a manual send, will send all outgoing messages currently queued.
        /// </summary>
        public void Send()
        {
            _uniqueEventsSubmissionQueue.Send();
            _summedEventsSubmissionQueue.Send();
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
            if (!_uniqueEventsSubmissionQueue.ShouldProcessEvent(attributeName))
            {
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
            _uniqueEventsSubmissionQueue.Events.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Get number of events available right now in store
        /// </summary>
        /// <returns>number of events in store</returns>
        public int Count()
        {
            return _uniqueEventsSubmissionQueue.Count + _summedEventsSubmissionQueue.Count;
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
            if (!_summedEventsSubmissionQueue.ShouldProcessEvent(metricsGroupName))
            {
                return false;
            }

            var @event = new SummedEvent(metricsGroupName, DateTimeHelper.Timestamp(), attributes);
            _summedEventsSubmissionQueue.Events.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Check submission job failures and start submission for jobs that NextInvokeTime 
        /// is lower than time now.
        /// </summary>
        /// <param name="time">Current time</param>
        private void SendPendingSubmissionJobs(float time)
        {
            _uniqueEventsSubmissionQueue.SendPendingEvents(time);
            _summedEventsSubmissionQueue.SendPendingEvents(time);
        }

        /// <summary>
        /// Generate default unique events submission URL based on the configuration Universe name and token.
        /// </summary>
        /// <param name="universeName">Submission Universe name</param>
        /// <param name="token">Submission token</param>
        /// <returns>Unique events submission URL</returns>
        internal static string GetDefaultUniqueEventsUrl(string universeName, string token)
        {
            const string apiPrefix = "unique-events";
            return GetDefaultSubmissionUrl(apiPrefix, universeName, token);
        }

        /// <summary>
        /// Generate default summed events submission URL based on the configuration Universe name and token.
        /// </summary>
        /// <param name="universeName">Submission Universe name</param>
        /// <param name="token">Submission token</param>
        /// <returns>Summed events submission URL</returns>
        internal static string GetDefaultSummedEventsUrl(string universeName, string token)
        {
            const string apiPrefix = "summed-events";
            return GetDefaultSubmissionUrl(apiPrefix, universeName, token);
        }

        private static string GetDefaultSubmissionUrl(string serviceName, string universeName, string token)
        {
            return $"{DefaultSubmissionUrl}{serviceName}/submit?token={token}&universe={universeName}";
        }

        public void GetAttributes(IDictionary<string, string> attributes)
        {
            attributes[ApplicationSessionKey] = _sessionId;
        }
    }
}
