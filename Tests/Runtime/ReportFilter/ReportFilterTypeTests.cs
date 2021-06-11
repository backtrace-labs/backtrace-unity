using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime.ReportFilter
{
    public class ReportFilterTypeTests : BacktraceBaseTest
    {
        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            var configuration = GenerateDefaultConfiguration();
            BacktraceClient.Configuration = configuration;
            AfterSetup(false);
        }

        [UnityTest]
        public IEnumerator TestErrorTypeFilter_ShouldFilterErrorLog_ShouldPreventFromSendingDataToBacktrace()
        {
            const string errorMessage = "errorMessage";
            var eventCalled = false;
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                eventCalled = true;
                return false;
            };
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Error;

            BacktraceClient.HandleUnityMessage(errorMessage, string.Empty, LogType.Error);
            yield return new WaitForEndOfFrame();

            Assert.IsFalse(eventCalled);
        }

        [Test]
        public void TestErrorTypeFilter_ShouldntFilterErrorLogWhenFilterDoesntIncludeIt_ShouldInvokeSkipCallback()
        {
            const string errorMessage = "errorMessage";
            var eventCalled = false;
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                eventCalled = true;
                return false;
            };
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.UnhandledException;

            BacktraceClient.HandleUnityMessage(errorMessage, string.Empty, LogType.Error);

            Assert.IsTrue(eventCalled);
        }


        [Test]
        public void TestErrorTypeFilterShouldSetCorrectReportFilterType_ReportFilterTypeHasCorrectValue()
        {
            const string errorMessage = "errorMessage";
            var reportFilterType = ReportFilterType.None;
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                reportFilterType = type;
                return false;
            };
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.UnhandledException;

            BacktraceClient.HandleUnityMessage(errorMessage, string.Empty, LogType.Error);
            
            Assert.AreEqual(ReportFilterType.Error, reportFilterType);
        }

        [UnityTest]
        public IEnumerator TestReportFilter_ShouldPreventFromSendingMessage_ClientNotSendingData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                eventCalled = true;
                return false;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Message;
            var message = "report message";

            BacktraceClient.Send(message);
            yield return new WaitForEndOfFrame();
            Assert.IsFalse(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilterWithMultipleOptions_ShouldPreventFromSendingMessage_ClientNotSendingData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                eventCalled = true;
                return false;
            };

            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.UnhandledException | ReportFilterType.Message;
            var message = "report message";

            BacktraceClient.Send(message);
            yield return new WaitForEndOfFrame();
            Assert.IsFalse(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilter_ShouldPreventFromSendingException_ClientNotSendingData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string message) =>
            {
                eventCalled = true;
                return false;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Exception;
            var exception = new Exception("something really bad");

            BacktraceClient.Send(exception);
            yield return new WaitForEndOfFrame();
            Assert.IsFalse(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilter_ShouldPreventFromSendingUnhandledException_ClientNotSendingData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string message) =>
            {
                eventCalled = true;
                return false;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.UnhandledException;
            var exception = new BacktraceUnhandledException(string.Empty, string.Empty);

            BacktraceClient.Send(exception);
            yield return new WaitForEndOfFrame();
            Assert.IsFalse(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilter_ShouldPreventFromSendingHang_ClientNotSendingData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string message) =>
            {
                eventCalled = true;
                return false;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Hang;
            var exception = new BacktraceUnhandledException("ANRException: Blocked thread detected", string.Empty);

            BacktraceClient.Send(exception);
            yield return new WaitForEndOfFrame();
            Assert.IsFalse(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilter_ShouldPreventFromSendingDifferentTypeOfExceptions_ClientNotSendingData()
        {
            var reportFilterCalled = false;
            var beforeSendCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                beforeSendCalled = true;
                return null;
            };
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string message) =>
            {
                reportFilterCalled = true;
                return false;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Exception & ReportFilterType.UnhandledException;
            var exception = new BacktraceUnhandledException("ANRException: Blocked thread detected", string.Empty);
            BacktraceClient.Send(exception);
            BacktraceClient.Send(new Exception("foo bar"));

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(reportFilterCalled);
            Assert.IsTrue(beforeSendCalled);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestReportFilter_ShouldntPreventFromSendingException_ClientAllowToSendData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Message;
            BacktraceClient.Send(new Exception("foo bar"));

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(eventCalled);

            yield return null;
        }


        [UnityTest]
        public IEnumerator TestReportFilter_ShouldntPreventFromSendingUnhandledException_ClientAllowToSendData()
        {
            var eventCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Exception;
            BacktraceClient.Send(new BacktraceUnhandledException(string.Empty, string.Empty));

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilterDelegate_ShouldPreventFromSendingUnhandledException_ClientNotSendingData()
        {
            var eventCalled = false;
            var reportFilterCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };

            BacktraceClient.SkipReport = (ReportFilterType type, Exception exception, string message) =>
            {
                reportFilterCalled = true;
                return type == ReportFilterType.UnhandledException;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Exception;
            BacktraceClient.Send(new BacktraceUnhandledException(string.Empty, string.Empty));

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(reportFilterCalled);
            Assert.IsFalse(eventCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportFilterDelegate_ShouldntPreventFromSendingUnhandledException_ClientNotSendingData()
        {
            var eventCalled = false;
            var reportFilterCalled = false;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                eventCalled = true;
                return null;
            };

            BacktraceClient.SkipReport = (ReportFilterType type, Exception exception, string message) =>
            {
                reportFilterCalled = true;
                return type != ReportFilterType.UnhandledException;
            };

            // in this situation to learn if we were able to continue processing report 
            // we should check if before send event or reportFilter event was called
            BacktraceClient.Configuration.ReportFilterType = ReportFilterType.Exception;
            BacktraceClient.Send(new BacktraceUnhandledException(string.Empty, string.Empty));

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(reportFilterCalled);
            Assert.IsTrue(eventCalled);

            yield return null;
        }
    }
}
