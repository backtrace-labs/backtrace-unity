using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceAttributeTests : BacktraceBaseTest
    {
        private const int CLIENT_RATE_LIMIT = 3;

        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            BacktraceClient.Configuration = GetBasicConfiguration();
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            AfterSetup();
        }

        [UnityTest]
        public IEnumerator TesClientAttributeAccessor_BacktraceDataShouldIncludeClientAttributes_ClientAttributesAreAvailableInDiagnosticData()
        {
            var key = "foo";
            var value = "bar";
            BacktraceClient[key] = value;
            BacktraceData data = null;
            BacktraceClient.BeforeSend = (BacktraceData reportData) =>
             {
                 data = reportData;
                 return null;
             };
            BacktraceClient.Send(new Exception("foo"));
            yield return new WaitForEndOfFrame();
            Assert.IsNotNull(data);
            Assert.AreEqual(data.Attributes.Attributes[key], value);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TesClientAttributes_ReportShouldntExtendClientAttributes_ClientAttributesWontStoreReportAttributes()
        {
            var key = "foo";
            var value = "bar";
            BacktraceClient[key] = value;
            var numberOfKeysBeforeSendRequest = BacktraceClient.GetAttributesCount();
            BacktraceData data = null;
            BacktraceClient.BeforeSend = (BacktraceData reportData) =>
            {
                data = reportData;
                return null;
            };
            var exceptionsMessage = new string[] { "foo", "bar" };
            foreach (var exceptionMessage in exceptionsMessage)
            {
                BacktraceClient.Send(new Exception(exceptionMessage));
                yield return new WaitForEndOfFrame();
            }

            Assert.AreEqual(data.Attributes.Attributes[key], value);
            Assert.AreEqual(numberOfKeysBeforeSendRequest, BacktraceClient.GetAttributesCount());
            yield return null;
        }


        [UnityTest]
        public IEnumerator TesClientAttributesMethod_BacktraceDataShouldIncludeClientAttributes_ClientAttributesAreAvailableInDiagnosticData()
        {
            var key = "foo2";
            var value = "bar2";
            var attributes = new Dictionary<string, string>();
            attributes[key] = value;
            BacktraceClient.SetAttributes(attributes);

            BacktraceData data = null;
            BacktraceClient.BeforeSend = (BacktraceData reportData) =>
            {
                data = reportData;
                return null;
            };
            BacktraceClient.Send(new Exception("foo"));
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(data);
            Assert.AreEqual(data.Attributes.Attributes[key], value);
            yield return null;
        }


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

        [Test]
        public void TestCorrectDictionaryGeneration_CreateCorrectAttributesDictionary_WithDiffrentClientAttributes()
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
            Assert.IsTrue(testObject.Attributes[clientAttributeKey] == clientAttributeValue);
            Assert.IsTrue(testObject.Attributes[reportAttributeKey] == reportAttributeValue);
        }

        [Test]
        public void TestExceptionTypeAttribute_ShouldSetExceptionTypeMessage_ExceptionTypeAttributeIsCorrect()
        {
            var report = new BacktraceReport("foo");
            var testAttributes = new BacktraceAttributes(report, null);

            Assert.AreEqual("Message", testAttributes.Attributes["error.type"]);
        }

        [Test]
        public void TestExceptionTypeAttribute_ShouldSetExceptionTypeException_ExceptionTypeAttributeIsCorrect()
        {
            var report = new BacktraceReport(new Exception("foo"));
            var testAttributes = new BacktraceAttributes(report, null);

            Assert.AreEqual("Exception", testAttributes.Attributes["error.type"]);
        }

        [Test]
        public void TestExceptionTypeAttribute_ShouldSetExceptionTypeUnhandledException_ExceptionTypeAttributeIsCorrect()
        {
            var report = new BacktraceReport(new BacktraceUnhandledException("foo", string.Empty));
            var testAttributes = new BacktraceAttributes(report, null);

            Assert.AreEqual("Unhandled exception", testAttributes.Attributes["error.type"]);
        }

        [Test]
        public void TestExceptionTypeAttribute_ShouldSetExceptionTypeHang_ExceptionTypeAttributeIsCorrect()
        {
            var report = new BacktraceReport(new BacktraceUnhandledException("ANRException: Blocked thread detected", string.Empty));
            var testAttributes = new BacktraceAttributes(report, null);

            Assert.AreEqual("Hang", testAttributes.Attributes["error.type"]);
        }
    }
}
