using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Model.Metrics.Mocks;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Metrics
{
    public class SummedEventTests
    {
        private const string MetricsEventName = "scene-changed";
        private const string _token = "aaaaabbbbbccccf82668682e69f59b38e0a853bed941e08e85f4bf5eb2c5458";
        private const string _universeName = "testing-universe-name";

        private const string _defaultSubmissionUrl = BacktraceMetrics.DefaultSubmissionUrl;
        private string _defaultUniqueEventsSubmissionUrl;
        private string _defaultSummedEventsSubmissionUrl;

        private AttributeProvider _attributeProvider;
        [OneTimeSetUp]
        public void Setup()
        {
            _attributeProvider = new AttributeProvider();
            _defaultUniqueEventsSubmissionUrl = string.Format("{0}/unique-events/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);
            _defaultSummedEventsSubmissionUrl = string.Format("{0}/summed-events/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);
            Debug.unityLogger.logEnabled = false;
        }
        [Test]
        public void BacktraceMetricsSummedEvents_ShoulOverrideDefaultSubmissionUrl_SendEventToValidUrl()
        {
            var expectedSubmissionUrl = string.Format("{0}/summed-events/unit-test/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);
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
                SummedEventsSubmissionUrl = expectedSubmissionUrl
            };
            backtraceMetrics.OverrideHttpClient(requestHandler);

            backtraceMetrics.AddSummedEvent(MetricsEventName);
            backtraceMetrics.Send();

            Assert.IsNotEmpty(jsonString);
            Assert.AreEqual(expectedSubmissionUrl, submissionUrl);
            Assert.IsEmpty(backtraceMetrics.SummedEvents);
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldBeAbleToOverrideDefaultSubmissionUrl_CorrectSubmissionUrl()
        {
            string expectedSubmissionUrl = string.Format("{0}/unit-test/summed-events/submit?token={1}&universe={2}", _defaultSubmissionUrl, _token, _universeName);

            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, expectedSubmissionUrl);

            Assert.AreEqual(expectedSubmissionUrl, backtraceMetrics.SummedEventsSubmissionUrl);
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldAddCorrectlySummedEvent_StoreValidSummedEvent()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddSummedEvent(MetricsEventName);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 1);
            var summedEvent = backtraceMetrics.SummedEvents.First.Value;
            Assert.AreEqual(summedEvent.Name, MetricsEventName);
            Assert.AreNotEqual(summedEvent.Timestamp, 0);
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldAddCorrectlySummedEventWithAttributes_StoreValidSummedEvent()
        {
            const string metricsEventName = "scene-changed";
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);
            const string expectedAttributeName = "foo";
            const string expectedAttributeValue = "bar";
            var metricsAttributes = new Dictionary<string, string>() { { expectedAttributeName, expectedAttributeValue } };
            backtraceMetrics.AddSummedEvent(metricsEventName, metricsAttributes);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 1);
            var summedEvent = backtraceMetrics.SummedEvents.First.Value;
            Assert.AreEqual(summedEvent.Name, metricsEventName);
            Assert.AreNotEqual(summedEvent.Timestamp, 0);
            Assert.IsTrue(summedEvent.Attributes.ContainsKey(expectedAttributeName));
            Assert.AreEqual(expectedAttributeValue, summedEvent.Attributes[expectedAttributeName]);
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldntAddEmptySummedEvent_SummedEventsAreEmpty()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddSummedEvent(string.Empty);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 0);
        }


        [Test]
        public void BacktraceMetricsSummedEvents_ShouldntAddNullableSUmmedEvent_SummedEventsAreEmpty()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, 0, _defaultUniqueEventsSubmissionUrl, _defaultSummedEventsSubmissionUrl);

            backtraceMetrics.AddSummedEvent(null);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 0);
        }
    }
}
