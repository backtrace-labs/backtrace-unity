using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;
namespace Tests
{
    public class BacktraceAttributeTests
    {
        [UnityTest]
        public IEnumerator TestAttributesGeneration_CreateCorrectAttributes_WithDiffrentReportConfiguration()
        {
            var report = new BacktraceReport("message");
            Assert.DoesNotThrow(() => new BacktraceAttributes(report, null));
            var exception = new FileNotFoundException();
            var exceptionReport = new BacktraceReport(exception, new Dictionary<string, object>() { { "attr", "attr" } });
            var attributes = new BacktraceAttributes(exceptionReport, null);
            Assert.IsTrue(attributes.Attributes.Count > 0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestCorrectDictionaryGeneration_CreateCorrectAttributesDictionary_WithDiffrentClientAttributes()
        {
            var exception = new FileNotFoundException();
            var reportAttributeKey = "report_attr";
            var reportAttributeValue = $"{reportAttributeKey}-value";
            var reportAttributes = new Dictionary<string, object>() { { reportAttributeKey, reportAttributeValue } };
            var exceptionReport = new BacktraceReport(exception, reportAttributes);

            string clientAttributeKey = "client_attr";
            string clientAttributeValue = $"{clientAttributeKey}-value";
            var clientAttributes = new Dictionary<string, object>() { { clientAttributeKey, clientAttributeValue } };

            var testObject = new BacktraceAttributes(exceptionReport, clientAttributes);
            Assert.IsTrue(testObject.Attributes.Keys.Any(n => n == clientAttributeKey));
            Assert.IsTrue(testObject.Attributes.Keys.Any(n => n == reportAttributeKey));
            Assert.IsTrue(testObject.Attributes[clientAttributeKey] as string == clientAttributeValue);
            Assert.IsTrue(testObject.Attributes[reportAttributeKey] as string == reportAttributeValue);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestCorrectDictionaryGeneration_ReplaceAttributes_TheSameDictionaryAttributes()
        {
            var exception = new FileNotFoundException();
            var reportAttributeKey = "report_attr";
            var reportAttributeValue = $"{reportAttributeKey}-value";
            var reportAttributes = new Dictionary<string, object>() { { reportAttributeKey, reportAttributeValue } };

            var clientAttributes = new Dictionary<string, object>() { { reportAttributeKey, $"{reportAttributeValue}-client" } };
            var exceptionReport = new BacktraceReport(exception, reportAttributes);
            Assert.IsFalse(clientAttributes[reportAttributeKey] as string == reportAttributeValue);
            yield return null;
        }
    }
}
