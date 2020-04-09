using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class RateLimitTests: BacktraceBaseTest
    {
        private const int CLIENT_RATE_LIMIT = 3;

        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            BacktraceClient.Configuration = GetBasicConfiguration();
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            AfterSetup();
        }
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void TestReportLimit_ShouldntHitRateLimit_AllReportsShouldBeInBacktrace(int reportPerMin)
        {
            uint rateLimit = Convert.ToUInt32(reportPerMin);
            BacktraceClient.SetClientReportLimit(rateLimit);
            int maximumNumberOfRetries = 0;
            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
            {
                maximumNumberOfRetries++;
                return new BacktraceResult();
            };
            int skippedReports = 0;
            BacktraceClient.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                skippedReports++;
            };
            for (int i = 0; i < rateLimit; i++)
            {
                BacktraceClient.Send("test");
            }
            Assert.AreEqual(maximumNumberOfRetries, rateLimit);
            Assert.AreEqual(0, skippedReports);
        }

        [UnityTest]
        public IEnumerator TestReportLimit_TestSendingMessage_SkippProcessingReports()
        {
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            int totalNumberOfReports = 5;
            int maximumNumberOfRetries = 0;
            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
            {
                maximumNumberOfRetries++;
                return new BacktraceResult();
            };
            int skippedReports = 0;
            BacktraceClient.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                skippedReports++;
            };

            for (int i = 0; i < totalNumberOfReports; i++)
            {
                BacktraceClient.Send("test");
            }
            Assert.AreEqual(totalNumberOfReports, maximumNumberOfRetries + skippedReports);
            Assert.AreEqual(maximumNumberOfRetries, CLIENT_RATE_LIMIT);
            Assert.AreEqual(totalNumberOfReports - CLIENT_RATE_LIMIT, skippedReports);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportLimit_TestSendingError_SkippProcessingReports()
        {
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            int totalNumberOfReports = 5;
            int maximumNumberOfRetries = 0;
            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
            {
                maximumNumberOfRetries++;
                return new BacktraceResult();
            };
            int skippedReports = 0;
            BacktraceClient.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                skippedReports++;
            };

            for (int i = 0; i < totalNumberOfReports; i++)
            {
                BacktraceClient.Send(new Exception("Exception"));

            }
            Assert.AreEqual(totalNumberOfReports, maximumNumberOfRetries + skippedReports);
            Assert.AreEqual(maximumNumberOfRetries, CLIENT_RATE_LIMIT);
            Assert.AreEqual(totalNumberOfReports - CLIENT_RATE_LIMIT, skippedReports);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestReportLimit_TestSendingBacktraceReport_SkippProcessingReports()
        {
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            int totalNumberOfReports = 5;
            int maximumNumberOfRetries = 0;
            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
            {
                maximumNumberOfRetries++;
                return new BacktraceResult();
            };
            int skippedReports = 0;
            BacktraceClient.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                skippedReports++;
            };

            for (int i = 0; i < totalNumberOfReports; i++)
            {
                var report = new BacktraceReport(new Exception("Exception"));
                BacktraceClient.Send(report);
            }
            Assert.AreEqual(totalNumberOfReports, maximumNumberOfRetries + skippedReports);
            Assert.AreEqual(maximumNumberOfRetries, CLIENT_RATE_LIMIT);
            Assert.AreEqual(totalNumberOfReports - CLIENT_RATE_LIMIT, skippedReports);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportLimit_InvalidReportNumber_IgnoreAdditionalReports()
        {
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            int totalNumberOfReports = 5;
            int maximumNumberOfRetries = 0;
            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
             {
                 maximumNumberOfRetries++;
                 return new BacktraceResult();
             };
            int skippedReports = 0;
            BacktraceClient.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                skippedReports++;
            };

            for (int i = 0; i < totalNumberOfReports; i++)
            {
                BacktraceClient.Send("test");

            }
            Assert.AreEqual(totalNumberOfReports, maximumNumberOfRetries + skippedReports);
            Assert.AreEqual(maximumNumberOfRetries, CLIENT_RATE_LIMIT);
            Assert.AreEqual(totalNumberOfReports - CLIENT_RATE_LIMIT, skippedReports);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestReportLimit_ValidReportNumber_AddAllReports()
        {
            BacktraceClient.SetClientReportLimit(CLIENT_RATE_LIMIT);
            int maximumNumberOfRetries = 0;
            BacktraceClient.RequestHandler = (string url, BacktraceData data) =>
            {
                maximumNumberOfRetries++;
                return new BacktraceResult();
            };
            for (int i = 0; i < 2; i++)
            {
                BacktraceClient.Send("test");
            }
            Assert.AreEqual(2, maximumNumberOfRetries);
            yield return null;
        }
    }
}
