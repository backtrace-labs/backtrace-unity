using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Backtrace.Unity.Model.Metrics.Mocks
{
    public sealed class BacktraceHttpClientMock : IBacktraceHttpClient
    {
        public int NumberOfRequests { get; set; }
        public bool Called { get; set; }
        public Action<string, BacktraceJObject> OnInvoke { get; set; }
        public string Response { get; set; }
        public long StatusCode { get; set; }
        public bool IsHttpError { get; set; }

        public bool IgnoreSslValidation { get; set; }

        public string BaseUrl { get; set; }
        public BacktraceHttpClientMock()
        {
            NumberOfRequests = 0;
            StatusCode = 200;

        }

        public void Post(string submissionUrl, BacktraceJObject jObject, Action<long, bool, string> onComplete)
        {
            NumberOfRequests++;
            Called = true;
            if (OnInvoke != null)
            {
                OnInvoke.Invoke(submissionUrl, jObject);
            }
            if (onComplete != null)
            {
                onComplete.Invoke(StatusCode, IsHttpError, Response);
            }
        }

        public UnityWebRequest Post(string submissionUrl, string json, IEnumerable<string> attachments, IDictionary<string, string> attributes)
        {
            return new UnityWebRequest();
        }

        public UnityWebRequest Post(string submissionUrl, byte[] minidump, IEnumerable<string> attachments, IDictionary<string, string> attributes)
        {
            return new UnityWebRequest();
        }
    }
}
