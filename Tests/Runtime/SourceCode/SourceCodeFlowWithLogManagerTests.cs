using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Linq;

namespace Backtrace.Unity.Tests.Runtime
{
    public class SourceCodeFlowWithLogManagerTests : BacktraceBaseTest
    {
        private readonly BacktraceApiMock api = new BacktraceApiMock();
        private readonly int _numberOfLogs = 10;

        [OneTimeSetUp]
        public void Setup()
        {
            BeforeSetup();

            var configuration = GetValidClientConfiguration();
            configuration.NumberOfLogs = (uint)_numberOfLogs;
            BacktraceClient.Configuration = configuration;
            AfterSetup(true);
            BacktraceClient.BacktraceApi = api;
        }

        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerAndSendExceptionReport_SourceCodeAvailable()
        {
            BacktraceClient.Send(new Exception("foo"));

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }


        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerAndSendMessageReport_SourceCodeAvailable()
        {
            BacktraceClient.Send("foo");

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }


        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledException_SourceCodeAvailable()
        {
            BacktraceClient.HandleUnityMessage("foo", string.Empty, UnityEngine.LogType.Exception);

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledError_SourceCodeAvailable()
        {
            BacktraceClient.HandleUnityMessage("foo", string.Empty, UnityEngine.LogType.Error);

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerWithMultipleLogMessage_SourceCodeAvailable()
        {
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, UnityEngine.LogType.Log);
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, UnityEngine.LogType.Warning);

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, UnityEngine.LogType.Exception);

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
        }

        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerWithMultipleLogMessageAndExceptionReport_SourceCodeAvailable()
        {
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, UnityEngine.LogType.Log);
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, UnityEngine.LogType.Warning);

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(new Exception(expectedExceptionMessage));

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
        }


        [Test]
        public void TestSourceCodeAssignment_EnabledLogManagerWithMultipleLogMessageAndMessageReport_SourceCodeAvailable()
        {
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, UnityEngine.LogType.Log);
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, UnityEngine.LogType.Warning);

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(expectedExceptionMessage);

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
        }

        [Test]
        public void TestSourceCodeAssignment_DisabledUnhandledException_ShouldStoreUnhandledExceptionInfo()
        {
            BacktraceClient.Configuration.HandleUnhandledExceptions = false;

            api.LastData = null;

            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, UnityEngine.LogType.Log);
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, UnityEngine.LogType.Warning);

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, UnityEngine.LogType.Exception);
            Assert.IsNull(api.LastData);

            var expectedReportMessage = "Report message";
            var report = new BacktraceReport(new Exception(expectedReportMessage));
            BacktraceClient.Send(report);
            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
            Assert.IsTrue(generatedText.Contains(expectedReportMessage));
        }
    }
}
