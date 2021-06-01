using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using Backtrace.Unity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Backtrace.Unity.Model.Metrics
{
    internal abstract class MetricsSubmissionQueue<T> where T : EventAggregationBase
    {
        /// <summary>
        /// Number of events in the queue
        /// </summary>
        public int Count
        {
            get
            {
                return Events.Count;
            }
        }

        /// <summary>
        /// Maximum number of events in store. If number of events in store hit the limit
        /// BacktraceMetrics instance will send data to Backtrace.
        /// </summary>
        public uint MaximumEvents { get; set; } = 50;

        /// <summary>
        /// Time between 
        /// </summary>
        public const int DefaultTimeInSecBetweenRequests = 10;

        /// <summary>
        /// Submission Queue name
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// List of submissions jobs that will store data with unique/summed events that we should try to retry
        /// </summary>
        private readonly List<MetricsSubmissionJob<T>> _submissionJobs = new List<MetricsSubmissionJob<T>>();

        /// <summary>
        /// List of events in the event queue
        /// </summary>
        internal LinkedList<T> Events = new LinkedList<T>();

        /// <summary>
        /// Number of dropped requests
        /// </summary>
        private int _numberOfDroppedRequests = 0;

        /// <summary>
        /// Http client
        /// </summary>
        internal IBacktraceHttpClient RequestHandler = new BacktraceHttpClient();

        /// <summary>
        /// Submission url
        /// </summary>
        internal string SubmissionUrl { get; set; }

        private readonly string _applicationName = Application.productName;
        private readonly string _applicationVersion = Application.version;

        internal MetricsSubmissionQueue(string name, string submissionUrl)
        {
            _name = name;
            SubmissionUrl = submissionUrl;
        }

        public bool ReachedLimit()
        {
            return MaximumEvents == Events.Count && MaximumEvents != 0;
        }

        /// <summary>
        /// Determine if Backtrace Metrics can add next event to store
        /// </summary>
        /// <param name="name">event name</param>
        /// <returns>True if we're able to add. Otherwise false.</returns>
        public bool ShouldProcessEvent(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Skipping report: attribute name is null or empty");
                return false;
            }
            if (ReachedLimit())
            {
                Debug.LogWarning("Skipping report: Reached store limit.");
                return false;
            }
            return true;
        }

        public abstract void StartWithEvent(string eventName);
        internal void Send()
        {
            SendPayload(new LinkedList<T>(Events));
        }
        internal void SendPayload(ICollection<T> events, uint attempts = 0)
        {
            if (events.Count == 0)
            {
                return;
            }
            var payload = CreateJsonPayload(events);

            RequestHandler.Post(SubmissionUrl, payload, (long statusCode, bool httpError, string response) =>
            {
                if (statusCode == 200)
                {
                    OnRequestCompleted();
                }
                else if (statusCode > 501 && statusCode != 505)
                {
                    _numberOfDroppedRequests++;
                    if (attempts + 1 == BacktraceMetrics.MaxNumberOfAttempts)
                    {
                        OnMaximumAttemptsReached(events);
                        return;
                    }
                    // schedule a job on the specific server failure.
                    _submissionJobs.Add(new MetricsSubmissionJob<T>()
                    {
                        Events = events,
                        NextInvokeTime = CalculateNextRetryTime(attempts + 1) + Time.unscaledTime,
                        NumberOfAttempts = attempts + 1
                    });

                }
            });
        }

        public void SendPendingEvents(float time)
        {
            for (int index = 0; index < _submissionJobs.Count; index++)
            {
                var submissionJob = _submissionJobs.ElementAt(index);
                if (submissionJob.NextInvokeTime < time)
                {
                    SendPayload(submissionJob.Events, submissionJob.NumberOfAttempts);
                    _submissionJobs.RemoveAt(index);
                }
            }
        }

        internal virtual void OnMaximumAttemptsReached(ICollection<T> events)
        {
            return;
        }

        internal abstract IEnumerable<BacktraceJObject> GetEventsPayload(ICollection<T> events);

        internal virtual BacktraceJObject CreateJsonPayload(ICollection<T> events)
        {
            var jsonData = new BacktraceJObject();
            jsonData.Add("application", _applicationName);
            jsonData.Add("appversion", _applicationVersion);
            jsonData.Add("metadata", CreatePayloadMetadata());
            jsonData.Add(_name, GetEventsPayload(events));
            return jsonData;
        }

        private double CalculateNextRetryTime(uint attemps)
        {
            const int jitterFraction = 1;
            const int backoffBase = 10;
            var value = DefaultTimeInSecBetweenRequests * Math.Pow(backoffBase, attemps);
            var retryLower = MathHelper.Clamp(value, 0, BacktraceMetrics.MaxTimeBetweenRequests);
            var retryUpper = retryLower + retryLower * jitterFraction;
            return MathHelper.Uniform(retryLower, retryUpper);
        }

        private BacktraceJObject CreatePayloadMetadata()
        {
            var payload = new BacktraceJObject();
            payload.Add("dropped_events", _numberOfDroppedRequests);
            return payload;
        }

        private void OnRequestCompleted()
        {
            _numberOfDroppedRequests = 0;
            return;
        }

    }
}
