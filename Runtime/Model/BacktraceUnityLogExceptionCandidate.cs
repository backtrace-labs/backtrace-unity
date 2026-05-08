using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Model
{
    internal sealed class BacktraceUnityLogExceptionCandidate
    {
        internal Exception Exception;
        internal string ContextName;
        internal List<string> MessagePrefixes;
        internal bool IsMainThread;
        internal int ThreadId;
        internal double ExpiresAtMs;
    }
}
