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
        private const int CLIENT_RATE_LIMIT = 3;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject();
            gameObject.SetActive(false);
            client = gameObject.AddComponent<BacktraceClient>();
            client.Configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            client.Configuration.ServerUrl = "https://submit.backtrace.io/test/1234123412341234123412341234123412341234123412341234123412341234/json";
            client.Refresh();
            client.SetClientReportLimit(CLIENT_RATE_LIMIT);
            gameObject.SetActive(true);
        }

        [UnityTest]
        public IEnumerator TestReportLimit_InvalidReportNumber_IgnoreAdditionalReports()
        {
            int totalNumberOfReports = 5;
            int maximumNumberOfRetries = 0;
            client.RequestHandler = (string url, BacktraceData data) =>
             {
                 maximumNumberOfRetries++;
                 return new BacktraceResult();
             };
            int skippedReports = 0;
            client.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                skippedReports++;
            };

            for (int i = 0; i < totalNumberOfReports; i++)
            {
                client.Send("test");

            }
            Assert.AreEqual(totalNumberOfReports, maximumNumberOfRetries + skippedReports);
            Assert.AreEqual(maximumNumberOfRetries, CLIENT_RATE_LIMIT);
            Assert.AreEqual(totalNumberOfReports - CLIENT_RATE_LIMIT, skippedReports);
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
