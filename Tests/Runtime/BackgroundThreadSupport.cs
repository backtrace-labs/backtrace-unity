using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BackgroundThreadSupport : BacktraceBaseTest
    {
        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            BacktraceClient.Configuration = GetBasicConfiguration();
            AfterSetup();
        }

        [Test]
        public void TestBackgroundThreadSupport_BackgroundExceptionShouldntThrow_ExceptionIsSavedInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var thread = new Thread(() =>
            {
                Assert.IsTrue(Thread.CurrentThread.ManagedThreadId != mainThreadId);
                var exception = new InvalidOperationException(exceptionMessage);
                client.Send(exception);
            });
            thread.Start();
            thread.Join();

            Assert.IsNotEmpty(BacktraceClient.BackgroundExceptions);
            Assert.AreEqual(exceptionMessage, BacktraceClient.BackgroundExceptions.First().Message);
            Assert.IsTrue(BacktraceClient.BackgroundExceptions.First().ExceptionTypeReport);
        }


        [Test]
        public void TestBackgroundThreadSupport_BackgroundReportShouldntThrow_ExceptionIsSavedInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var thread = new Thread(() =>
             {
                 Assert.IsTrue(Thread.CurrentThread.ManagedThreadId != mainThreadId);
                 var exception = new InvalidOperationException(exceptionMessage);
                 client.Send(new BacktraceReport(exception));
             });
            thread.Start();
            thread.Join();

            Assert.IsNotEmpty(BacktraceClient.BackgroundExceptions);
            Assert.AreEqual(exceptionMessage, BacktraceClient.BackgroundExceptions.First().Message);
            Assert.IsTrue(BacktraceClient.BackgroundExceptions.First().ExceptionTypeReport);
        }

        [Test]
        public void TestBackgroundThreadSupport_BackgroundReportWithAttributesShouldntThrow_ExceptionIsSavedInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";
            var attributeKey = "attribute-key";
            var attributeValue = exceptionMessage;
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var thread = new Thread(() =>
            {
                Assert.IsTrue(Thread.CurrentThread.ManagedThreadId != mainThreadId);
                var exception = new InvalidOperationException(exceptionMessage);
                client.Send(new BacktraceReport(exception, new Dictionary<string, string> { { attributeKey, attributeValue } }));
            });
            thread.Start();
            thread.Join();

            Assert.IsNotEmpty(BacktraceClient.BackgroundExceptions);
            var storedReport = BacktraceClient.BackgroundExceptions.First();
            Assert.AreEqual(exceptionMessage, storedReport.Message);
            Assert.IsTrue(storedReport.ExceptionTypeReport);
            Assert.AreEqual(storedReport.Attributes[attributeKey], attributeValue);
        }


        [Test]
        public void TestBackgroundThreadSupport_BackgroundMessageShouldntThrow_ExceptionIsSavedInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var thread = new Thread(() =>
             {
                 Assert.IsTrue(Thread.CurrentThread.ManagedThreadId != mainThreadId);
                 client.Send(exceptionMessage);
             });
            thread.Start();
            thread.Join();

            Assert.IsNotEmpty(BacktraceClient.BackgroundExceptions);
            Assert.AreEqual(exceptionMessage, BacktraceClient.BackgroundExceptions.First().Message);
            Assert.IsFalse(BacktraceClient.BackgroundExceptions.First().ExceptionTypeReport);
        }

        [Test]
        public void TestBackgroundThreadSupport_BackgroundUnhandledExceptionShouldntThrow_ExceptionIsSavedInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            var thread = new Thread(() =>
            {
                Assert.IsTrue(Thread.CurrentThread.ManagedThreadId != mainThreadId);
                client.HandleUnityBackgroundException(exceptionMessage, string.Empty, UnityEngine.LogType.Exception);
            });
            thread.Start();
            thread.Join();

            Assert.IsNotEmpty(BacktraceClient.BackgroundExceptions);
            Assert.AreEqual(exceptionMessage, BacktraceClient.BackgroundExceptions.First().Message);
            Assert.IsTrue(BacktraceClient.BackgroundExceptions.First().ExceptionTypeReport);
        }

        [Test]
        public void TestBackgroundThreadSupport_UserShouldBeAbleToFilterUnhandledExceptions_ReportShouldntBeAvailableInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";

            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string message) =>
            {
                return true;
            };
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var thread = new Thread(() =>
             {
                 Assert.IsTrue(Thread.CurrentThread.ManagedThreadId != mainThreadId);
                 client.HandleUnityBackgroundException(exceptionMessage, string.Empty, UnityEngine.LogType.Exception);
             });
            thread.Start();
            thread.Join();

            Assert.IsEmpty(BacktraceClient.BackgroundExceptions);
        }

        [Test]
        public void TestBackgroundThreadSupport_UserShouldBeAbleToFilterReports_ReportShouldntBeAvailableInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";

            BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string message) =>
            {
                return true;
            };
            var thread = new Thread(() =>
            {
                client.Send(exceptionMessage);
                var exception = new InvalidOperationException(exceptionMessage);
                client.Send(exception);
                client.Send(new BacktraceReport(exception));
            });

            thread.Start();
            thread.Join();

            Assert.IsEmpty(BacktraceClient.BackgroundExceptions);
        }


        [Test]
        public void TestBackgroundThreadSupport_RateLimitSkipReports_ReportShouldntBeAvailableInMainThreadLoop()
        {
            var client = BacktraceClient;
            string exceptionMessage = "foo";
            
            uint rateLimit = 5;
            var expectedNumberOfSkippedReports = 5;
            int actualNumberOfSkippedReports = 0;

            client.SetClientReportLimit(rateLimit);
            client.OnClientReportLimitReached = (BacktraceReport report) =>
            {
                actualNumberOfSkippedReports++;
            };

            var thread = new Thread(() =>
            {
                for (int i = 0; i < rateLimit + expectedNumberOfSkippedReports; i++)
                {
                    client.Send(new InvalidOperationException(exceptionMessage));
                }

            });
            thread.Start();
            thread.Join();
            Assert.IsNotEmpty(BacktraceClient.BackgroundExceptions);
            Assert.AreEqual(rateLimit, BacktraceClient.BackgroundExceptions.Count);
            Assert.AreEqual(expectedNumberOfSkippedReports, actualNumberOfSkippedReports);
        }
    }
}
