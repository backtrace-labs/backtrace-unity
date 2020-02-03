using Backtrace.Unity;
using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class DeduplicaitonTests
    {
        private GameObject _gameObject;
        private BacktraceDatabase _database;
        private BacktraceClient _client;

        [SetUp]
        public void Setup()
        {
            _gameObject = new GameObject();
            _gameObject.SetActive(false);
            var configuration = GenerateDefaultConfiguration();
            _client = _gameObject.AddComponent<BacktraceClient>();
            _client.Configuration = configuration;
            _database = _gameObject.AddComponent<BacktraceDatabase>();
            _database.Configuration = configuration;
            _database.Reload();
        }

        private BacktraceConfiguration GenerateDefaultConfiguration()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.ServerUrl = "https://test.sp.backtrace.io:6097/";
            configuration.DatabasePath = Application.dataPath;
            configuration.CreateDatabase = false;
            configuration.AutoSendMode = false;
            configuration.Enabled = true;
            configuration.DestroyOnLoad = true;

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
                _database.Add(report, new Dictionary<string, object>(), MiniDumpType.None);
            }
            Assert.AreEqual(totalNumberOfReports, _database.Count());
            Assert.AreEqual(totalNumberOfReports, _database.Get().Count());
            yield return null;
        }

        [TestCase(DeduplicationStrategy.Default)]
        [TestCase(DeduplicationStrategy.LibraryName)]
        [TestCase(DeduplicationStrategy.Classifier)]
        [TestCase(DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.LibraryName | DeduplicationStrategy.Classifier)]
        [TestCase(DeduplicationStrategy.LibraryName | DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.Classifier | DeduplicationStrategy.Message)]
        [TestCase(DeduplicationStrategy.LibraryName | DeduplicationStrategy.Classifier | DeduplicationStrategy.Message)]        
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
                _database.Add(report, new Dictionary<string, object>(), MiniDumpType.None);
            }
            Assert.AreEqual(totalNumberOfReports, _database.Count());
            var records = _database.Get();
            int expectedNumberOfReports = 1;
            Assert.AreEqual(expectedNumberOfReports, records.Count());
        }
    }
}