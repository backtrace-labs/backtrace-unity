using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
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

        [TearDown]
        public void Cleanup()
        {
            client.RequestHandler = null;
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
                string message = data.Attributes.Attributes["error.message"];
                Assert.IsTrue(message == exception.Message);
                return new BacktraceResult();
            };
            client.Send(exception);

            yield return new WaitForEndOfFrame();
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
                string message = data.Attributes.Attributes["error.message"];
                Assert.IsTrue(message == clientMessage);
                return new BacktraceResult();
            };
            client.Send(report, sendCallback: (BacktraceResult _) =>
            {
                trigger = true;
            });
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PiiTests_ShouldRemoveEnvironmentVariables_AnnotationsShouldntBeAvailable()
        {
            var trigger = false;
            var exception = new Exception("custom exception message");
            client.BeforeSend = (BacktraceData data) =>
             {
                 Assert.IsNotNull(data.Annotation.EnvironmentVariables);
                 data.Annotation.EnvironmentVariables = null;
                 return data;
             };

            client.RequestHandler = (string url, BacktraceData data) =>
            {
                trigger = true;
                Assert.IsNull(data.Annotation.EnvironmentVariables);
                return new BacktraceResult();
            };
            client.Send(exception);

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PiiTests_ShouldChangeApplicationDataPath_ApplicationDataPathDoesntHaveUserNameAnymore()
        {
            var trigger = false;
            var exception = new Exception("custom exception message");
            var expectedDataPath = "/some/path";
            client.BeforeSend = (BacktraceData data) =>
            {
                Assert.IsNotNull(data.Attributes.Attributes["application.data_path"]);
                data.Attributes.Attributes["application.data_path"] = expectedDataPath;
                return data;
            };
            client.RequestHandler = (string url, BacktraceData data) =>
            {
                trigger = true;
                Assert.AreEqual(expectedDataPath, data.Attributes.Attributes["application.data_path"]);
                return new BacktraceResult();
            };
            client.Send(exception);

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PiiTests_ShouldModifyEnvironmentVariable_IntegrationShouldUseModifiedEnvironmentVariables()
        {
            var trigger = false;
            var exception = new Exception("custom exception message");

            var environmentVariableKey = "foo";
            var expectedValue = "bar";
            Annotations.EnvironmentVariablesCache[environmentVariableKey] = expectedValue;

            client.BeforeSend = (BacktraceData data) =>
            {
                var actualValue = data.Annotation.EnvironmentVariables[environmentVariableKey];
                Assert.AreEqual(expectedValue, actualValue);
                trigger = true;
                return data;
            };
            client.Send(exception);

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }


        [UnityTest]
        public IEnumerator PiiTests_ShouldRemoveEnvironmentVariableValue_IntegrationShouldUseModifiedEnvironmentVariables()
        {
            var trigger = false;
            var exception = new Exception("custom exception message");

            var environmentVariableKey = "USERNAME";
            var expectedValue = "%USERNAME%";
            if (!Annotations.EnvironmentVariablesCache.ContainsKey(environmentVariableKey))
            {
                Annotations.EnvironmentVariablesCache[environmentVariableKey] = "fake user name";
            }

            var defaultUserName = Annotations.EnvironmentVariablesCache[environmentVariableKey];
            Annotations.EnvironmentVariablesCache[environmentVariableKey] = expectedValue;

            client.BeforeSend = (BacktraceData data) =>
            {
                var actualValue = data.Annotation.EnvironmentVariables[environmentVariableKey];
                Assert.AreEqual(expectedValue, actualValue);
                Assert.AreNotEqual(defaultUserName, actualValue);
                trigger = true;
                return data;
            };
            client.Send(exception);

            yield return new WaitForEndOfFrame();
            Assert.IsTrue(trigger);
            yield return null;
        }
    }
}
