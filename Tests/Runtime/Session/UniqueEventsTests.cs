using Backtrace.Unity.Common;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Model.Session;
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

        [TearDown]
        public void Cleanup()
        {
            _attributeProvider = new AttributeProvider();
        }

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
        public void BacktraceSessionUniqueEvents_ShouldAddCorrectlyUniqueEventWithoutAttributes_StoreValidUniqueEvent()
        {
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            backtraceSession.AddUniqueEvent(UniqueAttributeName);

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
        public void BacktraceSessionUniqueEvents_ShouldAddEventIfAttributeIsDefinedInCustomAttributes_StoreValidUniqueEvents()
        {
            var expectedAttributeName = "foo";
            var backtraceSession = new BacktraceSession(_attributeProvider, _submissionUrl, 0);

            var added = backtraceSession.AddUniqueEvent(expectedAttributeName, new Dictionary<string, string>() { { expectedAttributeName, expectedAttributeName } });

            Assert.IsTrue(added);
            Assert.AreEqual(backtraceSession.UniqueEvents.Count, 1);
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

        [Test]
        public void BacktraceSessionUniqueEvent_ShouldUpdateTimeStamp_UniqueEventIsUpdated()
        {
            const int nextTime = 1000;
            var timestamp = DateTimeHelper.Timestamp();
            var expectedNewTimestamp = timestamp + nextTime;
            var attributes = _attributeProvider.GenerateAttributes();

            var uniqueEvent = new UniqueEvent(UniqueAttributeName, timestamp, attributes);
            uniqueEvent.UpdateTimestamp(expectedNewTimestamp, attributes);

            Assert.AreEqual(expectedNewTimestamp, uniqueEvent.Timestamp);
        }


        [Test]
        public void BacktraceSessionUniqueEvent_ShouldUpdateAttributes_UniqueEventIsUpdated()
        {
            const int nextTime = 1000;
            const string newAttributeName = "foo";
            var timestamp = DateTimeHelper.Timestamp();
            var expectedNewTimestamp = timestamp + nextTime;
            var attributes = _attributeProvider.GenerateAttributes();


            var uniqueEvent = new UniqueEvent(UniqueAttributeName, timestamp, attributes);
            _attributeProvider[newAttributeName] = newAttributeName;
            uniqueEvent.UpdateTimestamp(expectedNewTimestamp, _attributeProvider.GenerateAttributes());

            Assert.AreEqual(newAttributeName, uniqueEvent.Attributes[newAttributeName]);
        }

        [Test]
        public void BacktraceSessionUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeValueIsEmpty_UniqueEventIsUpdated()
        {
            const int nextTime = 1000;
            const string uniqueEventName = "BacktraceSessionUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeValueIsEmpty_UniqueEventIsUpdated";
            var attributeProvider = new AttributeProvider();
            var timestamp = DateTimeHelper.Timestamp();
            var expectedNewTimestamp = timestamp + nextTime;
            attributeProvider[uniqueEventName] = uniqueEventName;
            var attributes = attributeProvider.GenerateAttributes();

            var uniqueEvent = new UniqueEvent(uniqueEventName, timestamp, attributes);
            attributeProvider[uniqueEventName] = string.Empty;

            uniqueEvent.UpdateTimestamp(expectedNewTimestamp, attributeProvider.GenerateAttributes());

            Assert.AreEqual(uniqueEventName, uniqueEvent.Attributes[uniqueEventName]);
        }

        [Test]
        public void BacktraceSessionUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeDoesntExist_UniqueEventIsUpdated()
        {
            const int nextTime = 1000;
            const string uniqueEventName = "BacktraceSessionUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeDoesntExist_UniqueEventIsUpdated";
            var timestamp = DateTimeHelper.Timestamp();
            var expectedNewTimestamp = timestamp + nextTime;

            var uniqueEvent = new UniqueEvent(uniqueEventName, timestamp, new Dictionary<string, string> { { uniqueEventName, uniqueEventName } });

            uniqueEvent.UpdateTimestamp(expectedNewTimestamp, null);

            Assert.AreEqual(uniqueEventName, uniqueEvent.Attributes[uniqueEventName]);
        }
    }
}
