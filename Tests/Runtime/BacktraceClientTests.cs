using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceClientTests : BacktraceBaseTest
    {
        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            AfterSetup(false);
        }


        [UnityTest]
        public IEnumerator TestClientCreation_ValidBacktraceConfiguration_ValidClientCreation()
        {
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Refresh();
            Assert.IsTrue(BacktraceClient.Enabled);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestClientCreation_EmptyConfiguration_DisabledClientCreation()
        {
            Assert.IsFalse(BacktraceClient.Enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientEvents_EmptyConfiguration_ShouldntThrowExceptionForDisabledClient()
        {
            Assert.IsFalse(BacktraceClient.Enabled);
            Assert.IsNull(BacktraceClient.OnServerError);
            Assert.IsNull(BacktraceClient.OnServerResponse);
            Assert.IsNull(BacktraceClient.BeforeSend);
            Assert.IsNull(BacktraceClient.RequestHandler);
            Assert.IsNull(BacktraceClient.OnUnhandledApplicationException);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestUnvailableEvents_EmptyConfiguration_ShouldntThrowException()
        {
            BacktraceClient.Configuration = null;
            BacktraceClient.Refresh();
            BacktraceClient.OnServerError = (Exception e) => { };
            BacktraceClient.OnServerResponse = (BacktraceResult r) => { };
            BacktraceClient.BeforeSend = (BacktraceData d) => d;
            BacktraceClient.OnUnhandledApplicationException = (Exception e) => { };

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSendEvent_DisabledApi_NotSendingEvent()
        {
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Refresh();
            Assert.DoesNotThrow(() => BacktraceClient.Send(new Exception("test exception")));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBeforeSendEvent_UpdateReportAttributesForUnhandledException_ShouldUpdateReportAttributes()
        {
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Refresh();
            var attributeName = "foo";
            var attributeValue = "bar";
            BacktraceClient.BeforeSend = (BacktraceData d) =>
             {
                 d.Attributes.Attributes[attributeName] = attributeValue;
                 return d;
             };
            BacktraceClient.RequestHandler = (string url, BacktraceData d) =>
             {
                 Assert.AreEqual(d.Attributes.Attributes[attributeName], attributeValue);
                 return new BacktraceResult
                 {
                     Status = BacktraceResultStatus.Ok
                 };
             };

            var unhandledException = new BacktraceUnhandledException("foo", string.Empty);
            BacktraceClient.Send(unhandledException);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBeforeSendEvent_ValidConfiguration_EventTrigger()
        {
            var trigger = false;
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Refresh();
            BacktraceClient.BeforeSend = (BacktraceData backtraceData) =>
            {
                trigger = true;
                return backtraceData;
            };
            BacktraceClient.Send(new Exception("test exception"));
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSendingReport_ValidConfiguration_ValidSend()
        {
            var trigger = false;
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Refresh();

            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
            {
                Assert.IsNotNull(data);
                Assert.IsFalse(string.IsNullOrEmpty(data.ToJson()));
                trigger = true;
                return new BacktraceResult();
            };
            BacktraceClient.Send(new Exception("test exception"));
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestFingerprintBehaviorForNormalizedExceptionMessage_ShouldGenerateFingerprintForExceptionReportWithoutStackTrace_ShouldIncludeFingerprintInBacktraceReport()
        {
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Configuration.UseNormalizedExceptionMessage = true;
            BacktraceClient.Refresh();

            // exception without stack trace might happened when exception occured because of
            // invalid game object setting or via weird crash
            // exception below has empty exception stack trace
            var exception = new BacktraceUnhandledException("00:00:00 00/00/00 Unhandled exception", string.Empty);
            var expectedNormalizedMessage = "Unhandledexception";
            var report = new BacktraceReport(exception);

            bool eventFired = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                Assert.IsNotNull(data.Attributes.Attributes["_mod_fingerprint"]);
                Assert.AreEqual(expectedNormalizedMessage.GetSha(), data.Attributes.Attributes["_mod_fingerprint"]);
                eventFired = true;
                // prevent backtrace data from sending to Backtrace.
                return null;
            };

            yield return BacktraceClient.StartCoroutine(CallBacktraceClientAndWait(report));


            Assert.IsTrue(eventFired);
        }


        [UnityTest]
        public IEnumerator TestFingerprintBehaviorForNormalizedExceptionMessage_ShouldntGenerateFingerprintForDisabledOption_FingerprintDoesntExist()
        {
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Configuration.UseNormalizedExceptionMessage = false;
            BacktraceClient.Refresh();

            // exception without stack trace might happened when exception occured because of
            // invalid game object setting or via weird crash
            // exception below has empty exception stack trace
            var exception = new BacktraceUnhandledException("00:00:00 00/00/00 Unhandled exception", string.Empty);
            var report = new BacktraceReport(exception);

            bool eventFired = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                Assert.IsFalse(data.Attributes.Attributes.ContainsKey("_mod_fingerprint"));
                eventFired = true;
                // prevent backtrace data from sending to Backtrace.
                return null;
            };

            yield return BacktraceClient.StartCoroutine(CallBacktraceClientAndWait(report));


            Assert.IsTrue(eventFired);
        }

        [UnityTest]
        public IEnumerator TestFingerprintBehaviorForNormalizedExceptionMessage_ShouldUseReportFingerprint_ReportFingerprintInAttributes()
        {
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Configuration.UseNormalizedExceptionMessage = true;
            BacktraceClient.Refresh();

            // exception without stack trace might happened when exception occured because of
            // invalid game object setting or via weird crash
            // exception below has empty exception stack trace
            var exception = new BacktraceUnhandledException("00:00:00 00/00/00 Unhandled exception", string.Empty);
            var report = new BacktraceReport(exception);
            const string expectedFingerprint = "foo-bar";
            report.Fingerprint = expectedFingerprint;

            bool eventFired = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventFired = true;
                Assert.IsNotNull(data.Attributes.Attributes["_mod_fingerprint"]);
                Assert.AreEqual(expectedFingerprint, data.Attributes.Attributes["_mod_fingerprint"]);
                // prevent backtrace data from sending to Backtrace.
                return null;
            };
            BacktraceClient.Send(report);
            yield return new WaitForEndOfFrame();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void TestFingerprintBehaviorForNormalizedMesssage_ShouldGenerateFingerprintForMessageReportWithoutStackTrace_ShouldIncludeFingerprintInBacktraceReport()
        {
            string message = "Backtrace report fake message";
            var report = new BacktraceReport(message)
            {
                DiagnosticStack = new System.Collections.Generic.List<BacktraceStackFrame>()
            };
            report.SetReportFingerprint(true);

            Assert.AreEqual(message.OnlyLetters().GetSha(), report.Attributes["_mod_fingerprint"]);
        }


        [UnityTest]
        public IEnumerator TestFingerprintBehaviorForNormalizedExceptionMessage_ShouldGenerateFingerprintAndShouldntRemoveAnyLetter_ShouldIncludeFingerprintInBacktraceReport()
        {
            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Configuration.UseNormalizedExceptionMessage = true;
            BacktraceClient.Refresh();


            var normalizedMessage = "Unhandledexception";
            var exception = new BacktraceUnhandledException(normalizedMessage, string.Empty);
            var report = new BacktraceReport(exception);

            bool eventFired = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                Assert.IsNotNull(data.Attributes.Attributes["_mod_fingerprint"]);
                Assert.AreEqual(normalizedMessage.GetSha(), data.Attributes.Attributes["_mod_fingerprint"]);
                // prevent backtrace data from sending to Backtrace.
                eventFired = true;
                // prevent backtrace data from sending to Backtrace.
                return null;
            };

            yield return BacktraceClient.StartCoroutine(CallBacktraceClientAndWait(report));
            Assert.IsTrue(eventFired);
        }

        [UnityTest]
        public IEnumerator TestFingerprintBehaviorForNormalizedExceptionMessage_ShouldntGenerateFingerprintForExistingStackTrace_ShouldIgnoreAttributeFingerprint()
        {

            BacktraceClient.Configuration = GetValidClientConfiguration();
            BacktraceClient.Configuration.UseNormalizedExceptionMessage = true;
            BacktraceClient.Refresh();

            var exception = new BacktraceUnhandledException("Unhandled exception", "foo()");
            var report = new BacktraceReport(exception);

            bool eventFired = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventFired = true;
                Assert.IsFalse(report.Attributes.ContainsKey("_mod_fingerprint"));
                // prevent backtrace data from sending to Backtrace.
                return null;
            };
            BacktraceClient.Send(report);
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(eventFired);
        }


        private IEnumerator CallBacktraceClientAndWait(BacktraceReport report)
        {
            BacktraceClient.Send(report);
            yield return new WaitForEndOfFrame();
        }
    }
}
