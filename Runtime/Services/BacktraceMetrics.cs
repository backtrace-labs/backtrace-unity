using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Json;
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
        /// Default time interval in min
        /// </summary>
        public const uint DefaultTimeIntervalInMin = 30;

        /// <summary>
        /// Default time interval in ms
        /// </summary>
        public const uint DefaultTimeIntervalInMs = DefaultTimeIntervalInMin * 60;
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
        /// Startup event name that will be send on the application startup
        /// </summary>
        private const string StartupEventName = "Application Launches";

        /// <summary>
        /// Default maximum number of attemps
        /// </summary>
        public const int MaxNumberOfAttemps = 3;

        /// <summary>
        /// Time between 
        /// </summary>
        public const int DefaultTimeInSecBetweenRequests = 10;

        /// <summary>
        /// Maximum time between requests
        /// </summary>
        public const int MaxTimeBetweenRequests = 5 * 60;

        /// <summary>
        /// Submission url
        /// </summary>
        public string SubmissionUrl { get; set; }

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
        public LinkedList<UniqueEvent> UniqueEvents { get; internal set; } = new LinkedList<UniqueEvent>();



        private int _numberOfDroppedRequests = 0;

        /// <summary>
        /// List of summed events that will be added to next submission payload
        /// </summary>
        internal readonly LinkedList<SummedEvent> SummedEvents = new LinkedList<SummedEvent>();

        /// <summary>
        /// Time interval in ms that algorithm uses to automatically send data to Backtrace
        /// </summary>
        private readonly long _timeIntervalInMs;

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
        /// List of submissions jobs that will store data with unique/summed events that we should try to retry
        /// </summary>
        private readonly List<MetricsSubmissionJob> _submissionJobs = new List<MetricsSubmissionJob>();

        /// <summary>
        /// Create new Backtrace metrics instance
        /// </summary>
        /// <param name="attributeProvider">Backtrace client attribute provider</param>
        /// <param name="uploadUrl">Upload URL</param>
        /// <param name="timeIntervalInMs">Update time interval in MS</param>
        public BacktraceMetrics(
            AttributeProvider attributeProvider,
            string uploadUrl,
            long timeIntervalInMs)
        {
            SubmissionUrl = uploadUrl;
            _attributeProvider = attributeProvider;
            _timeIntervalInMs = timeIntervalInMs;
            RequestHandler = new BacktraceHttpClient();
        }

        /// <summary>
        /// Send startup event to Backtrace
        /// </summary>
        public void SendStartupEvent()
        {
            var uniqueEventAttributes = _attributeProvider.GenerateAttributes();
            if (uniqueEventAttributes.TryGetValue(StartupUniqueEventName, out string value) && !string.IsNullOrEmpty(value))
            {
                UniqueEvents.AddLast(new UniqueEvent(StartupUniqueEventName, DateTimeHelper.Timestamp(), _attributeProvider.GenerateAttributes()));
            }

            SendPayload(
                UniqueEvents.ToArray(),
                new SummedEvent[1] { new SummedEvent(StartupEventName) });
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
            if (_timeIntervalInMs == 0)
            {
                return;
            }
            lock (_object)
            {

                var intervalUpdate = (time - _lastUpdateTime) >= _timeIntervalInMs;
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
            SendPayload(
              uniqueEvents: UniqueEvents.ToArray(),
              summedEvents: SummedEvents.ToArray());
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
            UniqueEvents.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Get number of events available right now in store
        /// </summary>
        /// <returns>number of events in store</returns>
        public int Count()
        {
            return UniqueEvents.Count + SummedEvents.Count;
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
            SummedEvents.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Check submission job failures and start submission for jobs that NextInvokeTime 
        /// is lower than time now.
        /// </summary>
        /// <param name="time">Current time</param>
        private void SendPendingSubmissionJobs(float time)
        {
            for (int index = 0; index < _submissionJobs.Count; index++)
            {
                var submissionJob = _submissionJobs.ElementAt(index);
                if (submissionJob.NextInvokeTime < time)
                {
                    SendPayload(submissionJob.UniqueEvents, submissionJob.SummedEvents, submissionJob.NumberOfAttemps);
                    _submissionJobs.RemoveAt(index);
                }
            }
        }

        private void SendPayload(UniqueEvent[] uniqueEvents, SummedEvent[] summedEvents, uint attemps = 0)
        {
            if (attemps == MaxNumberOfAttemps)
            {
                Debug.LogWarning("Backtrace Metrics: Cannot send session data to Backtrace due to submission issue.");
                return;
            }
            if (uniqueEvents.Length + summedEvents.Length == 0)
            {
                return;
            }
            var payload = CreateJsonPayload(uniqueEvents, summedEvents);

            // cleanup existing copy of events
            SummedEvents.Clear();

            // submit data to Backtrace
            RequestHandler.Post(SubmissionUrl, payload, (long statusCode, bool httpError, string response) =>
            {
                if (statusCode == 200)
                {
                    OnRequestCompleted();
                }
                else if (statusCode > 501 && statusCode != 505)
                {
                    _numberOfDroppedRequests++;
                    if (attemps + 1 == MaxNumberOfAttemps)
                    {
                        if (Count() + summedEvents.Length < MaximumEvents)
                        {
                            foreach (var summedEvent in summedEvents)
                            {
                                SummedEvents.AddFirst(summedEvent);
                            }
                        }

                        return;
                    }
                    // schedule a job on the specific server failure.
                    _submissionJobs.Add(new MetricsSubmissionJob()
                    {
                        UniqueEvents = uniqueEvents,
                        SummedEvents = summedEvents,
                        NextInvokeTime = CalculateNextRetryTime(attemps + 1),
                        NumberOfAttemps = attemps + 1
                    });

                }
            });
        }

        private double CalculateNextRetryTime(uint attemps)
        {
            const int jitterFraction = 1;
            const int backoffBase = 10;
            var value = DefaultTimeInSecBetweenRequests * Math.Pow(backoffBase, attemps);
            var retryLower = MathHelper.Clamp(value, 0, MaxTimeBetweenRequests);
            var retryUpper = retryLower + retryLower * jitterFraction;
            return MathHelper.Uniform(retryLower, retryUpper);
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

        private void OnRequestCompleted()
        {
            _numberOfDroppedRequests = 0;
            return;
        }

        private BacktraceJObject CreateJsonPayload(ICollection<UniqueEvent> uniqueEvents, ICollection<SummedEvent> summedEvents)
        {
            var jsonData = new BacktraceJObject();
            jsonData.Add("application", Application.productName);
            jsonData.Add("appversion", Application.version);
            jsonData.Add("metadata", CreatePayloadMetadata());

            // add unique events
            var uniqueEventsJson = new List<BacktraceJObject>();
            foreach (var uniqueEvent in uniqueEvents)
            {
                uniqueEventsJson.Add(uniqueEvent.ToJson());
                uniqueEvent.UpdateTimestamp(DateTimeHelper.Timestamp(), _attributeProvider.GenerateAttributes());
            }

            jsonData.Add("unique_events", uniqueEventsJson);

            // add summed events
            var summedEventJson = new List<BacktraceJObject>();
            var attributes = _attributeProvider.Get();
            foreach (var summedEvent in summedEvents)
            {
                summedEventJson.Add(summedEvent.ToJson(attributes));
            }

            jsonData.Add("session_events", summedEventJson);
            return jsonData;
        }

        private BacktraceJObject CreatePayloadMetadata()
        {
            var payload = new BacktraceJObject();
            payload.Add("dropped_events", _numberOfDroppedRequests);
            return payload;
        }
    }
}
