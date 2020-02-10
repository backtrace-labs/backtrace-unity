using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BacktraceClientTests: BacktraceBaseTest
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

            BacktraceClient.HandleUnhandledExceptions();
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
            Assert.IsTrue(trigger);
            yield return null;
        }

        private BacktraceConfiguration GetValidClientConfiguration()
        {
            var configuration = GetBasicConfiguration();
            BacktraceClient.RequestHandler = (string url, BacktraceData backtraceData) =>
            {
                return new BacktraceResult();
            };
            return configuration;
        }
    }
}
