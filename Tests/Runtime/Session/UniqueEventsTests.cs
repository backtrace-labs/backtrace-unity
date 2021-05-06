using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime.Session
{
    public class UniqueEventsTests
    {
        // existing attribute name in Backtrace
        const string UniqueAttributeName = "scene.name";
        private readonly string _submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events?token=TOKEN";
        private AttributeProvider _attributeProvider = new AttributeProvider();
        private const int DefaultMaximumNumberOfEventsInStore = 10;

        [Test]
        public void BacktraceSessionUniqueEvents_ShouldAddCorrectlyUniqueEventWithEmptyAttributes_StoreValidUniqueEvent()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddUniqueEvent(UniqueAttributeName, null);

            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceSession.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, _attributeProvider.GenerateAttributes().Count);
        }

        [Test]
        public void BacktraceSessionUniqueEvents_ShouldAddCorrectlyUniqueEventWithAttributes_StoreValidUniqueEvent()
        {
            const string expectedAttributeName = "foo";
            const string expectedAttributeValue = "bar";
            var attributes = new Dictionary<string, string>() { { expectedAttributeName, expectedAttributeValue } };
            int expectedNumberOfAttributes = _attributeProvider.GenerateAttributes().Count + attributes.Count;

            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddUniqueEvent(UniqueAttributeName, attributes);

            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceSession.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, expectedNumberOfAttributes);
        }

        [Test]
        public void BacktraceSessionUniqueEvents_ShouldAddCorrectlyUniqueEvent_StoreValidUniqueEvent()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            var added = backtraceSession.AddUniqueEvent(UniqueAttributeName);

            Assert.IsTrue(added);
            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceSession.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, _attributeProvider.GenerateAttributes().Count);
        }

        [Test]
        public void BacktraceSessionUniqueEvents_ShouldPreventFromAddingEventIfThereIsNoAttribute_StoreValidUniqueEvent()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            var added = backtraceSession.AddUniqueEvent($"{UniqueAttributeName}-not-existing");

            Assert.IsFalse(added);
            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 0);

        }

        [Test]
        public void BacktraceSessionUniqueEvents_ShouldntAddEmptyUniqueEvent_UniqueEventsAreEmpty()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            var added = backtraceSession.AddUniqueEvent(string.Empty);

            Assert.IsFalse(added);
            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 0);
        }


        [Test]
        public void BacktraceSessionUniqueEvents_ShouldntAddNullableUniqueEvent_UniqueEventsAreEmpty()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddUniqueEvent(null);

            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 0);
        }

        [Test]
        public void BacktraceSessionUniqueEvents_UniqueEventAttributeValueDontChangeOverTime_UniqueEventAttributesStayTheSame()
        {
            const string initializationValue = "foo";
            const string updatedValue = "bar";
            _attributeProvider[UniqueAttributeName] = initializationValue;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddUniqueEvent(UniqueAttributeName);
            _attributeProvider[UniqueAttributeName] = updatedValue;

            var uniqueEvent = backtraceSession.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Attributes[UniqueAttributeName], initializationValue);
        }


        [Test]
        public void BacktraceSessionUniqueEvents_UniqueEventAttributeExistsAfterDeletingItFromAttributeProvider_UniqueEventAttributesStayTheSame()
        {

            const string initializationValue = "foo";
            _attributeProvider[UniqueAttributeName] = initializationValue;
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddUniqueEvent(UniqueAttributeName);
            _attributeProvider[UniqueAttributeName] = string.Empty;

            var uniqueEvent = backtraceSession.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Attributes[UniqueAttributeName], initializationValue);
        }
    }
}
