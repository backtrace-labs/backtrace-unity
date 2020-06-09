using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class ClientSendTests
    {
        private BacktraceClient client;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject();
            gameObject.SetActive(false);
            client = gameObject.AddComponent<BacktraceClient>();
            client.Configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            client.Configuration.ServerUrl = "https://submit.backtrace.io/test/1234123412341234123412341234123412341234123412341234123412341234/json";
            client.Configuration.DestroyOnLoad = true;
            gameObject.SetActive(true);
            client.Refresh();
        }

        [UnityTest]
        public IEnumerator SendReport_ExceptionReport_ValidSend()
        {
            var trigger = false;
            var exception = new Exception("custom exception message");
            client.RequestHandler = (string url, BacktraceData data) =>
            {
                trigger = true;
                Assert.IsTrue(data.Classifier[0] == exception.GetType().Name);
                string message = data.Attributes.Attributes["error.message"] as string;
                Assert.IsTrue(message == exception.Message);
                return new BacktraceResult();
            };
            client.Send(exception);
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SendReport_MessageReport_ValidSend()
        {
            var trigger = false;
            var clientMessage = "custom message";
            var report = new BacktraceReport(clientMessage);
            client.RequestHandler = (string url, BacktraceData data) =>
            {
                trigger = true;
                string message = data.Attributes.Attributes["error.message"] as string;
                Assert.IsTrue(message == clientMessage);
                return new BacktraceResult();
            };
            client.Send(report, sendCallback: (BacktraceResult _) =>
            {
                trigger = true;
            });
            Assert.IsTrue(trigger);
            yield return null;
        }
    }
}
