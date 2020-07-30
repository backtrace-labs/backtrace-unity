using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class DeduplicaitonTests : BacktraceBaseTest
    {
        private BacktraceDatabaseMock _database;

        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            var configuration = GenerateDefaultConfiguration();
            BacktraceClient.Configuration = configuration;
            _database = GameObject.AddComponent<BacktraceDatabaseMock>();
            _database.Configuration = configuration;
            _database.Reload();
            AfterSetup();
        }

        /// <summary>
        /// Generate specific backtrace configuration object for deduplication testing
        /// </summary>
        protected override BacktraceConfiguration GenerateDefaultConfiguration()
        {
            var configuration = GetBasicConfiguration();
            configuration.DatabasePath = Application.temporaryCachePath;
            configuration.CreateDatabase = false;
            configuration.AutoSendMode = false;
            configuration.Enabled = true;

            return configuration;
        }

        [UnityTest]
        public IEnumerator TestDisabledDeduplicationStrategy_DeduplicationNone_ShouldntMergeReports()
        {
            _database.DeduplicationStrategy = DeduplicationStrategy.None;
            _database.Clear();
            var report = new BacktraceReport(new Exception("Exception Message"));
            var data = report.ToBacktraceData(null, -1);

            // validate total number of reports
            // Count method should return all reports (include reports after deduplicaiton)
            int totalNumberOfReports = 2;
            for (int i = 0; i < totalNumberOfReports; i++)
            {
                _database.Add(data);
            }
            Assert.AreEqual(totalNumberOfReports, _database.Count());
            Assert.AreEqual(totalNumberOfReports, _database.Get().Count());
            yield return null;
        }

        [TestCase(DeduplicationStrategy.Default)]
        [TestCase(DeduplicationStrategy.Classifier)]
        [TestCase(DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.Classifier | DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.Default | DeduplicationStrategy.Classifier | DeduplicationStrategy.Message)]
        public void TestDeduplicationStrategy_TestDifferentStrategies_ReportShouldMerge(DeduplicationStrategy deduplicationStrategy)
        {
            _database.DeduplicationStrategy = deduplicationStrategy;
            _database.Clear();
            var report = new BacktraceReport(new Exception("Exception Message"));
            var data = report.ToBacktraceData(null, -1);
            // validate total number of reports
            // Count method should return all reports (include reports after deduplicaiton)
            int totalNumberOfReports = 2;
            for (int i = 0; i < totalNumberOfReports; i++)
            {
                _database.Add(data);
            }
            Assert.AreEqual(totalNumberOfReports, _database.Count());
            var records = _database.Get();
            int expectedNumberOfReports = 1;
            Assert.AreEqual(expectedNumberOfReports, records.Count());
        }

        //avoid testing default as a single parameter because default will analyse stack trace, which will be the same
        // for both exceptions
        [TestCase(DeduplicationStrategy.Classifier)]
        [TestCase(DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.Classifier | DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.Classifier | DeduplicationStrategy.Default)]
        [TestCase(DeduplicationStrategy.Default | DeduplicationStrategy.Message)]
        public void TestDeduplicaiton_DifferentExceptions_ShouldGenerateDifferentHashForDifferentRerports(DeduplicationStrategy strategy)
        {
            var report1 = new BacktraceReport(new Exception("test"));
            var report2 = new BacktraceReport(new ArgumentException("argument test"));

            var deduplicationStrategy1 = new DeduplicationModel(new BacktraceData(report1), strategy);
            var deduplicationStrategy2 = new DeduplicationModel(new BacktraceData(report2), strategy);

            var sha1 = deduplicationStrategy1.GetSha();
            var sha2 = deduplicationStrategy2.GetSha();

            Assert.AreNotEqual(sha1, sha2);
        }

        [Test]
        public void TestDeduplicaiton_EmptyExceptionMessage_ShouldGenerateCorrectSha()
        {
            var report1 = new BacktraceReport(new Exception(string.Empty));
            var deduplicationStrategy1 = new DeduplicationModel(new BacktraceData(report1), DeduplicationStrategy.Message);

            var sha1 = deduplicationStrategy1.GetSha();
            Assert.IsNotEmpty(sha1);
        }

        [Test]
        public void TestDeduplicaiton_EmptyStackTraceMessage_ShouldGenerateCorrectSha()
        {
            var report1 = new BacktraceReport(new BacktraceUnhandledException(string.Empty, string.Empty));
            var deduplicationStrategy1 = new DeduplicationModel(new BacktraceData(report1), DeduplicationStrategy.Default);

            var sha1 = deduplicationStrategy1.GetSha();
            Assert.IsNotEmpty(sha1);
        }

        [Test]
        public void TestDeduplicaiton_NoClassifier_ShouldGenerateCorrectSha()
        {
            var report1 = new BacktraceReport(string.Empty);
            var deduplicationStrategy1 = new DeduplicationModel(new BacktraceData(report1), DeduplicationStrategy.Classifier);

            var sha1 = deduplicationStrategy1.GetSha();
            Assert.IsNotEmpty(sha1);
        }
    }
}