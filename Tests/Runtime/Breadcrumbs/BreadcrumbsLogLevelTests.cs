using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.InMemory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Breadcrumbs
{
    public class BreadcrumbsLogLevelTests
    {
        private const BacktraceBreadcrumbType ManualBreadcrumbsType = BacktraceBreadcrumbType.Manual;

        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Assert)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Exception)]
        public void TestLogLevel_ShouldFilterLogLevel_BreadcrumbIsNotAvailable(LogType testedLevel)
        {
            const string message = "message";
            const int expectedNumberOfLogs = 0;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);

            var logTypeThatUnsupportCurrentTestCase =
                (Enum.GetValues(typeof(UnityEngineLogLevel)) as IEnumerable<UnityEngineLogLevel>)
                .First(n => n != breadcrumbsManager.ConvertLogTypeToLogLevel(testedLevel));

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, logTypeThatUnsupportCurrentTestCase);

            var result = breadcrumbsManager.Log(message, testedLevel);

            Assert.IsFalse(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
        }

        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Assert)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Exception)]
        public void TestLogLevel_ShouldtFilterLogLevel_BreadcrumbIsAvailable(LogType testedLevel)
        {
            const string message = "message";
            const int expectedNumberOfLogs = 1;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var unityEngineLogLevel = breadcrumbsManager.ConvertLogTypeToLogLevel(testedLevel);
            var logTypeThatUnsupportCurrentTestCase =
                (Enum.GetValues(typeof(UnityEngineLogLevel)) as IEnumerable<UnityEngineLogLevel>)
                .First(n => n == unityEngineLogLevel);

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, logTypeThatUnsupportCurrentTestCase);
            var result = breadcrumbsManager.Log(message, testedLevel);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
            var breadcrumb = inMemoryBreadcrumbStorage.Breadcrumbs.ElementAt(0);
            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual(unityEngineLogLevel, breadcrumb.Level);
            Assert.AreEqual(BreadcrumbLevel.Manual, breadcrumb.Type);
            Assert.IsNull(breadcrumb.Attributes);
        }

        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Assert)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Exception)]
        public void TestLogLevel_ShouldtFilterLogLevelWithAttributes_BreadcrumbIsAvailable(LogType testedLevel)
        {
            const string message = "message";
            const string attributeName = "foo";
            const string attributeValue = "bar";
            const int expectedNumberOfLogs = 1;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var unityEngineLogLevel = breadcrumbsManager.ConvertLogTypeToLogLevel(testedLevel);
            var logTypeThatUnsupportCurrentTestCase =
                (Enum.GetValues(typeof(UnityEngineLogLevel)) as IEnumerable<UnityEngineLogLevel>)
                .First(n => n == unityEngineLogLevel);

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, logTypeThatUnsupportCurrentTestCase);
            var result = breadcrumbsManager.Log(message, testedLevel, new Dictionary<string, string>() { { attributeName, attributeValue } });

            Assert.IsTrue(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
            var breadcrumb = inMemoryBreadcrumbStorage.Breadcrumbs.ElementAt(0);
            Assert.IsTrue(breadcrumb.Attributes.ContainsKey(attributeName));
            Assert.AreEqual(attributeValue, breadcrumb.Attributes[attributeName]);
        }

        [Test]
        public void DebugLogLevel_ShouldFilterDebugLogLevel_BreadcrumbsWasntSave()
        {
            const string message = "message";
            const int expectedNumberOfLogs = 0;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var logTypeThatUnsupportCurrentTestCase = UnityEngineLogLevel.Error;
            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, logTypeThatUnsupportCurrentTestCase);

            var result = breadcrumbsManager.Debug(message);

            Assert.IsFalse(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
        }

        [Test]
        public void DebugLogLevel_ShouldntFilterDebugLogLevel_BreadcrumbIsAvailable()
        {
            const string message = "message";
            const int expectedNumberOfLogs = 1;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var supportedLogLevel = UnityEngineLogLevel.Debug;
            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, supportedLogLevel);

            var result = breadcrumbsManager.Debug(message);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
        }

        [Test]
        public void DebugLogLevel_ShouldntFilterDebugLogLevelWithAttributes_BreadcrumbIsAvailable()
        {
            const string message = "message";
            const string attributeName = "foo";
            const string attributeValue = "bar";
            const int expectedNumberOfLogs = 1;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var supportedLogLevel = UnityEngineLogLevel.Debug;

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, supportedLogLevel);
            var result = breadcrumbsManager.Debug(message, new Dictionary<string, string>() { { attributeName, attributeValue } });

            Assert.IsTrue(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
            var breadcrumbAttributes = inMemoryBreadcrumbStorage.Breadcrumbs.ElementAt(0).Attributes;
            Assert.IsTrue(breadcrumbAttributes.ContainsKey(attributeName));
            Assert.AreEqual(attributeValue, breadcrumbAttributes[attributeName]);
        }

        [Test]
        public void WarningLogLevel_ShouldFilterWarningLogLevel_BreadcrumbsWasntSave()
        {
            const string message = "message";
            const int expectedNumberOfLogs = 0;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var logTypeThatUnsupportCurrentTestCase = UnityEngineLogLevel.Error;

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, logTypeThatUnsupportCurrentTestCase);
            var result = breadcrumbsManager.Warning(message);

            Assert.IsFalse(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
        }

        [Test]
        public void WarningLogLevel_ShouldntFilterWarningLogLevel_BreadcrumbIsAvailable()
        {
            const string message = "message";
            const int expectedNumberOfLogs = 1;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage);
            var supportedLogLevel = UnityEngineLogLevel.Warning;
            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, supportedLogLevel);

            var result = breadcrumbsManager.Warning(message);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
        }
    }
}
