using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;

namespace Tests
{
    public class BacktraceReportTests
    {
        private readonly Exception exception = new DivideByZeroException("fake exception message");
        private readonly Dictionary<string, object> reportAttributes = new Dictionary<string, object>()
            {
                { "test_attribute", "test_attribute_value" },
                { "temporary_attribute", 123 },
                { "temporary_attribute_bool", true}
            };
        private readonly List<string> attachemnts = new List<string>() { "path", "path2" };

        [UnityTest]
        public IEnumerator TestReportCreation_CreateCorrectReport_WithDiffrentConstructors()
        {
            Assert.DoesNotThrow(() => new BacktraceReport("message"));
            Assert.DoesNotThrow(() => new BacktraceReport("message", new Dictionary<string, object>(), new List<string>()));
            Assert.DoesNotThrow(() => new BacktraceReport("message", attachmentPaths: attachemnts));

            var exception = new FileNotFoundException();
            Assert.DoesNotThrow(() => new BacktraceReport(exception));
            Assert.DoesNotThrow(() => new BacktraceReport(exception, new Dictionary<string, object>(), new List<string>()));
            Assert.DoesNotThrow(() => new BacktraceReport(exception, attachmentPaths: attachemnts));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportSerialization_SerializeValidReport_ExceptionReport()
        {
            var report = new BacktraceReport(
                exception: exception,
                attributes: reportAttributes,
                attachmentPaths: attachemnts);

            return TestSerialization(report);
        }

        [UnityTest]
        public IEnumerator TestReportSerialization_SerializeValidReport_MessageReport()
        {
            string message = "message";
            var report = new BacktraceReport(
                message: message,
                attributes: reportAttributes,
                attachmentPaths: attachemnts);

            return TestSerialization(report);
        }


        [UnityTest]
        public IEnumerator TestReportValues_ShouldAssignCorrectExceptionInformation_ExceptionReport()
        {
            var report = new BacktraceReport(
               exception: exception,
               attributes: reportAttributes,
               attachmentPaths: attachemnts);

            Assert.AreEqual(exception.Message, report.Message);
            Assert.AreEqual(exception.GetType().Name, report.Classifier);
            Assert.AreEqual(attachemnts.Count(), report.AttachmentPaths.Count());
            Assert.AreEqual(reportAttributes["test_attribute"], report.Attributes["test_attribute"]);
            Assert.AreEqual(reportAttributes["temporary_attribute"], report.Attributes["temporary_attribute"]);
            Assert.AreEqual(reportAttributes["temporary_attribute_bool"], report.Attributes["temporary_attribute_bool"]);
            
            yield return null;
        }

        private IEnumerator TestSerialization(BacktraceReport report)
        {
            var json = report.ToJson();
            var deserializedReport = BacktraceReport.Deserialize(json);
            foreach (var attribute in deserializedReport.Attributes)
            {
                // ignore validating types - tests already validating it.
                // here we want to be sure that we correctly assigned json data
                var attributeValue = reportAttributes[attribute.Key].ToString();
                Assert.AreEqual(attributeValue, attribute.Value);
            }
            Assert.IsTrue(attachemnts.SequenceEqual(deserializedReport.AttachmentPaths));
            Assert.AreEqual(report.Classifier, deserializedReport.Classifier);
            Assert.AreEqual(report.DiagnosticStack.Count, deserializedReport.DiagnosticStack.Count);
            Assert.AreEqual(report.Message, deserializedReport.Message);
            yield return null;
        }
    }
}
