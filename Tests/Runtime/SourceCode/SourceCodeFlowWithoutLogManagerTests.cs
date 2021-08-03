using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

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

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerAndSendExceptionReport_SourceCodeAvailable()
        {

            var invoked = false;
            BacktraceClient.BeforeSend = (BacktraceData lastData) =>
            {
                invoked = true;
                Assert.IsNotNull(lastData.SourceCode);

                var threadName = lastData.ThreadData.MainThread;
                Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
                return lastData;
            };
            BacktraceClient.Send(new Exception("foo"));
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(invoked);

        }


        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerAndSendMessageReport_SourceCodeAvailable()
        {
            var invoked = false;
            BacktraceClient.BeforeSend = (BacktraceData lastData) =>
            {
                invoked = true;
                Assert.IsNotNull(lastData.SourceCode);

                var threadName = lastData.ThreadData.MainThread;
                Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
                return lastData;
            };
            BacktraceClient.Send("foo");
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(invoked);
        }


        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerAndSendUnhandledException_SourceCodeAvailable()
        {
            var invoked = false;
            BacktraceClient.BeforeSend = (BacktraceData lastData) =>
            {
                invoked = true;
                Assert.IsNotNull(lastData.SourceCode);

                var threadName = lastData.ThreadData.MainThread;
                Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
                return lastData;
            };

            BacktraceClient.HandleUnityMessage("foo", string.Empty, LogType.Exception);
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(invoked);

        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerAndSendUnhandledError_SourceCodeAvailable()
        {
            var invoked = false;
            BacktraceClient.BeforeSend = (BacktraceData lastData) =>
            {
                invoked = true;

                Assert.IsNotNull(lastData.SourceCode);

                var threadName = lastData.ThreadData.MainThread;
                Assert.AreEqual(BacktraceSourceCode.SOURCE_CODE_PROPERTY, lastData.ThreadData.ThreadInformations[threadName].Stack.First().SourceCode);
                return lastData;
            };

            BacktraceClient.HandleUnityMessage("foo", string.Empty, LogType.Error);
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(invoked);
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerWithMultipleLogMessage_SourceCodeAvailable()
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
            BacktraceClient.HandleUnityMessage(expectedExceptionMessage, string.Empty, LogType.Exception);
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsFalse(generatedText.Contains(fakeLogMessage));
            Assert.IsFalse(generatedText.Contains(fakeWarningMessage));
        }

        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerWithMultipleLogMessageAndExceptionReport_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };


            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, UnityEngine.LogType.Log);
            yield return new WaitForEndOfFrame();
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, UnityEngine.LogType.Warning);
            yield return new WaitForEndOfFrame();

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(new Exception(expectedExceptionMessage));
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsFalse(generatedText.Contains(fakeLogMessage));
            Assert.IsFalse(generatedText.Contains(fakeWarningMessage));
        }


        [UnityTest]
        public IEnumerator TestSourceCodeAssignment_DisabledLogManagerWithMultipleLogMessageAndMessageReport_SourceCodeAvailable()
        {
            BacktraceData lastData = null;
            BacktraceClient.BeforeSend = (BacktraceData data) =>
            {
                lastData = data;
                return data;
            };
            //fake messages
            var fakeLogMessage = "log";
            BacktraceClient.HandleUnityMessage(fakeLogMessage, string.Empty, UnityEngine.LogType.Log);
            var fakeWarningMessage = "warning message";
            BacktraceClient.HandleUnityMessage(fakeWarningMessage, string.Empty, UnityEngine.LogType.Warning);

            // real exception
            var expectedExceptionMessage = "Exception message";
            BacktraceClient.Send(expectedExceptionMessage);
            yield return new WaitForEndOfFrame();

            Assert.IsNotNull(lastData.SourceCode);

            var generatedText = lastData.SourceCode.Text;
            Assert.IsTrue(generatedText.Contains(expectedExceptionMessage));
            Assert.IsFalse(generatedText.Contains(fakeLogMessage));
            Assert.IsFalse(generatedText.Contains(fakeWarningMessage));
        }
    }
}