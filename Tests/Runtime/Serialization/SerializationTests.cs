using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class SerializationTests
    {
        [UnityTest]
        public IEnumerator TestDataSerialization_ValidReport_ShouldGenerateValidJsonReport()
        {
            var report = new BacktraceReport(new Exception("test"));
            var data = new BacktraceData(report, new Dictionary<string, string>(), 0);
            Assert.DoesNotThrow(() => data.ToJson());
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestDataSerialization_ValidStringReport_ShouldGenerateValidJsonReport()
        {
            var report = new BacktraceReport("string");
            var data = new BacktraceData(report, new Dictionary<string, string>(), 0);
            Assert.DoesNotThrow(() => data.ToJson());
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestDataSerialization_ReportWithCustomAttribtues_ShouldGenerateValidJsonReportWithAttributes()
        {
            var attributes = new Dictionary<string, string>()
            {
                ["foo"] = "foo",
                ["bar"] = "",
            };

            var report = new BacktraceReport(new Exception("test"));
            var data = new BacktraceData(report, attributes, 0);

            var json = data.ToJson();
            foreach (var keyValuePair in attributes)
            {
                var value = string.Format("\"{0}\":{1}", keyValuePair.Key, string.IsNullOrEmpty(keyValuePair.Value) ? "\"\"" : string.Format("\"{0}\"", keyValuePair.Value));
                Assert.IsTrue(json.Contains(value));
            }


            yield return null;
        }
    }
}
