using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.InMemory;
using NUnit.Framework;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Breadcrumbs
{
    public class BacktraceBreadcrumbsTypeTests
    {

        [Test]
        public void TestManualLogWithLogLevel_ShouldSuccessfullyAddLog_LogIsStored()
        {
            const string message = "message";
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            //anything else than Manual
            BreadcrumbLevel breadcrumbLevel = BreadcrumbLevel.System;
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal | UnityEngineLogLevel.Info | UnityEngineLogLevel.Warning;
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage, BacktraceBreadcrumbType.System, level);
            breadcrumbsManager.EnableBreadcrumbs();
            var result = breadcrumbsManager.Log(message, breadcrumbLevel, LogType.Log, null);
            Assert.IsTrue(result);
        }


        [Test]
        public void TestManualLogWithLogLevel_ShouldDropUnwantedBreadcrumbLevel_ReturnFalse()
        {
            const string message = "message";
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            //anything else than Manual
            BreadcrumbLevel breadcrumbLevel = BreadcrumbLevel.Configuration;
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal | UnityEngineLogLevel.Info | UnityEngineLogLevel.Warning;
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage, BacktraceBreadcrumbType.User, level);
            breadcrumbsManager.EnableBreadcrumbs();
            var result = breadcrumbsManager.Log(message, breadcrumbLevel, LogType.Log, null);
            Assert.IsFalse(result);

        }
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Assert)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Exception)]
        public void TestManualLogs_ShouldFilterAllManualLogs_BreadcrumbsWasntSaved(LogType testedLevel)
        {
            const string message = "message";
            const int expectedNumberOfLogs = 0;
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            //anything else than Manual
            var breadcrumbType = BacktraceBreadcrumbType.Configuration;
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal | UnityEngineLogLevel.Info | UnityEngineLogLevel.Warning;
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage, breadcrumbType, level);

            breadcrumbsManager.EnableBreadcrumbs();
            var result = breadcrumbsManager.Log(message, testedLevel);

            Assert.IsFalse(result);
            Assert.AreEqual(expectedNumberOfLogs, inMemoryBreadcrumbStorage.Breadcrumbs.Count);
        }

        [Test]
        public void TestBreadcrumbsInitializationForInvalidLogLevel_ShouldReturnFalse_BreadcrumbsConfigurationIsInvalid()
        {
            // level not set - test simulates Unity Editor behavior 
            UnityEngineLogLevel level = UnityEngineLogLevel.None;
            // any defined type
            BacktraceBreadcrumbType backtraceBreadcrumbType = BacktraceBreadcrumbType.Log;

            var result = BacktraceBreadcrumbs.CanStoreBreadcrumbs(level, backtraceBreadcrumbType);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestBreadcrumbsInitializationForValidOptions_ShouldReturnTrue_BreadcrumbsConfigurationIsValid()
        {
            // any log level that might be selected by user
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug;
            BacktraceBreadcrumbType backtraceBreadcrumbType = BacktraceBreadcrumbType.Log;

            var result = BacktraceBreadcrumbs.CanStoreBreadcrumbs(level, backtraceBreadcrumbType);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestSystemLogs_ShouldEnableThem_EventsAreSet()
        {
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal | UnityEngineLogLevel.Info | UnityEngineLogLevel.Warning;
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage, BacktraceBreadcrumbType.System, level);

            breadcrumbsManager.EnableBreadcrumbs();

            Assert.IsTrue(breadcrumbsManager.EventHandler.HasRegisteredEvents);
            breadcrumbsManager.UnregisterEvents();
        }

        [Test]
        public void TestNavigationLogs_ShouldEnableThem_EventsAreSet()
        {
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal | UnityEngineLogLevel.Info | UnityEngineLogLevel.Warning;
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage, BacktraceBreadcrumbType.Navigation, level);

            breadcrumbsManager.EnableBreadcrumbs();

            Assert.IsTrue(breadcrumbsManager.EventHandler.HasRegisteredEvents);
            breadcrumbsManager.UnregisterEvents();
        }

        [Test]
        public void TestLogLogs_ShouldEnableThem_EventsAreSet()
        {
            var inMemoryBreadcrumbStorage = new BacktraceInMemoryLogManager();
            UnityEngineLogLevel level = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal | UnityEngineLogLevel.Info | UnityEngineLogLevel.Warning;
            var breadcrumbsManager = new BacktraceBreadcrumbs(inMemoryBreadcrumbStorage, BacktraceBreadcrumbType.Log, level);

            breadcrumbsManager.EnableBreadcrumbs();

            Assert.IsTrue(breadcrumbsManager.EventHandler.HasRegisteredEvents);
            breadcrumbsManager.UnregisterEvents();
        }

    }
}
