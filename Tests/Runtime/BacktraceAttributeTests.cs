using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceAttributeTests
    {
        [UnityTest]
        public IEnumerator TestAttributesGeneration_CreateCorrectAttributes_WithDiffrentReportConfiguration()
        {
            var report = new BacktraceReport("message");
            Assert.DoesNotThrow(() => new BacktraceAttributes(report, null));
            var exception = new FileNotFoundException();
            var exceptionReport = new BacktraceReport(exception, new Dictionary<string, string>() { { "attr", "attr" } });
            var attributes = new BacktraceAttributes(exceptionReport, null);
            Assert.IsTrue(attributes.Attributes.Count > 0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestCorrectDictionaryGeneration_CreateCorrectAttributesDictionary_WithDiffrentClientAttributes()
        {
            var exception = new FileNotFoundException();
            var reportAttributeKey = "report_attr";
            var reportAttributeValue = string.Format("{0}-value", reportAttributeKey);
            var reportAttributes = new Dictionary<string, string>() { { reportAttributeKey, reportAttributeValue } };
            var exceptionReport = new BacktraceReport(exception, reportAttributes);

            string clientAttributeKey = "client_attr";
            string clientAttributeValue = string.Format("{0}-value", clientAttributeKey);
            var clientAttributes = new Dictionary<string, string>() { { clientAttributeKey, clientAttributeValue } };

            var testObject = new BacktraceAttributes(exceptionReport, clientAttributes);
            Assert.IsTrue(testObject.Attributes.Keys.Any(n => n == clientAttributeKey));
            Assert.IsTrue(testObject.Attributes.Keys.Any(n => n == reportAttributeKey));
            Assert.IsTrue(testObject.Attributes[clientAttributeKey]== clientAttributeValue);
            Assert.IsTrue(testObject.Attributes[reportAttributeKey]== reportAttributeValue);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestCorrectDictionaryGeneration_ReplaceAttributes_TheSameDictionaryAttributes()
        {
            var reportAttributeKey = "report_attr";
            var reportAttributeValue = string.Format("{0}-value", reportAttributeKey);
            var clientAttributes = new Dictionary<string, string>() { { reportAttributeKey,
                string.Format("{0}-client", reportAttributeValue)
            } };
            Assert.IsFalse(clientAttributes[reportAttributeKey] == reportAttributeValue);
            yield return null;
        }
    }
}
