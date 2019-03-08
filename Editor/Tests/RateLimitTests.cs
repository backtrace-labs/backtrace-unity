using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class RateLimitTests
    {
        private BacktraceClient client;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject();
            gameObject.SetActive(false);
            client = gameObject.AddComponent<BacktraceClient>();
            client.Configuration = new BacktraceConfiguration()
            {
                ServerUrl = "https://submit.backtrace.io/test/1234123412341234123412341234123412341234123412341234123412341234/json"
            };
            client.SetClientReportLimit(3);
            gameObject.SetActive(true);
            client.Refresh();
        }

        [UnityTest]
        public IEnumerator TestReportLimit_InvalidReportNumber_IgnoreAdditionalReports()
        {
            int maximumNumberOfRetries = 0;
            client.RequestHandler = (string url, BacktraceData data) =>
             {
                 maximumNumberOfRetries++;
                 return new BacktraceResult();
             };
            for (int i = 0; i < 5; i++)
            {
                client.Send("test");
            }
            Assert.AreEqual(5, maximumNumberOfRetries);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportLimit_ValidReportNumber_AddAllReports()
        {
            int maximumNumberOfRetries = 0;
            client.RequestHandler = (string url, BacktraceData data) =>
            {
                maximumNumberOfRetries++;
                return new BacktraceResult();
            };
            for (int i = 0; i < 2; i++)
            {
                client.Send("test");
            }
            Assert.AreEqual(2, maximumNumberOfRetries);
            yield return null;
        }
    }
}
