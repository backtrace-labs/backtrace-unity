using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using Backtrace.Unity.Model.Metrics.Mocks;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Model.Metrics
{
    public class BacktraceMetricsTests
    {
        const string MetricsEventName = "scene-changed";
        // existing attribute name in Backtrace
        const string UniqueAttributeName = "scene.name";
        private const string _submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events?token=TOKEN";
        private AttributeProvider _attributeProvider = new AttributeProvider();

        [OneTimeSetUp]
        public void Setup()
        {
            Debug.unityLogger.logEnabled = false;
        }
        [Test]
        public void BacktraceMetrics_ShouldTriggerUploadProcessOnSendMethodWithOnlySummedEvent_DataWasSendToTheService()
        {
            var jsonString = string.Empty;
            var submissionUrl = string.Empty;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
            backtraceMetrics.RequestHandler = new BacktraceHttpClientMock()
            {
                OnInvoke = (string url, BacktraceJObject json) =>
                {
                    jsonString = json.ToJson();
                    submissionUrl = url;
                }
            };

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.Send();

            Assert.IsNotEmpty(jsonString);
            Assert.AreEqual(submissionUrl, _submissionUrl);
            Assert.IsEmpty(backtraceMetrics.SummedEvents);
        }

        [Test]
        public void BacktraceMetrics_ShouldTriggerUploadProcessOnSendMethodWithOnlyUniqueEvent_DataWasSendToTheService()
        {
            var jsonString = string.Empty;
            var submissionUrl = string.Empty;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
            backtraceMetrics.RequestHandler = new BacktraceHttpClientMock()
            {
                OnInvoke = (string url, BacktraceJObject json) =>
                {
                    jsonString = json.ToJson();
                    submissionUrl = url;
                }
            };

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            backtraceMetrics.Send();

            Assert.IsNotEmpty(jsonString);
            Assert.AreEqual(submissionUrl, _submissionUrl);
            Assert.IsEmpty(backtraceMetrics.SummedEvents);
        }


        [Test]
        public void BacktraceMetrics_ShouldntTriggerUploadWhenDataIsNotAvailable_DataWasntSendToBacktrace()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.Send();

            Assert.IsFalse(requestHandler.Called);
        }

        [UnityTest]
        public IEnumerator BacktraceMetrics_ShouldTry3TimesOn503BeforeDroppingEvents_DataWasntSendToBacktrace()
        {
            const int expectedNumberOfEventsAfterFailure = 2;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
            var requestHandler = new BacktraceHttpClientMock()
            {
                StatusCode = 503
            };
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            backtraceMetrics.Send();
            for (int i = 0; i < BacktraceMetrics.MaxNumberOfAttemps; i++)
            {
                yield return new WaitForSeconds(1);
                // immidiately run next update
                var time = BacktraceMetrics.MaxTimeBetweenRequests + (BacktraceMetrics.MaxTimeBetweenRequests * i) + i + 1;
                backtraceMetrics.Tick(time);
            }

            yield return new WaitForSeconds(1);
            Assert.AreEqual(BacktraceMetrics.MaxNumberOfAttemps, requestHandler.NumberOfRequests);
            Assert.AreEqual(backtraceMetrics.Count(), expectedNumberOfEventsAfterFailure);
        }


        [UnityTest]
        public IEnumerator BacktraceMetrics_ShouldTry3TimesOn503AndDropSummedEventsOnMaximumNumberOfEvents_DataWasDeleted()
        {
            const int expectedNumberOfEventsAfterFailure = 1;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
            var requestHandler = new BacktraceHttpClientMock()
            {
                StatusCode = 503
            };
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            backtraceMetrics.MaximumEvents = expectedNumberOfEventsAfterFailure;

            backtraceMetrics.Send();
            for (int i = 0; i < BacktraceMetrics.MaxNumberOfAttemps; i++)
            {
                yield return new WaitForSeconds(1);
                // immidiately run next update
                var time = BacktraceMetrics.MaxTimeBetweenRequests + (BacktraceMetrics.MaxTimeBetweenRequests * i) + i + 1;
                backtraceMetrics.Tick(time);
            }

            yield return new WaitForSeconds(1);
            Assert.AreEqual(BacktraceMetrics.MaxNumberOfAttemps, requestHandler.NumberOfRequests);
            Assert.AreEqual(backtraceMetrics.Count(), expectedNumberOfEventsAfterFailure);
        }

        [Test]
        public void BacktraceMetrics_ShouldTryOnlyOnceOnHttpFailure_DataWasntSendToBacktrace()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
            var requestHandler = new BacktraceHttpClientMock()
            {
                IsHttpError = true
            };
            backtraceMetrics.RequestHandler = requestHandler;
            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            backtraceMetrics.Send();

            Assert.AreEqual(requestHandler.NumberOfRequests, 1);
        }


        [Test]
        public void BacktraceMetrics_ShouldSkipMoreUniqueEventsWhenHitTheLimit_DataWasntSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int numberOfTestEvents = 10;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0)
            {
                MaximumEvents = maximumNumberOfEvents
            };

            for (int i = 0; i < numberOfTestEvents; i++)
            {
                backtraceMetrics.AddUniqueEvent($"{UniqueAttributeName} {i}", new Dictionary<string, string>() { { $"{UniqueAttributeName} {i}", "value" } });
            }

            Assert.AreEqual(maximumNumberOfEvents, backtraceMetrics.Count());
        }

        [Test]
        public void BacktraceMetrics_ShouldSkipMoreSummedEventsWhenHitTheLimit_DataWasntSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int numberOfTestEvents = 10;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0)
            {
                MaximumEvents = maximumNumberOfEvents
            };

            for (int i = 0; i < numberOfTestEvents; i++)
            {
                backtraceMetrics.AddSummedEvent($"{MetricsEventName} {i}");
            }

            Assert.AreEqual(maximumNumberOfEvents, backtraceMetrics.Count());
        }

        [Test]
        public void BacktraceMetrics_ShouldTriggerUploadViaTickMethodWhenReachedMaximumNumberOfEvents_DataWasSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int defaultTimeIntervalInSec = 10;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, defaultTimeIntervalInSec)
            {
                MaximumEvents = maximumNumberOfEvents
            };
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;
            for (int i = 0; i < maximumNumberOfEvents; i++)
            {
                backtraceMetrics.AddSummedEvent($"{MetricsEventName} {i}");
            }

            backtraceMetrics.Tick(defaultTimeIntervalInSec + 1);

            Assert.AreEqual(0, backtraceMetrics.Count());
            Assert.AreEqual(1, requestHandler.NumberOfRequests);
        }

        [Test]
        public void BacktraceMetrics_ShouldntTriggerDownloadViaTickMethodWhenDidntReachMaximumNumberOfEvents_DataWasSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int expectedNumberOfEvents = 2;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 1)
            {
                MaximumEvents = maximumNumberOfEvents
            };
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;
            for (int i = 0; i < expectedNumberOfEvents; i++)
            {
                backtraceMetrics.AddSummedEvent($"{MetricsEventName} {i}");
            }

            backtraceMetrics.Tick(0);

            Assert.AreEqual(expectedNumberOfEvents, backtraceMetrics.Count());
            Assert.AreEqual(0, requestHandler.NumberOfRequests);
        }


        [Test]
        public void BacktraceMetrics_ShouldTriggerUploadAfterTimeIntervalHit_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            const int expectedNumberOfEvents = 1;// unique event
            const int expectedNumberOfRequests = 1; //should combine data together
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, timeInterval);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            backtraceMetrics.Tick(timeInterval + 1);


            Assert.AreEqual(expectedNumberOfEvents, backtraceMetrics.Count());
            Assert.AreEqual(expectedNumberOfRequests, requestHandler.NumberOfRequests);
        }

        [Test]
        public void BacktraceMetrics_ShouldntTriggerDownloadBeforeTimeIntervalHit_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            const int numberOfAddedEvents = 2;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, timeInterval);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            backtraceMetrics.Tick(timeInterval - 1);

            Assert.AreEqual(numberOfAddedEvents, backtraceMetrics.Count());
            Assert.AreEqual(0, requestHandler.NumberOfRequests);
        }


        [Test]
        public void BacktraceMetrics_ShouldTriggerUploadAfterTimeIntervalHitAgain_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, timeInterval);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            backtraceMetrics.Tick(timeInterval + 1);

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            backtraceMetrics.Tick((timeInterval * 2) + 1);

            // we added two unique events - but because we added two the same reports
            // they should be combined
            Assert.AreEqual(1, backtraceMetrics.Count());
            Assert.AreEqual(2, requestHandler.NumberOfRequests);
        }
        [Test]
        public void BacktraceMetrics_ShouldntTriggerDownloadAfterTimeIntervalFirstHit_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, timeInterval);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            backtraceMetrics.Tick(timeInterval + 1);

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            backtraceMetrics.Tick(timeInterval + 2);

            Assert.AreEqual(2, backtraceMetrics.Count());
            Assert.AreEqual(1, requestHandler.NumberOfRequests);
        }

        [Test]
        public void BacktraceMetricsDefaultEvent_ShouldSendDefaultEventOnTheApplicationStartup_DataWasSendToBacktrace()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 10);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceMetrics.RequestHandler = requestHandler;

            backtraceMetrics.SendStartupEvent();

            Assert.AreEqual(1, requestHandler.NumberOfRequests);
        }

    }
}
