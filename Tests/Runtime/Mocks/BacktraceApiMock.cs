using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime
{
    internal class BacktraceApiMock : IBacktraceApi
    {
        public BacktraceData LastData;
        public Action<Exception> OnServerError { get; set; }
        public Action<BacktraceResult> OnServerResponse { get; set; }
        public Func<string, BacktraceData, BacktraceResult> RequestHandler { get; set; }

        public IEnumerator Send(BacktraceData data, Action<BacktraceResult> callback = null)
        {
            LastData = data;
            if (callback != null)
            {
                callback.Invoke(new BacktraceResult() { Status = Types.BacktraceResultStatus.Ok });
            }
            yield return null;
        }

        public IEnumerator Send(string json, List<string> attachments, int deduplication, Action<BacktraceResult> callback)
        {
            yield return null;
        }

        public IEnumerator SendMinidump(string minidumpPath, IEnumerable<string> attachments, Action<BacktraceResult> callback = null)
        {
            yield return null;
        }
    }
}
