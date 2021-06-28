using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Model.Metrics;
using Backtrace.Unity.Model.Metrics.Mocks;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime.Metrics
{
    public class UniqueEventsTests
    {
        // existing attribute name in Backtrace
        const string UniqueAttributeName = "scene.name";
        private AttributeProvider _attributeProvider;
        private const string _token = "aaaaabbbbbccccf82668682e69f59b38e0a853bed941e08e85f4bf5eb2c5458";
        private const string _universeName = "testing-universe-name";

        private const string _defaultSubmissionUrl = BacktraceMetrics.DefaultSubmissionUrl;
        private string _defaultUniqueEventsSubmissionUrl;
        private string _defaultSummedEventsSubmissionUrl;

        [SetUp]
        public void Setup()
        {
            _attributeProvider = new AttributeProvider();
            _defaultUniqueEventsSubmissionUrl = string.Format("{0}/unique-events/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);
            _defaultSummedEventsSubmissionUrl = string.Format("{0}/summed-events/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);
        }
        [TearDown]
        public void Cleanup()
        {
            _attributeProvider = new AttributeProvider();
        }
        [Test]
        public void BacktraceMetricsUniqueEvents_ShoulOverrideDefaultSubmissionUrl_SendEventToValidUrl()
        {
            var expectedSubmissionUrl = string.Format("{0}/unique-events/unit-test/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);
            var jsonString = string.Empty;
            var submissionUrl = string.Empty;
            var requestHandler = new BacktraceHttpClientMock()
            {
                OnInvoke = (string url, BacktraceJObject json) =>
                {
                    jsonString = json.ToJson();
                    submissionUrl = url;
                }
            };
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl)
            {
                UniqueEventsSubmissionUrl = expectedSubmissionUrl
            };
            backtraceMetrics.OverrideHttpClient(requestHandler);

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            backtraceMetrics.Send();

            Assert.IsNotEmpty(jsonString);
            Assert.AreEqual(expectedSubmissionUrl, submissionUrl);
            Assert.IsEmpty(backtraceMetrics.SummedEvents);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldBeAbleToOverrideDefaultSubmissionUrl_CorrectSubmissionUrl()
        {
            string expectedSubmissionUrl = string.Format("{0}/unit-test/unique-events/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);

            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, expectedSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            Assert.AreEqual(expectedSubmissionUrl, backtraceMetrics.UniqueEventsSubmissionUrl);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldAddCorrectlyUniqueEventWithEmptyAttributes_StoreValidUniqueEvent()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName, null);

            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceMetrics.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, _attributeProvider.GenerateAttributes().Count);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldAddCorrectlyUniqueEventWithoutAttributes_StoreValidUniqueEvent()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceMetrics.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, _attributeProvider.GenerateAttributes().Count);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldAddCorrectlyUniqueEventWithAttributes_StoreValidUniqueEvent()
        {
            const string expectedAttributeName = "foo";
            const string expectedAttributeValue = "bar";
            var attributes = new Dictionary<string, string>() { { expectedAttributeName, expectedAttributeValue } };
            int expectedNumberOfAttributes = _attributeProvider.GenerateAttributes().Count + attributes.Count;

            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName, attributes);

            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceMetrics.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, expectedNumberOfAttributes);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldAddCorrectlyUniqueEvent_StoreValidUniqueEvent()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            var added = backtraceMetrics.AddUniqueEvent(UniqueAttributeName);

            Assert.IsTrue(added);
            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 1);
            var uniqueEvent = backtraceMetrics.UniqueEvents.First.Value;
            Assert.AreEqual(uniqueEvent.Name, UniqueAttributeName);
            Assert.AreNotEqual(uniqueEvent.Timestamp, 0);
            Assert.AreEqual(uniqueEvent.Attributes.Count, _attributeProvider.GenerateAttributes().Count);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldPreventFromAddingEventIfThereIsNoAttribute_StoreValidUniqueEvent()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            var added = backtraceMetrics.AddUniqueEvent(string.Format("{0}-not-existing", UniqueAttributeName));

            Assert.IsFalse(added);
            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 0);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldAddEventIfAttributeIsDefinedInCustomAttributes_StoreValidUniqueEvents()
        {
            var expectedAttributeName = "foo";
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            var added = backtraceMetrics.AddUniqueEvent(expectedAttributeName, new Dictionary<string, string>() { { expectedAttributeName, expectedAttributeName } });

            Assert.IsTrue(added);
            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 1);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldntAddEmptyUniqueEvent_UniqueEventsAreEmpty()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            var added = backtraceMetrics.AddUniqueEvent(string.Empty);

            Assert.IsFalse(added);
            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 0);
        }


        [Test]
        public void BacktraceMetricsUniqueEvents_ShouldntAddNullableUniqueEvent_UniqueEventsAreEmpty()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddUniqueEvent(null);

            Assert.AreEqual(backtraceMetrics.UniqueEvents.Count, 0);
        }

        [Test]
        public void BacktraceMetricsUniqueEvents_UniqueEventAttributeValueDontChangeOverTime_UniqueEventAttributesStayTheSame()
        {
            const string initializationValue = "foo";
            const string updatedValue = "bar";
            _attributeProvider[UniqueAttributeName] = initializationValue;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            _attributeProvider[UniqueAttributeName] = updatedValue;

            var uniqueEvent = backtraceMetrics.UniqueEvents.First.Value;
            Assert.AreEqual(initializationValue, uniqueEvent.Attributes[UniqueAttributeName]);
        }


        [Test]
        public void BacktraceMetricsUniqueEvents_UniqueEventAttributeExistsAfterDeletingItFromAttributeProvider_UniqueEventAttributesStayTheSame()
        {
            const string initializationValue = "foo";
            _attributeProvider[UniqueAttributeName] = initializationValue;
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddUniqueEvent(UniqueAttributeName);
            _attributeProvider[UniqueAttributeName] = string.Empty;

            var uniqueEvent = backtraceMetrics.UniqueEvents.First.Value;
            Assert.AreEqual(initializationValue, uniqueEvent.Attributes[UniqueAttributeName]);
        }

        [Test]
        public void BacktraceMetricsUniqueEvent_ShouldUpdateTimeStamp_UniqueEventIsUpdated()
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
        public void BacktraceMetricsUniqueEvent_ShouldUpdateAttributes_UniqueEventIsUpdated()
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
        public void BacktraceMetricsUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeValueIsEmpty_UniqueEventIsUpdated()
        {
            const int nextTime = 1000;
            const string uniqueEventName = "BacktraceMetricsUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeValueIsEmpty_UniqueEventIsUpdated";
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
        public void BacktraceMetricsUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeDoesntExist_UniqueEventIsUpdated()
        {
            const int nextTime = 1000;
            const string uniqueEventName = "BacktraceMetricsUniqueEvent_ShouldPreventFromUpdatingAttributeWhenUniqueAttributeDoesntExist_UniqueEventIsUpdated";
            var timestamp = DateTimeHelper.Timestamp();
            var expectedNewTimestamp = timestamp + nextTime;

            var uniqueEvent = new UniqueEvent(uniqueEventName, timestamp, new Dictionary<string, string> { { uniqueEventName, uniqueEventName } });

            uniqueEvent.UpdateTimestamp(expectedNewTimestamp, null);

            Assert.AreEqual(uniqueEventName, uniqueEvent.Attributes[uniqueEventName]);
        }
    }
}
