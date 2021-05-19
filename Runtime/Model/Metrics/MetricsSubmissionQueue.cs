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
        public uint MaximumEvents { get; set; } = 350;

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
        internal IBacktraceHttpClient RequestHandler;

        /// <summary>
        /// Submission url
        /// </summary>
        private readonly string _submissionUrl;

        internal MetricsSubmissionQueue(string name, string universeName, string token, IBacktraceHttpClient httpClient)
        {
            _name = name;
            RequestHandler = httpClient;
            _submissionUrl = $"{_name}/submit?token={token}&universe={universeName}";
        }

        public abstract void StartWithEvent(string eventName);
        internal void Send()
        {
            SendPayload(new LinkedList<T>(Events));
        }
        internal void SendPayload(ICollection<T> events, uint attemps = 0)
        {
            if (events.Count == 0)
            {
                return;
            }
            var payload = CreateJsonPayload(events);

            RequestHandler.Post(_submissionUrl, payload, (long statusCode, bool httpError, string response) =>
            {
                if (statusCode == 200)
                {
                    OnRequestCompleted();
                }
                else if (statusCode > 501 && statusCode != 505)
                {
                    _numberOfDroppedRequests++;
                    if (attemps + 1 == BacktraceMetrics.MaxNumberOfAttemps)
                    {
                        OnMaximumAttemptsReached(events);
                        return;
                    }
                    // schedule a job on the specific server failure.
                    _submissionJobs.Add(new MetricsSubmissionJob<T>()
                    {
                        Events = events,
                        NextInvokeTime = CalculateNextRetryTime(attemps + 1),
                        NumberOfAttemps = attemps + 1
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
                    SendPayload(submissionJob.Events, submissionJob.NumberOfAttemps);
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
            jsonData.Add("application", Application.productName);
            jsonData.Add("appversion", Application.version);
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
