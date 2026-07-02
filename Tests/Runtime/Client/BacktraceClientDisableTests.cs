using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime.Client
{
    class BacktraceClientDisableTests : BacktraceBaseTest
    {

        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            AfterSetup(false);
        }

        // Skip in a Player build: DisableInEditor only disables the client in the Editor
        private static void SkipWhenNotInEditor()
        {
            if (!Application.isEditor)
            {
                Assert.Ignore("DisableInEditor only applies in the Editor.");
            }
        }

        [Test]
        public void TestEditorDisabling_ShouldntSendData_ShouldntSendExceptionViaSendAPI()
        {
            SkipWhenNotInEditor();
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Configuration.DisableInEditor = true;
            BacktraceClient.Refresh();
            bool invoked = false;
            BacktraceClient.SkipReport = (ReportFilterType filterType, Exception exception, string message) =>
            {
                invoked = true;
                // return false to make sure we won't filter a report.
                return false;
            };

            BacktraceClient.Send(new Exception("Test"));

            Assert.IsFalse(invoked);
            Assert.IsFalse(BacktraceClient.Enabled);
        }

        [Test]
        public void TestEditorDisabling_ShouldntSendData_ShouldntSendReportViaSendAPI()
        {
            SkipWhenNotInEditor();
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Configuration.DisableInEditor = true;
            BacktraceClient.Refresh();
            bool invoked = false;
            BacktraceClient.SkipReport = (ReportFilterType filterType, Exception exception, string message) =>
            {
                invoked = true;
                // return false to make sure we won't filter a report.
                return false;
            };

            BacktraceClient.Send(new BacktraceReport(new Exception("Test")));

            Assert.IsFalse(invoked);
            Assert.IsFalse(BacktraceClient.Enabled);
        }

        [Test]
        public void TestEditorDisabling_ShouldntSendData_ShouldntSendMessageViaSendAPI()
        {
            SkipWhenNotInEditor();
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Configuration.DisableInEditor = true;
            BacktraceClient.Refresh();
            bool invoked = false;
            BacktraceClient.SkipReport = (ReportFilterType filterType, Exception exception, string message) =>
            {
                invoked = true;
                // return false to make sure we won't filter a report.
                return false;
            };

            BacktraceClient.Send("test");

            Assert.IsFalse(invoked);
            Assert.IsFalse(BacktraceClient.Enabled);
        }

        [Test]
        public void TestEditorDisabling_ShouldntSendData_ShouldntSendUnhandledException()
        {
            SkipWhenNotInEditor();
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Configuration.DisableInEditor = true;
            BacktraceClient.Refresh();
            bool invoked = false;
            BacktraceClient.SkipReport = (ReportFilterType filterType, Exception exception, string message) =>
            {
                invoked = true;
                // return false to make sure we won't filter a report.
                return false;
            };

            BacktraceClient.HandleUnityBackgroundException("something bad happened", string.Empty, LogType.Exception);

            Assert.IsFalse(invoked);
            Assert.IsFalse(BacktraceClient.Enabled);
        }

        [UnityTest]
        public IEnumerator TestEditorDisabling_ShouldSendData_ShouldSendMessageViaSendAPI()
        {
            var clientConfiguration = GetValidClientConfiguration();
            BacktraceClient.Configuration = clientConfiguration;
            BacktraceClient.Configuration.DisableInEditor = false;
            BacktraceClient.Refresh();
            bool invoked = false;
            BacktraceClient.SkipReport = (ReportFilterType filterType, Exception exception, string message) =>
            {
                invoked = true;
                return false;
            };
            bool beforeSendInvoked = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                beforeSendInvoked = true;
                // skip sending report
                return null;
            };

            BacktraceClient.Send("test");

            Assert.IsTrue(invoked);
            Assert.IsTrue(beforeSendInvoked);
            Assert.IsTrue(BacktraceClient.Enabled);
            yield return null;
        }
    }
}
