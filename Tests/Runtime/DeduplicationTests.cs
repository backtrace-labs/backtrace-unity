using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using Backtrace.Unity.Extensions;

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
        private BacktraceConfiguration GenerateDefaultConfiguration()
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

            // validate total number of reports
            // Count method should return all reports (include reports after deduplicaiton)
            int totalNumberOfReports = 2;
            for (int i = 0; i < totalNumberOfReports; i++)
            {
                _database.Add(report, new Dictionary<string, string>(), MiniDumpType.None);
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

            // validate total number of reports
            // Count method should return all reports (include reports after deduplicaiton)
            int totalNumberOfReports = 2;
            for (int i = 0; i < totalNumberOfReports; i++)
            {
                _database.Add(report, new Dictionary<string, string>(), MiniDumpType.None);
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
        public void TestFingerprintBehavior_ShouldGenerateFingerprintForExceptionReportWithoutStackTrace_ShouldIncludeFingerprintInBacktraceReport()
        {
            // exception without stack trace might happened when exception occured because of
            // invalid game object setting or via weird crash
            // exception below has empty exception stack trace
            var exception = new BacktraceUnhandledException("00:00:00 00/00/00 Unhandled exception", string.Empty);

            var report = new BacktraceReport(exception);
            Assert.AreEqual(exception.Message.OnlyLetters().GetSha(), report.Attributes["_mod_fingerprint"]);
            var data = new BacktraceData(report, null);
            Assert.IsNotEmpty(data.Attributes.Attributes["_mod_fingerprint"].ToString());
        }


        [Test]
        public void TestFingerprintBehavior_ShouldGenerateFingerprintWithOnlyLetters_ShouldIncludeFingerprintInBacktraceReport()
        {
            var exception = new BacktraceUnhandledException("00:00:00 00/00/00 Unhandled exception", string.Empty);
            var report = new BacktraceReport(exception);
            Assert.AreEqual(exception.Message.OnlyLetters().GetSha(), report.Attributes["_mod_fingerprint"]);
        }

        [Test]
        public void TestFingerprintBehavior_ShouldGenerateFingerprintAndShouldntRemoveLetters_ShouldIncludeFingerprintInBacktraceReport()
        {
            var exception = new BacktraceUnhandledException("Unhandled exception", string.Empty);
            var report = new BacktraceReport(exception);
            Assert.AreEqual(exception.Message.OnlyLetters().GetSha(), report.Attributes["_mod_fingerprint"]);
        }

        [Test]
        public void TestFingerprintBehavior_ShouldntGenerateFingerprintForExistingStackTrace_ShouldIgnoreAttributeFingerprint()
        {
            var exception = new BacktraceUnhandledException("Unhandled exception", "foo()");
            var report = new BacktraceReport(exception);
            Assert.IsFalse(report.Attributes.ContainsKey("_mod_fingerprint"));
        }
    }
}