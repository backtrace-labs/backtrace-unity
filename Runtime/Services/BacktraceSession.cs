﻿using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Json;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Services
{
    internal sealed class BacktraceSession : IBacktraceSession
    {
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

        private int _numberOfDroppedRequests = 0;

        /// <summary>
        /// List of unique events that will be added to next session submission payload
        /// </summary>
        internal readonly LinkedList<UniqueEvent> UniqueEvents = new LinkedList<UniqueEvent>();

        /// <summary>
        /// List of session events that will be added to next session submission payload
        /// </summary>
        internal readonly LinkedList<SessionEvent> SessionEvents = new LinkedList<SessionEvent>();

        /// <summary>
        /// Time interval in ms that algorithm uses to automatically send data to Backtrace
        /// </summary>
        private readonly long _timeIntervalInMs;

        /// <summary>
        /// Last update time that will be updated after each update start
        /// </summary>
        private float _lastUpdateTime = 0;

        /// <summary>
        /// Maximum number of events in store. If number of events in store hit the limit
        /// BacktraceSession instance will send data to Backtrace.
        /// </summary>
        private uint _maximumNumberOfEventsInStore;

        /// <summary>
        /// Http client
        /// </summary>
        internal IBacktraceHttpClient RequestHandler;

        /// <summary>
        /// Backtrace attribute provider - shared reference that will be reused by Backtrace client to generate attributes.
        /// BacktraceSession implementation uses attribute provider to generate attributes dynamically when user adds a session events.
        /// </summary>
        private readonly AttributeProvider _attributeProvider;

        /// <summary>
        /// Lock object
        /// </summary>
        private object _object = new object();

        private readonly List<SessionSubmissionJob> _submissionJobs = new List<SessionSubmissionJob>();

        /// <summary>
        /// Create new Backtrace session instance
        /// </summary>
        /// <param name="attributeProvider">Backtrace client attribute provider</param>
        /// <param name="uploadUrl">Upload URL</param>
        /// <param name="timeIntervalInMs">Update time interval in MS</param>
        /// <param name="maximumNumberOfEventsInStore">Determine how many events we can store in event store</param>
        public BacktraceSession(
            AttributeProvider attributeProvider,
            string uploadUrl,
            long timeIntervalInMs,
            uint maximumNumberOfEventsInStore)
        {
            SubmissionUrl = uploadUrl;
            _attributeProvider = attributeProvider;
            _maximumNumberOfEventsInStore = maximumNumberOfEventsInStore;
            _timeIntervalInMs = timeIntervalInMs;
            RequestHandler = new BacktraceHttpClient();
        }

        /// <summary>
        /// Send startup event ot Backtrace
        /// </summary>
        public void SendStartupEvent()
        {
            SendPayload(new UniqueEvent[0], new SessionEvent[1] { new SessionEvent(StartupEventName) });
        }

        /// <summary>
        /// Backtrace session update interval method used by Backtrace client to update session events time
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
                var reachedEventLimit = _maximumNumberOfEventsInStore == Count() && _maximumNumberOfEventsInStore != 0;
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
        /// Force BacktraceSession to Send data to Backtrace
        /// </summary>
        public void Send()
        {
            SendPayload(
              uniqueEvents: UniqueEvents.ToArray(),
              sessionEvents: SessionEvents.ToArray());
        }

        /// <summary>
        /// Add unique event to next Backtrace session request
        /// </summary>
        /// <param name="attributeName">attribute name</param>
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
            return UniqueEvents.Count + SessionEvents.Count;
        }

        /// <summary>
        /// Add session event to next Backtrace session request
        /// </summary>
        /// <param name="eventName">session event name</param>
        public bool AddSessionEvent(string eventName, IDictionary<string, string> attributes = null)
        {
            if (!ShouldProcessEvent(eventName))
            {
                Debug.LogWarning("Skipping report: Reached store limit or event has empty name.");
                return false;
            }

            var @event = new SessionEvent(eventName, DateTimeHelper.Timestamp(), attributes);
            SessionEvents.AddLast(@event);
            return true;
        }

        /// <summary>
        /// Clean unique and session events stored in Backtrace Session
        /// </summary>
        public void ClearEvents()
        {
            UniqueEvents.Clear();
            SessionEvents.Clear();
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
                    SendPayload(submissionJob.UniqueEvents, submissionJob.SessionEvents, submissionJob.NumberOfAttemps);
                    _submissionJobs.RemoveAt(index);
                }
            }
        }

        private void SendPayload(UniqueEvent[] uniqueEvents, SessionEvent[] sessionEvents, uint attemps = 0)
        {
            if (attemps == MaxNumberOfAttemps)
            {
                Debug.LogWarning("Backtrace Session: Cannot send session data to Backtrace due to submission issue.");
                return;
            }
            if (uniqueEvents.Length + sessionEvents.Length == 0)
            {
                return;
            }
            var payload = CreateJsonPayload(uniqueEvents, sessionEvents);

            // cleanup existing copy of events
            ClearEvents();

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
                        return;
                    }
                    // schedule a job on the specific server failure.
                    _submissionJobs.Add(new SessionSubmissionJob()
                    {
                        UniqueEvents = uniqueEvents,
                        SessionEvents = sessionEvents,
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
        /// Determine if Backtrace Session can add next event to store
        /// </summary>
        /// <param name="name">event name</param>
        /// <returns>True if we're able to add. Otherwise false.</returns>
        private bool ShouldProcessEvent(string name)
        {
            return !string.IsNullOrEmpty(name) && (_maximumNumberOfEventsInStore == 0 || (Count() + 1 <= _maximumNumberOfEventsInStore));
        }

        private void OnRequestCompleted()
        {
            _numberOfDroppedRequests = 0;
            return;
        }

        private BacktraceJObject CreateJsonPayload(ICollection<UniqueEvent> uniqueEvents, ICollection<SessionEvent> sessionEvents)
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
            }

            jsonData.Add("unique_events", uniqueEventsJson);

            // add session events
            var sessionEventsJson = new List<BacktraceJObject>();
            var attributes = _attributeProvider.Get();
            foreach (var sessionEvent in sessionEvents)
            {
                sessionEventsJson.Add(sessionEvent.ToJson(attributes));
            }

            jsonData.Add("session_events", sessionEventsJson);
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
