#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class AndroidAnrThreadLifecycleTests
    {
        [Test]
        public void AttachExceptionIsContainedAndSanitized()
        {
            var warnings = new List<string>();
            bool watchdogRan = false;
            int detachCalls = 0;

            Assert.DoesNotThrow(() => AndroidAnrThreadLifecycle.Run(
                () => { throw new InvalidOperationException("sensitive attach detail"); },
                tryAttach => { watchdogRan = true; },
                () => { detachCalls++; },
                warnings.Add));

            Assert.IsFalse(watchdogRan);
            Assert.AreEqual(0, detachCalls);
            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_ANR_THREAD_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
            StringAssert.DoesNotContain("sensitive attach detail", warnings[0]);
        }

        [Test]
        public void NonzeroAttachResultIsRetriedAndEventuallyDetached()
        {
            int attachCalls = 0;
            int detachCalls = 0;
            bool retrySucceeded = false;
            var warnings = new List<string>();

            AndroidAnrThreadLifecycle.Run(
                () => ++attachCalls == 1 ? -1 : 0,
                tryAttach => { retrySucceeded = tryAttach(); },
                () => { detachCalls++; },
                warnings.Add);

            Assert.AreEqual(2, attachCalls);
            Assert.IsTrue(retrySucceeded);
            Assert.AreEqual(1, detachCalls);
            Assert.IsEmpty(warnings);
        }

        [Test]
        public void DetachExceptionIsContainedAndSanitized()
        {
            var warnings = new List<string>();

            Assert.DoesNotThrow(() => AndroidAnrThreadLifecycle.Run(
                () => 0,
                tryAttach => { },
                () => { throw new InvalidOperationException("sensitive detach detail"); },
                warnings.Add));

            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_JNI_DETACH_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
            StringAssert.DoesNotContain("sensitive detach detail", warnings[0]);
        }

        [Test]
        public void HangTypeWriteFailureIsClassifiedAndStillRestoresCrashType()
        {
            var warnings = new List<string>();
            string errorType = "Crash";
            bool dumpSent = false;

            AndroidAnrThreadLifecycle.CaptureNativeDump(
                () =>
                {
                    // Mirrors SetNativeAttribute: the native write completed, then JNI
                    // local-reference cleanup failed before the method returned.
                    errorType = "Hang";
                    throw new InvalidOperationException("sensitive cleanup detail");
                },
                () => { dumpSent = true; },
                () => { errorType = "Crash"; },
                warnings.Add);

            Assert.AreEqual("Crash", errorType);
            Assert.IsFalse(dumpSent);
            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
            StringAssert.DoesNotContain("sensitive cleanup detail", warnings[0]);
        }

        [Test]
        public void NativeDumpFailureStillRestoresCrashType()
        {
            var warnings = new List<string>();
            string errorType = "Crash";

            AndroidAnrThreadLifecycle.CaptureNativeDump(
                () => { errorType = "Hang"; },
                () => { throw new InvalidOperationException("sensitive dump detail"); },
                () => { errorType = "Crash"; },
                warnings.Add);

            Assert.AreEqual("Crash", errorType);
            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_NATIVE_DUMP_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
            StringAssert.DoesNotContain("sensitive dump detail", warnings[0]);
        }

        [Test]
        public void CrashTypeRestorationFailureIsContainedAndSanitized()
        {
            var warnings = new List<string>();

            Assert.DoesNotThrow(() => AndroidAnrThreadLifecycle.CaptureNativeDump(
                () => { },
                () => { },
                () => { throw new InvalidOperationException("sensitive restore detail"); },
                warnings.Add));

            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
            StringAssert.DoesNotContain("sensitive restore detail", warnings[0]);
        }
    }
}
#endif
