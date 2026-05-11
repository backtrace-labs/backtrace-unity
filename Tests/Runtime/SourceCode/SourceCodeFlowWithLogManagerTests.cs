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

        private const string ParsedUnityStackTrace =
            "Backtrace.Unity.Tests.Runtime.SourceCodeExample.TestMethod() (at Assets/Tests/SourceCodeExample.cs:123)";

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

            yield return WaitForFrame.Wait();

            AssertFirstFrameSourceCode(lastData);

            yield return null;
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

            yield return WaitForFrame.Wait();

            AssertFirstFrameSourceCode(lastData);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledException_EmptyUnityStack_SourceCodeAvailableWithoutFrameReferences()
        {
            BacktraceData lastData = null;
            const string expectedMessage = "foo";

            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.HandleUnityMessage(expectedMessage, string.Empty, LogType.Exception);

            yield return WaitForFrame.Wait();

            AssertStacklessSourceCode(lastData, expectedMessage);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledError_EmptyUnityStack_SourceCodeAvailableWithoutFrameReferences()
        {
            BacktraceData lastData = null;
            const string expectedMessage = "foo";

            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.HandleUnityMessage(expectedMessage, string.Empty, LogType.Error);

            yield return WaitForFrame.Wait();

            AssertStacklessSourceCode(lastData, expectedMessage);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledException_WithUnityStack_FrameSourceCodeAvailable()
        {
            BacktraceData lastData = null;

            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.HandleUnityMessage("foo", ParsedUnityStackTrace, LogType.Exception);

            yield return WaitForFrame.Wait();

            AssertFirstFrameSourceCode(lastData);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_EnabledLogManagerAndSendUnhandledError_WithUnityStack_FrameSourceCodeAvailable()
        {
            BacktraceData lastData = null;

            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };

            BacktraceClient.HandleUnityMessage("foo", ParsedUnityStackTrace, LogType.Error);

            yield return WaitForFrame.Wait();

            AssertFirstFrameSourceCode(lastData);

            yield return null;
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

            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);

            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);

            var expectedExceptionMessage = "Exception message";
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, LogType.Exception);

            yield return WaitForFrame.Wait();

            Assert.IsNotNull(lastData);
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));

            yield return null;
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

            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);

            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);

            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(new Exception(expectedExceptionMessage));

            yield return WaitForFrame.Wait();

            Assert.IsNotNull(lastData);
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));

            yield return null;
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

            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);

            yield return WaitForFrame.Wait();

            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);

            yield return WaitForFrame.Wait();

            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(expectedExceptionMessage);

            yield return WaitForFrame.Wait();

            Assert.IsNotNull(lastData);
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));

            yield return null;
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

            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, LogType.Log);

            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, LogType.Warning);

            var expectedExceptionMessage = "Exception message";
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, LogType.Exception);

            Assert.IsNull(api.LastData);

            var expectedReportMessage = "Report message";
            var report = new BacktraceReport(new Exception(expectedReportMessage));

            BacktraceClient.Send(report);

            yield return WaitForFrame.Wait();

            Assert.IsNotNull(lastData);
            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsTrue(generatedText.Contains(fakeLogMessage));
            Assert.IsTrue(generatedText.Contains(fakeWarningMessage));
            Assert.IsTrue(generatedText.Contains(expectedReportMessage));

            yield return null;
        }

        private static void AssertFirstFrameSourceCode(BacktraceData data)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.SourceCode);
            Assert.IsNotNull(data.ThreadData);

            var threadName = data.ThreadData.MainThread;

            Assert.IsTrue(data.ThreadData.ThreadInformations.ContainsKey(threadName));
            Assert.Greater(data.ThreadData.ThreadInformations[threadName].Stack.Count(), 0);
            Assert.AreEqual(
                BacktraceSourceCode.SOURCE_CODE_PROPERTY,
                data.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        private static void AssertStacklessSourceCode(BacktraceData data, string expectedMessage)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.SourceCode);
            Assert.IsTrue(data.SourceCode.Text.Contains(expectedMessage));
            Assert.IsNotNull(data.ThreadData);

            var threadName = data.ThreadData.MainThread;

            Assert.IsTrue(data.ThreadData.ThreadInformations.ContainsKey(threadName));
            Assert.AreEqual(0, data.ThreadData.ThreadInformations[threadName].Stack.Count());
        }
    }
}
