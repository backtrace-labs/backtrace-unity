using Backtrace.Unity.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Backtrace.Unity.Model
{
    internal sealed class BacktraceUnityLogExceptionCandidateStore
    {
        private const int MaxCandidates = 32;
        private const double CandidateTtlMs = 2000;

        private readonly object _lock = new object();
        private readonly Queue<BacktraceUnityLogExceptionCandidate> _candidates =
            new Queue<BacktraceUnityLogExceptionCandidate>();

        internal bool Record(
            Exception exception,
            string contextName,
            bool isMainThread)
        {
            if (exception == null)
            {
                return false;
            }
            var prefixes = BacktraceUnityLogCapture.CreateExceptionMessagePrefixes(exception);
            if (prefixes == null || prefixes.Count == 0)
            {
                return false;
            }
            lock (_lock)
            {
                PruneExpired(DateTimeHelper.TimestampMs());
                while (_candidates.Count >= MaxCandidates)
                {
                    _candidates.Dequeue();
                }
                _candidates.Enqueue(new BacktraceUnityLogExceptionCandidate
                {
                    Exception = exception,
                    ContextName = contextName ?? string.Empty,
                    MessagePrefixes = prefixes,
                    IsMainThread = isMainThread,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    ExpiresAtMs = DateTimeHelper.TimestampMs() + CandidateTtlMs
                });
            }
            return true;
        }

        internal bool TryConsume(
            string unityMessage,
            out BacktraceUnityLogExceptionCandidate candidate)
        {
            candidate = null;
            if (string.IsNullOrEmpty(unityMessage))
            {
                return false;
            }
            lock (_lock)
            {
                PruneExpired(DateTimeHelper.TimestampMs());
                if (_candidates.Count == 0)
                {
                    return false;
                }
                var count = _candidates.Count;
                for (var i = 0; i < count; i++)
                {
                    var current = _candidates.Dequeue();
                    if (candidate == null &&
                        MatchesAnyPrefix(unityMessage, current.MessagePrefixes))
                    {
                        candidate = current;
                        continue;
                    }
                    _candidates.Enqueue(current);
                }
                return candidate != null;
            }
        }

        internal void Clear()
        {
            lock (_lock)
            {
                _candidates.Clear();
            }
        }

        private void PruneExpired(double nowMs)
        {
            while (_candidates.Count > 0 &&
                   _candidates.Peek().ExpiresAtMs < nowMs)
            {
                _candidates.Dequeue();
            }
        }

        private static bool MatchesAnyPrefix(
            string message,
            IList<string> prefixes)
        {
            if (string.IsNullOrEmpty(message) || prefixes == null)
            {
                return false;
            }
            for (var i = 0; i < prefixes.Count; i++)
            {
                var prefix = prefixes[i];
                if (!string.IsNullOrEmpty(prefix) &&
                    message.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
