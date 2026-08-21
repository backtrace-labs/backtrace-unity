#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class AndroidNativeInitializationTests
    {
        [Test]
        public void RejectedBeforeNativeBridgeDoesNotRollback()
        {
            int rollbackCount = 0;

            bool initialized = AndroidNativeInitialization.Execute(
                markCompleted => false,
                () => Assert.Fail("Completion must not run"),
                () => rollbackCount++,
                null);

            Assert.IsFalse(initialized);
            Assert.AreEqual(0, rollbackCount);
        }

        [Test]
        public void NativeBridgeFalseResultRollsBackPartialState()
        {
            int completionCount = 0;
            int rollbackCount = 0;

            bool initialized = AndroidNativeInitialization.Execute(
                markCompleted =>
                {
                    markCompleted();
                    return false;
                },
                () => completionCount++,
                () => rollbackCount++,
                null);

            Assert.IsFalse(initialized);
            Assert.AreEqual(0, completionCount);
            Assert.AreEqual(1, rollbackCount);
        }

        [Test]
        public void NativeBridgeFalseResultRollbackFailureIsContained()
        {
            var rollbackFailure = new InvalidOperationException("sensitive rollback detail");
            Exception reported = null;

            Assert.DoesNotThrow(() =>
            {
                bool initialized = AndroidNativeInitialization.Execute(
                    markCompleted =>
                    {
                        markCompleted();
                        return false;
                    },
                    () => Assert.Fail("Completion must not run"),
                    () => { throw rollbackFailure; },
                    failure => reported = failure);

                Assert.IsFalse(initialized);
            });

            Assert.AreSame(rollbackFailure, reported);
        }

        [Test]
        public void AttributeSetupFailureRollsBackActiveBackend()
        {
            var setupFailure = new InvalidOperationException("attribute setup failed");
            int rollbackCount = 0;

            var thrown = Assert.Throws<InvalidOperationException>(() => AndroidNativeInitialization.Execute(
                markActive =>
                {
                    markActive();
                    return true;
                },
                () => { throw setupFailure; },
                () => rollbackCount++,
                null));

            Assert.AreSame(setupFailure, thrown);
            Assert.AreEqual(1, rollbackCount);
        }

        [Test]
        public void CleanupFailureAfterActivationRollsBackBackend()
        {
            var cleanupFailure = new InvalidOperationException("JNI cleanup failed");
            int completionCount = 0;
            int rollbackCount = 0;

            var thrown = Assert.Throws<InvalidOperationException>(() => AndroidNativeInitialization.Execute(
                markActive =>
                {
                    // Mirrors InvokeInitialize: native code returned true, then its finally
                    // block failed while deleting a local reference.
                    markActive();
                    throw cleanupFailure;
                },
                () => completionCount++,
                () => rollbackCount++,
                null));

            Assert.AreSame(cleanupFailure, thrown);
            Assert.AreEqual(0, completionCount);
            Assert.AreEqual(1, rollbackCount);
        }

        [Test]
        public void RollbackFailureDoesNotReplaceSetupFailure()
        {
            var setupFailure = new InvalidOperationException("attribute setup failed");
            var rollbackFailure = new ApplicationException("rollback failed");
            Exception reportedFailure = null;

            var thrown = Assert.Throws<InvalidOperationException>(() => AndroidNativeInitialization.Execute(
                markActive => true,
                () => { throw setupFailure; },
                () => { throw rollbackFailure; },
                exception => reportedFailure = exception));

            Assert.AreSame(setupFailure, thrown);
            Assert.AreSame(rollbackFailure, reportedFailure);
        }

        [Test]
        public void LocalReferenceFailureDoesNotSkipRemainingReferences()
        {
            var firstReference = new IntPtr(1);
            var secondReference = new IntPtr(2);
            var cleanupFailure = new InvalidOperationException("first cleanup failed");
            var attempted = new List<IntPtr>();

            var thrown = Assert.Throws<InvalidOperationException>(() =>
                AndroidNativeInitialization.DeleteLocalReferences(
                    reference =>
                    {
                        attempted.Add(reference);
                        if (reference == firstReference)
                        {
                            throw cleanupFailure;
                        }
                    },
                    IntPtr.Zero,
                    firstReference,
                    secondReference));

            Assert.AreSame(cleanupFailure, thrown);
            CollectionAssert.AreEqual(new[] { firstReference, secondReference }, attempted);
        }
    }
}
#endif
