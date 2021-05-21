using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Metrics
{
    public class SummedEventTests
    {
        //private readonly string _submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events?token=TOKEN";
        private readonly string _token = "aaaaabbbbbccccf82668682e69f59b38e0a853bed941e08e85f4bf5eb2c5458";
        private readonly string _universeName = "testing-universe-name";
        private AttributeProvider _attributeProvider = new AttributeProvider();
        [OneTimeSetUp]
        public void Setup()
        {
            Debug.unityLogger.logEnabled = false;
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldBeAbleToOverrideDefaultSubmissionUrl_CorrectSubmissionUrl()
        {
            const string submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events";

            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, submissionUrl, 0, _token, _universeName);

            Assert.AreEqual(submissionUrl, backtraceMetrics.SubmissionUrl);
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldAddCorrectlySummedEvent_StoreValidSummedEvent()
        {
            const string metricsEventName = "scene-changed";
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, BacktraceMetrics.DefaultSubmissionUrl, 0, _token, _universeName);

            backtraceMetrics.AddSummedEvent(metricsEventName);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 1);
            var summedEvent = backtraceMetrics.SummedEvents.First.Value;
            Assert.AreEqual(summedEvent.Name, metricsEventName);
            Assert.AreNotEqual(summedEvent.Timestamp, 0);
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldAddCorrectlySummedEventWithAttributes_StoreValidSummedEvent()
        {
            const string metricsEventName = "scene-changed";
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, BacktraceMetrics.DefaultSubmissionUrl, 0, _token, _universeName);
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
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, BacktraceMetrics.DefaultSubmissionUrl, 0, _token, _universeName);

            backtraceMetrics.AddSummedEvent(string.Empty);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 0);
        }


        [Test]
        public void BacktraceMetricsSummedEvents_ShouldntAddNullableSUmmedEvent_SummedEventsAreEmpty()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, BacktraceMetrics.DefaultSubmissionUrl, 0, _token, _universeName);

            backtraceMetrics.AddSummedEvent(null);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 0);
        }
    }
}
