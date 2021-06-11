using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Backtrace.Unity.Model
{
    internal interface IBacktraceHttpClient
    {
        bool IgnoreSslValidation { get; set; }
        void Post(string submissionUrl, BacktraceJObject jObject, Action<long, bool, string> onComplete);
        UnityWebRequest Post(string submissionUrl, string json, IEnumerable<string> attachments);
        UnityWebRequest Post(string submissionUrl, byte[] minidump, IEnumerable<string> attachments);
    }
}
