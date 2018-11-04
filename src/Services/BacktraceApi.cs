using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Newtonsoft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Backtrace.Unity.Services
{
    /// <summary>
    /// Backtrace Api class that allows to send a diagnostic data to server
    /// </summary>
    internal class BacktraceApi : IBacktraceApi
    {
        /// <summary>
        /// User custom request method
        /// </summary>
        public Func<string, string, BacktraceData, BacktraceResult> RequestHandler { get; set; } = null;

        /// <summary>
        /// Event triggered when server is unvailable
        /// </summary>
        public Action<Exception> OnServerError { get; set; } = null;

        /// <summary>
        /// Event triggered when server respond to diagnostic data
        /// </summary>
        public Action<BacktraceResult> OnServerResponse { get; set; }
        
        internal readonly ReportLimitWatcher reportLimitWatcher;

        /// <summary>
        /// Url to server
        /// </summary>
        private readonly string _serverurl;

        /// <summary>
        /// Create a new instance of Backtrace API
        /// </summary>
        /// <param name="credentials">API credentials</param>
        public BacktraceApi(BacktraceCredentials credentials, uint reportPerMin = 3)
        {
            if (credentials == null)
            {
                throw new ArgumentException($"{nameof(BacktraceCredentials)} cannot be null");
            }
            _serverurl = $"{credentials.BacktraceHostUri.AbsoluteUri}post?format=json&token={credentials.Token}";
            reportLimitWatcher = new ReportLimitWatcher(reportPerMin);
        }

        /// <summary>
        /// Sending a diagnostic report data to server API. 
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        /// <returns>Server response</returns>
        public IEnumerator Send(BacktraceData data, Action<BacktraceResult> callback = null)
        {
            //check rate limiting
            bool watcherValidation = reportLimitWatcher.WatchReport(data.Report);
            if (!watcherValidation)
            {
                yield return BacktraceResult.OnLimitReached(data.Report);
            }
            var json = BacktraceDataConverter.SerializeObject(data);
            yield return Send(json, data.Attachments, data.Report, callback);
        }

        private IEnumerator Send(string json, List<string> attachments, BacktraceReport report, Action<BacktraceResult> callback)
        {
            using (var request = new UnityWebRequest(_serverurl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                BacktraceResult result;
                if (request.responseCode == 200)
                {
                    result = new BacktraceResult();
                    OnServerResponse?.Invoke(result);
                }
                else
                {
                    PrintLog(request);
                    var exception = new Exception(request.error);
                    result = BacktraceResult.OnError(report, exception);
                    OnServerError?.Invoke(exception);
                }
                callback?.Invoke(result);
                yield return result;
                yield return HandleResult(request, report, callback);
            }
        }

        private IEnumerable HandleResult(UnityWebRequest request, BacktraceReport report, Action<BacktraceResult> callback)
        {
            BacktraceResult result;
            if (request.responseCode == 200)
            {
                result = new BacktraceResult();
                OnServerResponse?.Invoke(result);
            }
            else
            {
                PrintLog(request);
                var exception = new Exception(request.error);
                result = BacktraceResult.OnError(report, exception);
                OnServerError?.Invoke(exception);
            }
            callback?.Invoke(result);
            yield return result;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void PrintLog(UnityWebRequest request)
        {
            StringBuilder sb = new StringBuilder();
            var responseHeaders = request.GetResponseHeaders();
            if (responseHeaders != null)
            {
                foreach (KeyValuePair<string, string> dict in request.GetResponseHeaders())
                {
                    sb.Append(dict.Key).Append(": \t[").Append(dict.Value).Append("]\n");
                }
                Debug.Log("RESPONSE: " + sb.ToString());
            }
        }

        public void SetClientRateLimitEvent(Action<BacktraceReport> onClientReportLimitReached)
        {
            reportLimitWatcher.OnClientReportLimitReached = onClientReportLimitReached;
        }

        public void SetClientRateLimit(uint rateLimit)
        {
            reportLimitWatcher.SetClientReportLimit(rateLimit);
        }
    }
}