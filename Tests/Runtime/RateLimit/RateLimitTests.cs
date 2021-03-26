using Backtrace.Unity.Common;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Backtrace.Unity.Tests.Runtime.RateLimit
{
    public class RateLimitTests
    {
        [Test]
        public void TestReportLimit_ShouldDeclineLastReport_ShouldReturnFalseForLastRecord()
        {
            uint reportLimitWatcherSize = 5;
            var reportLimitWatcher = new ReportLimitWatcher(reportLimitWatcherSize);
            var timestamp = DateTimeHelper.Timestamp();
            for (int i = 0; i < reportLimitWatcherSize; i++)
            {
                var result = reportLimitWatcher.WatchReport(timestamp);
                Assert.IsTrue(result);
            }
            var shouldFail = reportLimitWatcher.WatchReport(timestamp);
            Assert.IsFalse(shouldFail);
        }

        [Test]
        public void TestReportLimitWarningMessage_ShouldPrintWarningMessage_ShouldDisplayMessageMethodShouldReturnTrue()
        {
            uint reportLimitWatcherSize = 5;
            var reportLimitWatcher = new ReportLimitWatcher(reportLimitWatcherSize);
            var timestamp = DateTimeHelper.Timestamp();
            for (int i = 0; i < reportLimitWatcherSize; i++)
            {
                var result = reportLimitWatcher.WatchReport(timestamp);
                Assert.IsTrue(result);
            }

            var shouldFail = reportLimitWatcher.WatchReport(timestamp, false);
            Assert.IsFalse(shouldFail);
            Assert.IsTrue(reportLimitWatcher.ShouldDisplayMessage());
        }

        [Test]
        public void TestReportLimitFromMultipleThreads_ShouldDeclineReportAfterLimitHit_ShouldReturnFalseWhenLimitHit()
        {
            uint reportLimitWatcherSize = 5;
            var numberOfThreads = 3;

            var acceptedReports = 0;
            var declinedReports = 0;
            var reportLimitWatcher = new ReportLimitWatcher(reportLimitWatcherSize);

            // create and start multiple threads that will use report limit watcher
            // simulate multiple update methods that generate reports
            var threads = new List<Thread>();
            for (int threadIndex = 0; threadIndex < numberOfThreads; threadIndex++)
            {
                threads.Add(new Thread(() =>
                {
                    for (int i = 0; i < reportLimitWatcherSize; i++)
                    {
                        var result = reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp());
                        if (result)
                        {
                            acceptedReports++;
                        }
                        else
                        {
                            declinedReports++;
                        }
                    }
                }));
            }

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            var numberOfTries = numberOfThreads * reportLimitWatcherSize;
            // validate how many reports we tried to store
            Assert.AreEqual(numberOfTries, acceptedReports + declinedReports);
            // validate how many reports we stored in limit queue
            Assert.AreEqual(reportLimitWatcherSize, acceptedReports);
            // validate how many reports we declined
            Assert.AreEqual(numberOfTries - reportLimitWatcherSize, declinedReports);
        }
    }
}