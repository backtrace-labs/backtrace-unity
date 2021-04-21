using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using Backtrace.Unity.Tests.Runtime.Session.Mocks;
using NUnit.Framework;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime.Session
{
    public class BacktraceSessionTests
    {
        const string SessionEventName = "scene-changed";
        // existing attribute name in Backtrace
        const string UniqueAttributeName = "scene.name";
        private const string _submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events?token=TOKEN";
        private AttributeProvider _attributeProvider = new AttributeProvider();

        [Test]
        public void BacktraceSession_ShouldTriggerUploadProcessOnSendMethodWithOnlySessionEvent_DataWasSendToTheService()
        {
            var jsonString = string.Empty;
            var submissionUrl = string.Empty;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, 1);
            backtraceSession.RequestHandler = new BacktraceHttpClientMock()
            {
                OnIvoke = (string url, BacktraceJObject json) =>
                {
                    jsonString = json.ToJson();
                    submissionUrl = url;
                }
            };

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.Send();

            Assert.IsNotEmpty(jsonString);
            Assert.AreEqual(submissionUrl, _submissionUrl);
            Assert.IsEmpty(backtraceSession.SessionEvents);
        }

        [Test]
        public void BacktraceSession_ShouldTriggerUploadProcessOnSendMethodWithOnlyUniqueEvent_DataWasSendToTheService()
        {
            var jsonString = string.Empty;
            var submissionUrl = string.Empty;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, 1);
            backtraceSession.RequestHandler = new BacktraceHttpClientMock()
            {
                OnIvoke = (string url, BacktraceJObject json) =>
                {
                    jsonString = json.ToJson();
                    submissionUrl = url;
                }
            };

            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);
            backtraceSession.Send();

            Assert.IsNotEmpty(jsonString);
            Assert.AreEqual(submissionUrl, _submissionUrl);
            Assert.IsEmpty(backtraceSession.SessionEvents);
        }


        [Test]
        public void BacktraceSession_ShouldntTriggerUploadWhenDataIsNotAvailable_DataWasntSendToBacktrace()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, 1);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;

            backtraceSession.Send();

            Assert.IsFalse(requestHandler.Called);
        }

        [Test]
        public void BacktraceSession_ShouldTry3TimesOn503BeforeDroppingEvents_DataWasntSendToBacktrace()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, 1);
            var requestHandler = new BacktraceHttpClientMock()
            {
                StatusCode = 503
            };
            backtraceSession.RequestHandler = requestHandler;
            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);
            backtraceSession.Send();

            Assert.AreEqual(requestHandler.NumberOfRequests, BacktraceSession.DefaultNumberOfRetries);
        }

        [Test]
        public void BacktraceSession_ShouldTryAtLeastOneOnHttpFailure_DataWasntSendToBacktrace()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, 1);
            var requestHandler = new BacktraceHttpClientMock()
            {
                IsHttpError = true
            };
            backtraceSession.RequestHandler = requestHandler;
            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);
            backtraceSession.Send();

            Assert.AreEqual(requestHandler.NumberOfRequests, 1);
        }


        [Test]
        public void BacktraceSession_ShouldSkipMoreUniqueEventsWhenHitTheLimit_DataWasntSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int numberOfTestEvents = 10;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, maximumNumberOfEvents);

            for (int i = 0; i < numberOfTestEvents; i++)
            {
                backtraceSession.AddUniqueEventAttribute($"{UniqueAttributeName} {i}", new Dictionary<string, string>() { { $"{UniqueAttributeName} {i}", "value" } });
            }

            Assert.AreEqual(maximumNumberOfEvents, backtraceSession.Count());
        }

        [Test]
        public void BacktraceSession_ShouldSkipMoreSessionEventsWhenHitTheLimit_DataWasntSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int numberOfTestEvents = 10;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, maximumNumberOfEvents);

            for (int i = 0; i < numberOfTestEvents; i++)
            {
                backtraceSession.AddSessionEvent($"{SessionEventName} {i}");
            }

            Assert.AreEqual(maximumNumberOfEvents, backtraceSession.Count());
        }

        [Test]
        public void BacktraceSession_ShouldTriggerDownloadViaTickMethodWhenReachedMaximumNumberOfEvents_DataWasSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, maximumNumberOfEvents);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;
            for (int i = 0; i < maximumNumberOfEvents; i++)
            {
                backtraceSession.AddSessionEvent($"{SessionEventName} {i}");
            }

            backtraceSession.Tick(0);

            Assert.AreEqual(0, backtraceSession.Count());
            Assert.AreEqual(1, requestHandler.NumberOfRequests);
        }

        [Test]
        public void BacktraceSession_ShouldntTriggerDownloadViaTickMethodWhenDidntReachMaximumNumberOfEvents_DataWasSendToBacktrace()
        {
            const int maximumNumberOfEvents = 3;
            const int expectedNumberOfEvents = 2;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 1, maximumNumberOfEvents);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;
            for (int i = 0; i < expectedNumberOfEvents; i++)
            {
                backtraceSession.AddSessionEvent($"{SessionEventName} {i}");
            }

            backtraceSession.Tick(0);

            Assert.AreEqual(expectedNumberOfEvents, backtraceSession.Count());
            Assert.AreEqual(0, requestHandler.NumberOfRequests);
        }


        [Test]
        public void BacktraceSession_ShouldTriggerDownloadAfterTimeIntervalHit_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, timeInterval, 3);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);

            backtraceSession.Tick(timeInterval + 1);

            Assert.AreEqual(0, backtraceSession.Count());
            Assert.AreEqual(1, requestHandler.NumberOfRequests);
        }

        [Test]
        public void BacktraceSession_ShouldntTriggerDownloadBeforeTimeIntervalHit_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            const int numberOfAddedEvents = 2;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, timeInterval, 3);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);

            backtraceSession.Tick(timeInterval - 1);

            Assert.AreEqual(numberOfAddedEvents, backtraceSession.Count());
            Assert.AreEqual(0, requestHandler.NumberOfRequests);
        }


        [Test]
        public void BacktraceSession_ShouldTriggerDownloadAfterTimeIntervalHitAgain_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, timeInterval, 3);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);

            backtraceSession.Tick(timeInterval + 1);

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);

            backtraceSession.Tick((timeInterval * 2) + 1);

            Assert.AreEqual(0, backtraceSession.Count());
            Assert.AreEqual(2, requestHandler.NumberOfRequests);
        }
        [Test]
        public void BacktraceSession_ShouldntTriggerDownloadAfterTimeIntervalFirstHit_DataWasSendToBacktrace()
        {
            const int timeInterval = 10;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, timeInterval, 3);
            var requestHandler = new BacktraceHttpClientMock();
            backtraceSession.RequestHandler = requestHandler;

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);

            backtraceSession.Tick(timeInterval + 1);

            backtraceSession.AddSessionEvent(SessionEventName);
            backtraceSession.AddUniqueEventAttribute(UniqueAttributeName);

            backtraceSession.Tick(timeInterval + 2);

            Assert.AreEqual(2, backtraceSession.Count());
            Assert.AreEqual(1, requestHandler.NumberOfRequests);
        }
    }
}
