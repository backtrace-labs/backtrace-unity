#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.InMemory;
using Backtrace.Unity.Tests.Runtime.Native.Mocks;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Native
{
    public sealed class BreadcrumbsAnrTests
    {
        [Test]
        public void TestAnrBreadcrumbReporting_ShouldAddAnrBreadcrumb_BreadcrumbSuccessfullyStored()
        {
            const int expectedNumberOfBreadcrumbs = 1;
            var backtraceStorageManager = new BacktraceInMemoryLogManager();
            var breadcrumbs = new BacktraceBreadcrumbs(backtraceStorageManager, BacktraceBreadcrumbType.System, UnityEngineLogLevel.Warning);
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var nativeClient = new TestableNativeClient(configuration, breadcrumbs);

            nativeClient.SimulateAnr();
            nativeClient.Update(0);

            Assert.AreEqual(expectedNumberOfBreadcrumbs, breadcrumbs.LogManager.Length());
            Assert.AreEqual(TestableNativeClient.AnrMessage, backtraceStorageManager.Breadcrumbs.First().Message);
        }

        [Test]
        public void TestAnrBreadcrumbReporting_ShouldAddSingleAnrBreadcrumb_SingleBreadcrumbSuccessfullyStored()
        {
            const int expectedNumberOfBreadcrumbs = 1;
            var backtraceStorageManager = new BacktraceInMemoryLogManager();
            var breadcrumbs = new BacktraceBreadcrumbs(backtraceStorageManager, BacktraceBreadcrumbType.System, UnityEngineLogLevel.Warning);

            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var nativeClient = new TestableNativeClient(configuration, breadcrumbs);

            nativeClient.SimulateAnr();
            nativeClient.Update(0);
            nativeClient.Update(1);
            nativeClient.Update(2);

            Assert.AreEqual(expectedNumberOfBreadcrumbs, breadcrumbs.LogManager.Length());
            Assert.AreEqual(TestableNativeClient.AnrMessage, backtraceStorageManager.Breadcrumbs.First().Message);
        }

        [Test]
        public void TestAnrBreadcrumbReporting_ShouldAddSingleAnrBreadcrumbFromMultipleThreads_SingleBreadcrumbSuccessfullyStored()
        {
            const int numberOfThreads = 5;
            const int expectedNumberOfBreadcrumbs = 1;
            var backtraceStorageManager = new BacktraceInMemoryLogManager();
            var breadcrumbs = new BacktraceBreadcrumbs(backtraceStorageManager, BacktraceBreadcrumbType.System, UnityEngineLogLevel.Warning);

            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var nativeClient = new TestableNativeClient(configuration, breadcrumbs);

            nativeClient.SimulateAnr();
            var threads = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i] = new Thread(() =>
                {
                    nativeClient.Update(0);
                });
                threads[i].Start();
            }

            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i].Join();
            }
            Assert.AreEqual(expectedNumberOfBreadcrumbs, breadcrumbs.LogManager.Length());
            Assert.AreEqual(TestableNativeClient.AnrMessage, backtraceStorageManager.Breadcrumbs.First().Message);
        }

        [Test]
        public void TestAnrBreadcrumbReporting_ShouldIgnoreBreadcrumbsWithoutValidBreadcrumbType_NoBreadcrumbsStored()
        {
            const int expectedNumberOfBreadcrumbs = 0;
            var notSupportedAnrBreadcrumbLevel = BacktraceBreadcrumbType.Log;
            var backtraceStorageManager = new BacktraceInMemoryLogManager();
            var breadcrumbs = new BacktraceBreadcrumbs(backtraceStorageManager, notSupportedAnrBreadcrumbLevel, UnityEngineLogLevel.Warning);
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var nativeClient = new TestableNativeClient(configuration, breadcrumbs);

            nativeClient.SimulateAnr();
            nativeClient.Update(0);

            Assert.AreEqual(expectedNumberOfBreadcrumbs, breadcrumbs.LogManager.Length());
        }

        [Test]
        public void TestAnrBreadcrumbReporting_ShouldIgnoreBreadcrumbsWithoutValidUnityLogLevel_NoBreadcrumbsStored()
        {
            const int expectedNumberOfBreadcrumbs = 0;
            var notSupportedAnrUnityLogLevel = UnityEngineLogLevel.Debug;
            var backtraceStorageManager = new BacktraceInMemoryLogManager();
            var breadcrumbs = new BacktraceBreadcrumbs(backtraceStorageManager, BacktraceBreadcrumbType.System, notSupportedAnrUnityLogLevel);
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var nativeClient = new TestableNativeClient(configuration, breadcrumbs);

            nativeClient.SimulateAnr();
            nativeClient.Update(0);

            Assert.AreEqual(expectedNumberOfBreadcrumbs, breadcrumbs.LogManager.Length());
        }
    }
}
#endif