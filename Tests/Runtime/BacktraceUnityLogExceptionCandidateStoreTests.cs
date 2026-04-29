using Backtrace.Unity.Model;
using NUnit.Framework;
using System;

namespace Backtrace.Unity.Tests.Runtime
{
    public sealed class BacktraceUnityLogExceptionCandidateStoreTests
    {
        [Test]
        public void CandidateStore_ShouldConsumeMatchingCandidateOnlyOnce()
        {
            var store = new BacktraceUnityLogExceptionCandidateStore();
            var exception = new ArgumentNullException("obj");
            Assert.True(store.Record(exception, "TestContext", true));

            BacktraceUnityLogExceptionCandidate candidate;
            Assert.True(store.TryConsume(
                "ArgumentNullException: " + exception.Message,
                out candidate));
            Assert.NotNull(candidate);

            BacktraceUnityLogExceptionCandidate secondCandidate;
            Assert.False(store.TryConsume(
                "ArgumentNullException: " + exception.Message,
                out secondCandidate));
            Assert.Null(secondCandidate);
        }

        [Test]
        public void CandidateStore_ShouldIgnoreNonMatchingMessage()
        {
            var store = new BacktraceUnityLogExceptionCandidateStore();
            var exception = new ArgumentNullException("obj");
            Assert.True(store.Record(exception, "TestContext", true));

            BacktraceUnityLogExceptionCandidate candidate;
            Assert.False(store.TryConsume(
                "InvalidOperationException: other",
                out candidate));
            Assert.Null(candidate);
        }
    }
}
