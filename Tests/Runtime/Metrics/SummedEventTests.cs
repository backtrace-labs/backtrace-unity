using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Metrics
{
    public class SummedEventTests
    {
        private readonly string _submissionUrl = "https://event-edge.backtrace.io/api/user-aggregation/events?token=TOKEN";
        private AttributeProvider _attributeProvider = new AttributeProvider();
        [OneTimeSetUp]
        public void Setup()
        {
            Debug.unityLogger.logEnabled = false;
        }

        [Test]
        public void BacktraceMetricsSummedEvents_ShouldAddCorrectlySummedEvent_StoreValidSummedEvent()
        {
            const string metricsEventName = "scene-changed";
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);

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
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);
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
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);

            backtraceMetrics.AddSummedEvent(string.Empty);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 0);
        }


        [Test]
        public void BacktraceMetricsSummedEvents_ShouldntAddNullableSUmmedEvent_SummedEventsAreEmpty()
        {
            var backtraceMetrics = new BacktraceMetrics(_attributeProvider, _submissionUrl, 0);

            backtraceMetrics.AddSummedEvent(null);

            Assert.AreEqual(backtraceMetrics.SummedEvents.Count, 0);
        }
    }
}
