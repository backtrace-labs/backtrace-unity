using UnityEngine;
using Backtrace.Unity.Model;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceBaseTest
    {
        protected GameObject GameObject;
        protected BacktraceClient BacktraceClient;
        protected void BeforeSetup()
        {
            Debug.unityLogger.logEnabled = false;
            GameObject = new GameObject();
            GameObject.SetActive(false);
            BacktraceClient = GameObject.AddComponent<BacktraceClient>();
        }

        protected void AfterSetup(bool refresh = true)
        {
            if (refresh)
            {
                BacktraceClient.Refresh();
            }
            GameObject.SetActive(true);
        }

        protected BacktraceConfiguration GetBasicConfiguration()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.ServerUrl = "https://submit.backtrace.io/test/token/json";
            configuration.DestroyOnLoad = true;
            return configuration;
        }

        protected BacktraceConfiguration GetValidClientConfiguration()
        {
            var configuration = GetBasicConfiguration();
            BacktraceClient.RequestHandler = (string url, BacktraceData backtraceData) =>
            {
                return new BacktraceResult();
            };
            return configuration;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(GameObject);
        }
    }
}