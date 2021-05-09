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

        [Test]
        public void BacktraceSessionSessionEvents_ShouldAddCorrectlySessionEvent_StoreValidSessionEvent()
        {
            const string sessionEventName = "scene-changed";
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddSessionEvent(sessionEventName);

            Assert.AreEqual(backtraceSession.SessionEvents.Count, 1);
            var sessionEvent = backtraceSession.SessionEvents.First.Value;
            Assert.AreEqual(sessionEvent.Name, sessionEventName);
            Assert.AreNotEqual(sessionEvent.Timestamp, 0);
        }

        [Test]
        public void BacktraceSessionSessionEvents_ShouldAddCorrectlySessionEventWithAttributes_StoreValidSessionEvent()
        {
            const string sessionEventName = "scene-changed";
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);
            const string expectedAttributeName = "foo";
            const string expectedAttributeValue = "bar";
            var sessionAttributes = new Dictionary<string, string>() { { expectedAttributeName, expectedAttributeValue } };
            backtraceSession.AddSessionEvent(sessionEventName, sessionAttributes);

            Assert.AreEqual(backtraceSession.SessionEvents.Count, 1);
            var sessionEvent = backtraceSession.SessionEvents.First.Value;
            Assert.AreEqual(sessionEvent.Name, sessionEventName);
            Assert.AreNotEqual(sessionEvent.Timestamp, 0);
            Assert.IsTrue(sessionEvent.Attributes.ContainsKey(expectedAttributeName));
            Assert.AreEqual(expectedAttributeValue, sessionEvent.Attributes[expectedAttributeName]);
        }

        [Test]
        public void BacktraceSessionSessionEvents_ShouldntAddEmptySessionEvent_SessionEventsAreEmpty()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddSessionEvent(string.Empty);

            Assert.AreEqual(backtraceSession.SessionEvents.Count, 0);
        }


        [Test]
        public void BacktraceSessionSessionEvents_ShouldntAddNullableSessionEvent_SessionEventsAreEmpty()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddSessionEvent(null);

            Assert.AreEqual(backtraceSession.SessionEvents.Count, 0);
        }
    }
}
