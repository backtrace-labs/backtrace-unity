using Backtrace.Unity.Json;
using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Backtrace.Unity.Tests.Runtime.Session.Mocks
{
    public sealed class BacktraceHttpClientMock : IBacktraceHttpClient
    {
        public int NumberOfRequests { get; set; } = 0;
        public bool Called { get; set; } = false;
        public Action<string, BacktraceJObject> OnIvoke { get; set; }
        public string Response { get; set; } = string.Empty;
        public long StatusCode { get; set; } = 200;
        public bool IsHttpError { get; set; } = false;

        public bool IgnoreSslValidation { get; set; } = false;

        public void Post(string submissionUrl, BacktraceJObject jObject, Action<long, bool, string> onComplete)
        {
            NumberOfRequests++;
            Called = true;
            OnIvoke?.Invoke(submissionUrl, jObject);
            onComplete?.Invoke(StatusCode, IsHttpError, Response);
        }

        public UnityWebRequest Post(string submissionUrl, string json, IEnumerable<string> attachments)
        {
            return new UnityWebRequest();
        }

        public UnityWebRequest Post(string submissionUrl, byte[] minidump, IEnumerable<string> attachments)
        {
            return new UnityWebRequest();
        }
    }
}
