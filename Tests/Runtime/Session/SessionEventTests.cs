using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime.Session
{
    public class SessionEventTests
    {
        private readonly string _submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events?token=TOKEN";
        private AttributeProvider _attributeProvider = new AttributeProvider();
        private const int DefaultMaximumNumberOfEventsInStore = 10;

        [Test]
        public void BacktraceSessionSessionEvents_ShouldAddCorrectlyUniqueEvent_StoreValidUniqueEvent()
        {
            const string sessionEventName = "scene-changed";
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, DefaultMaximumNumberOfEventsInStore);

            backtraceSession.AddSessionEvent(sessionEventName);

            Assert.AreEqual(backtraceSession.SessionEvents.Count, 1);
            var sessionEvent = backtraceSession.SessionEvents.First.Value;
            Assert.AreEqual(sessionEvent.Name, sessionEventName);
            Assert.AreNotEqual(sessionEvent.Timestamp, 0);
        }


        [Test]
        public void BacktraceSessionSessionEvents_ShouldPreventFromAddingMultipleTheSameEvents_LastValueIsAvailable()
        {
            const string sessionEventName = "scene-changed";
            const int numberOfTheSameEvents = 3;
            string expectedLastKeyName = $"key-{numberOfTheSameEvents}";
            string expectedLastKeyValue = numberOfTheSameEvents.ToString();
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, DefaultMaximumNumberOfEventsInStore);

            for (int i = 0; i <= numberOfTheSameEvents; i++)
            {
                backtraceSession.AddSessionEvent(sessionEventName, new Dictionary<string, string>() { { $"key-{i}", i.ToString() } });
            }

            Assert.AreEqual(backtraceSession.SessionEvents.Count, 1);
            var sessionEvent = backtraceSession.SessionEvents.First.Value;
            Assert.AreEqual(sessionEvent.Name, sessionEventName);
            Assert.AreEqual(expectedLastKeyValue, sessionEvent.Attributes[expectedLastKeyName]);
            Assert.AreEqual(1, sessionEvent.Attributes.Count);
        }

        [Test]
        public void BacktraceSessionSessionEvents_ShouldntAddEmptyUniqueEvent_UniqueEventsAreEmpty()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, DefaultMaximumNumberOfEventsInStore);

            backtraceSession.AddSessionEvent(string.Empty);

            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 0);
        }


        [Test]
        public void BacktraceSessionSessionEvents_ShouldntAddNullableUniqueEvent_UniqueEventsAreEmpty()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0, DefaultMaximumNumberOfEventsInStore);

            backtraceSession.AddUniqueEventAttribute(null);

            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 0);
        }
    }
}
