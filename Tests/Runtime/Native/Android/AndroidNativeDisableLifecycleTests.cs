#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class AndroidNativeDisableLifecycleTests
    {
        [Test]
        public void EveryCleanupStageRunsInOrderWhenEarlierStagesFail()
        {
            var stages = new List<string>();
            var warnings = new List<string>();

            Assert.DoesNotThrow(() => AndroidNativeDisableLifecycle.Run(
                () =>
                {
                    stages.Add("anr-thread");
                    throw new InvalidOperationException("sensitive anr thread detail");
                },
                () =>
                {
                    stages.Add("native");
                    throw new InvalidOperationException("sensitive native detail");
                },
                () =>
                {
                    stages.Add("anr-stop");
                    throw new InvalidOperationException("sensitive anr stop detail");
                },
                () =>
                {
                    stages.Add("anr-dispose");
                    throw new InvalidOperationException("sensitive anr dispose detail");
                },
                () =>
                {
                    stages.Add("unhandled-stop");
                    throw new InvalidOperationException("sensitive unhandled stop detail");
                },
                () =>
                {
                    stages.Add("unhandled-dispose");
                    throw new InvalidOperationException("sensitive unhandled dispose detail");
                },
                warnings.Add));

            CollectionAssert.AreEqual(
                new[]
                {
                    "anr-thread",
                    "native",
                    "anr-stop",
                    "anr-dispose",
                    "unhandled-stop",
                    "unhandled-dispose"
                },
                stages);
            CollectionAssert.AreEqual(
                new[]
                {
                    "BT_UNITY_ANDROID_ANR_STOP_FAILURE: Failure type: System.InvalidOperationException",
                    "BT_UNITY_ANDROID_NATIVE_DISABLE_FAILURE: Failure type: System.InvalidOperationException",
                    "BT_UNITY_ANDROID_ANR_WATCHER_STOP_FAILURE: Failure type: System.InvalidOperationException",
                    "BT_UNITY_ANDROID_ANR_WATCHER_DISPOSE_FAILURE: Failure type: System.InvalidOperationException",
                    "BT_UNITY_ANDROID_UNHANDLED_WATCHER_STOP_FAILURE: Failure type: System.InvalidOperationException",
                    "BT_UNITY_ANDROID_UNHANDLED_WATCHER_DISPOSE_FAILURE: Failure type: System.InvalidOperationException"
                },
                warnings);

            foreach (var warning in warnings)
            {
                StringAssert.DoesNotContain("sensitive", warning);
            }
        }

        [Test]
        public void MissingOptionalCleanupStagesDoNotSkipAnrThreadStop()
        {
            int stopAnrCalls = 0;

            Assert.DoesNotThrow(() => AndroidNativeDisableLifecycle.Run(
                () => { stopAnrCalls++; },
                null,
                null,
                null,
                null,
                null,
                null));

            Assert.AreEqual(1, stopAnrCalls);
        }

        [Test]
        public void LoggingFailureCannotInterruptRemainingCleanup()
        {
            int cleanupCalls = 0;

            Assert.DoesNotThrow(() => AndroidNativeDisableLifecycle.Run(
                () => { throw new InvalidOperationException(); },
                () => { cleanupCalls++; },
                () => { cleanupCalls++; },
                () => { cleanupCalls++; },
                () => { cleanupCalls++; },
                () => { cleanupCalls++; },
                warning => { throw new InvalidOperationException(); }));

            Assert.AreEqual(5, cleanupCalls);
        }
    }
}
#endif
