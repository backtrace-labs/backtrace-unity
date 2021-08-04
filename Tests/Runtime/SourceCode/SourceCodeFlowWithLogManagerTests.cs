using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class SourceCodeFlowWithLogManagerTests : BacktraceBaseTest
    {
        private readonly BacktraceApiMock api = new BacktraceApiMock();
        private readonly int _numberOfLogs = 10;

        [SetUp]
        public void Setup()
        {
            BeforeSetup();

            var configuration = GetValidClientConfiguration();
            configuration.NumberOfLogs = (uint)_numberOfLogs;
            BacktraceClient.Configuration = configuration;
            AfterSetup(true);
            BacktraceClient.BacktraceApi = api;
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendExceptionReport_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.Send(new Exception("foo"));
            yield return new WaitForEndOfFrame();


            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }


        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendMessageReport_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.Send("foo");
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }


        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledException_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.HandleUnityMessage("foo", string.Empty, LogType.Exception);
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledError_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.HandleUnityMessage("foo", string.Empty, LogType.Error);
            yield return new WaitForEndOfFrame();
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerWithMultipleLogMessage_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, LogType.Exception);
            yield return new WaitForEndOfFrame();
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerWithMultipleLogMessageAndExceptionReport_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);
            yield return new WaitForEndOfFrame();
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);
            yield return new WaitForEndOfFrame();

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(new Exception(expectedExceptionMessage));
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
        }


        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerWithMultipleLogMessageAndMessageReport_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);
            yield return new WaitForEndOfFrame();
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);
            yield return new WaitForEndOfFrame();

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(expectedExceptionMessage);
            yield return new WaitForEndOfFrame();
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledUnhandledException_ShouldStoreUnhandledExceptionInfo()
        {
            BacktraceClient.Configuration.HandleUnhandledExceptions = false;

            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);
            yield return new WaitForEndOfFrame();

            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);
            yield return new WaitForEndOfFrame();

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, LogType.Exception);
            yield return new WaitForEndOfFrame();
            Assert.IsNull(api.LastData);

            var expectedReportMessage = "Report message";
            var report = new BacktraceReport(new Exception(expectedReportMessage));
            BacktraceClient.Send(report);
            yield return new WaitForEndOfFrame();
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
            Assert.IsTrue(generatedText.Contains(expectedReportMessage));
        }
    }
}