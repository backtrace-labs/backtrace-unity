using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Linq;

namespace Backtrace.Unity.Tests.Runtime
{
    public class SourceCodeFlowWithoutLogManagerTests : BacktraceBaseTest
    {
        private readonly BacktraceApiMock api = new BacktraceApiMock();

        [OneTimeSetUp]
        public void Setup()
        {
            BeforeSetup();

            var configuration = GetValidClientConfiguration();
            configuration.NumberOfLogs = 0;
            BacktraceClient.Configuration = configuration;
            AfterSetup(true);
            BacktraceClient.BacktraceApi = api;
        }

        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerAndSendExceptionReport_SourceCodeAvailable()
        {
            BacktraceClient.Send(new Exception("foo"));

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }


        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerAndSendMessageReport_SourceCodeAvailable()
        {
            BacktraceClient.Send("foo");

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }


        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerAndSendUnhandledException_SourceCodeAvailable()
        {
            BacktraceClient.HandleUnityMessage("foo", string.Empty, UnityEngine.LogType.Exception);

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerAndSendUnhandledError_SourceCodeAvailable()
        {
            BacktraceClient.HandleUnityMessage("foo", string.Empty, UnityEngine.LogType.Error);

            var lastData = api.LastData;
            Assert.IsNotNull(lastData.SourceCode);

            var threadName = lastData.ThreadData.MainThread;
            Assert.AreEqual(lastData.SourceCode.Id, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
        }

        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerWithMultipleLogMessage_SourceCodeAvailable()
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
            Assert.IsFalse(generatedText.Contains(fakeLogMessage));
            Assert.IsFalse(generatedText.Contains(fakeWarningMessage));
        }

        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerWithMultipleLogMessageAndExceptionReport_SourceCodeAvailable()
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
            Assert.IsFalse(generatedText.Contains(fakeLogMessage));
            Assert.IsFalse(generatedText.Contains(fakeWarningMessage));
        }


        [Test]
        public void TestSourceCodeAssignment_DisabledLogManagerWithMultipleLogMessageAndMessageReport_SourceCodeAvailable()
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
            Assert.IsFalse(generatedText.Contains(fakeLogMessage));
            Assert.IsFalse(generatedText.Contains(fakeWarningMessage));
        }
    }
}
