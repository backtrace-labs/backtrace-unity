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

        [Test]
        public void TestEditorDisabling_ShouldntSendData_ShouldntSendExceptionViaSendAPI()
        {
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
