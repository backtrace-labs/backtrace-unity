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
            

            Assert.IsFalse(eventCalled);
            yield return null;
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


            Assert.IsTrue(reportFilterCalled);
            Assert.IsTrue(eventCalled);

            yield return null;
        }

        [Test]
        public void TestUnityLogHandlerException_ShouldUseUnhandledExceptionFilterType()
        {
            var reportFilterType = ReportFilterType.None;
            BacktraceClient.Configuration.UnityLogHandlerExceptionCapture =
                BacktraceUnityLogHandlerExceptionCaptureMode.Enabled;
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                reportFilterType = type;
                return true;
            };
            var exception = new InvalidOperationException("Validation exception");
            Assert.IsTrue(
                BacktraceClient.RecordUnityLogHandlerException(exception, null));
            BacktraceClient.HandleUnityMessage(
                "InvalidOperationException: Validation exception",
                "ExampleClass.HandleException() (at Assets/ExampleClass.cs:42)",
                LogType.Exception);
            Assert.AreEqual(ReportFilterType.UnhandledException, reportFilterType);
        }

        [Test]
        public void TestUnityCallbackExceptionWithoutLogHandler_ShouldUseUnhandledExceptionFilterType()
        {
            var reportFilterType = ReportFilterType.None;
            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
            {
                reportFilterType = type;
                return true;
            };
            BacktraceClient.HandleUnityMessage(
                "NullReferenceException: Object reference not set to an instance of an object.",
                "ExampleClass.Update() (at Assets/ExampleClass.cs:42)",
                LogType.Exception);
            Assert.AreEqual(ReportFilterType.UnhandledException, reportFilterType);
        }

        [UnityTest]
        public IEnumerator TestExplicitSendException_ShouldRemainHandledException()
        {
            BacktraceData data = null;
            BacktraceClient.BeforeSend = (BacktraceData reportData) =>
            {
                data = reportData;
                return null;
            };
            BacktraceClient.Send(new InvalidOperationException("Handled exception"));
            yield return WaitForFrame.Wait();
            Assert.IsNotNull(data);
            Assert.AreEqual("Exception", data.Attributes.Attributes["error.type"]);
            Assert.IsFalse(data.Attributes.Attributes.ContainsKey("backtrace.unity.capture_path"));
        }
    }
}
