using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BacktraceClientTests
    {
        private BacktraceClient client;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject();
            gameObject.SetActive(false);
            client = gameObject.AddComponent<BacktraceClient>();
            client.Configuration = null;
            gameObject.SetActive(true);
        }

        [UnityTest]
        public IEnumerator TestClientCreation_ValidBacktraceConfiguration_ValidClientCreation()
        {
            var clientConfiguration = GetValidClientConfiguration();
            client.Configuration = clientConfiguration;
            client.Refresh();
            Assert.IsTrue(client.Enabled);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestClientCreation_EmptyConfiguration_DisabledClientCreation()
        {
            Assert.IsFalse(client.Enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientEvents_EmptyConfiguration_ShouldntThrowExceptionForDisabledClient()
        {
            Assert.IsFalse(client.Enabled);

            client.HandleUnhandledExceptions();
            Assert.IsNull(client.OnServerError);
            Assert.IsNull(client.OnServerResponse);
            Assert.IsNull(client.BeforeSend);
            Assert.IsNull(client.RequestHandler);
            Assert.IsNull(client.OnUnhandledApplicationException);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestUnvailableEvents_EmptyConfiguration_ShouldntThrowException()
        {
            client.OnServerError = (Exception e) => { };
            client.OnServerResponse = (BacktraceResult r) => { };
            client.BeforeSend = (BacktraceData d) => d;
            client.OnUnhandledApplicationException = (Exception e) => { };

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSendEvent_DisabledApi_NotSendingEvent()
        {
            client.Configuration = GetValidClientConfiguration();
            client.Refresh();
            Assert.DoesNotThrow(() => client.Send(new Exception("test exception")));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBeforeSendEvent_ValidConfiguration_EventTrigger()
        {
            var trigger = false;
            client.Configuration = GetValidClientConfiguration();
            client.Refresh();
            client.BeforeSend = (BacktraceData backtraceData) =>
            {
                trigger = true;
                return backtraceData;
            };
            client.Send(new Exception("test exception"));
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSendingReport_ValidConfiguration_ValidSend()
        {
            var trigger = false;
            client.Configuration = GetValidClientConfiguration();
            client.Refresh();

            client.RequestHandler = (string url, BacktraceData data) =>
            { 
                Assert.IsNotNull(data);
                Assert.IsFalse(string.IsNullOrEmpty(data.ToJson()));
                trigger = true;
                return new BacktraceResult();
            };
            client.Send(new Exception("test exception"));
            Assert.IsTrue(trigger);
            yield return null;
        }

        private BacktraceConfiguration GetValidClientConfiguration()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.ServerUrl = "https://test.sp.backtrace.io:6097/";
            configuration.Token = "1234123412341234123412341234123412341234123412341234123412341234";
            configuration.DestroyOnLoad = true;
            return configuration;
        }

    }
}
