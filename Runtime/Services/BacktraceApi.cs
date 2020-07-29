using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;

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
        [Obsolete("RequestHandler is obsolete. BacktraceApi won't be able to provide BacktraceData in every situation")]
        public Func<string, BacktraceData, BacktraceResult> RequestHandler { get; set; }

        /// <summary>
        /// Event triggered when server is unvailable
        /// </summary>
        public Action<Exception> OnServerError { get; set; }

        /// <summary>
        /// Event triggered when server respond to diagnostic data
        /// </summary>
        public Action<BacktraceResult> OnServerResponse { get; set; }


        /// <summary>
        /// Url to server
        /// </summary>
        private readonly Uri _serverurl;

        /// <summary>
        /// Enable performance statistics
        /// </summary>
        public bool EnablePerformanceStatistics { get; set; } = false;

        /// <summary>
        /// Url to server
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return _serverurl.ToString();
            }
        }


        private readonly BacktraceCredentials _credentials;



        private readonly bool _ignoreSslValidation;
        /// <summary>
        /// Create a new instance of Backtrace API
        /// </summary>
        /// <param name="credentials">API credentials</param>
        public BacktraceApi(
            BacktraceCredentials credentials, 
            bool ignoreSslValidation = false)
        {
            _credentials = credentials;
            if (_credentials == null)
            {
                throw new ArgumentException(string.Format("{0} cannot be null", "BacktraceCredentials"));
            }

            _ignoreSslValidation = ignoreSslValidation;
            _serverurl = credentials.GetSubmissionUrl();
        }

        /// <summary>
        /// Send minidump to Backtrace
        /// </summary>
        /// <param name="minidumpPath">Path to minidump</param>
        /// <param name="attachments">List of attachments</param>
        /// <param name="callback">Callback</param>
        /// <returns>Server response</returns>
        public IEnumerator SendMinidump(string minidumpPath, IEnumerable<string> attachments, Action<BacktraceResult> callback = null)
        {
            if (attachments == null)
            {
                attachments = new List<string>();
            }

            var stopWatch = EnablePerformanceStatistics
               ? System.Diagnostics.Stopwatch.StartNew()
               : new System.Diagnostics.Stopwatch();

            var jsonServerUrl = ServerUrl;
            var minidumpServerUrl = jsonServerUrl.IndexOf("submit.backtrace.io") != -1
                ? jsonServerUrl.Replace("/json", "/minidump")
                : jsonServerUrl.Replace("format=json", "format=minidump");

            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("upload_file", File.ReadAllBytes(minidumpPath))
            };

            foreach (var file in attachments)
            {
                if (File.Exists(file) && new FileInfo(file).Length > 10000000)
                {
                    formData.Add(new MultipartFormFileSection(
                        string.Format("attachment__{0}", Path.GetFileName(file)),
                        File.ReadAllBytes(file)));
                }
            }

            yield return new WaitForEndOfFrame();

            var boundaryId = string.Format("----------{0:N}", Guid.NewGuid());
            var boundaryIdBytes = Encoding.ASCII.GetBytes(boundaryId);

            using (var request = UnityWebRequest.Post(minidumpServerUrl, formData, boundaryIdBytes))
            {
                request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundaryId);
                request.timeout = 15000;
                yield return request.SendWebRequest();
                var result = request.isNetworkError || request.isHttpError
                    ? new BacktraceResult()
                    {
                        Message = request.error,
                        Status = Types.BacktraceResultStatus.ServerError
                    }
                    : BacktraceResult.FromJson(request.downloadHandler.text);

                if (callback != null)
                {
                    callback.Invoke(result);
                }
                if (EnablePerformanceStatistics)
                {
                    stopWatch.Stop();
                    Debug.Log(string.Format("Backtrace - minidump send time: {0}ms", MetricsHelper.GetPerformanceInfo(stopWatch)));
                }

                yield return result;
            }
        }

        /// <summary>
        /// Sending a diagnostic report data to server API. 
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        /// <returns>Server response</returns>
        public IEnumerator Send(BacktraceData data, Action<BacktraceResult> callback = null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (RequestHandler != null)
            {
                yield return RequestHandler.Invoke(ServerUrl, data);
            }
            else if (data != null)
            {
                var json = data.ToJson();
                yield return Send(json, data.Attachments, data.Deduplication, callback);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Sending diagnostic report to Backtrace
        /// </summary>
        /// <param name="json">diagnostic data JSON</param>
        /// <param name="attachments">List of report attachments</param>
        /// <param name="deduplication">Deduplication count</param>
        /// <param name="callback">coroutine callback</param>
        /// <returns>Server response</returns>
        public IEnumerator Send(string json, List<string> attachments, int deduplication, Action<BacktraceResult> callback)
        {
            var queryAttributes = new Dictionary<string, string>();
            if (deduplication > 0)
            {
                queryAttributes["_mod_duplicate"] = deduplication.ToString();
            }
            yield return Send(json, attachments, queryAttributes, callback);

        }

        /// <summary>
        /// Sending diagnostic report to Backtrace
        /// </summary>
        /// <param name="json">diagnostic data JSON</param>
        /// <param name="attachments">List of report attachments</param>
        /// <param name="queryAttributes">Query string attributes</param>
        /// <param name="callback">coroutine callback</param>
        /// <returns>Server response</returns>
        public IEnumerator Send(string json, List<string> attachments, Dictionary<string, string> queryAttributes, Action<BacktraceResult> callback)
        {
            var stopWatch = EnablePerformanceStatistics
              ? System.Diagnostics.Stopwatch.StartNew()
              : new System.Diagnostics.Stopwatch();

            var requestUrl = queryAttributes != null
                ? GetParametrizedQuery(queryAttributes)
                : ServerUrl;


            using (var request = new UnityWebRequest(requestUrl, "POST"))
            {
#if UNITY_2018_4_OR_NEWER
                if (_ignoreSslValidation)
                {
                    request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                }
#endif
                request.timeout = 15000;
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                BacktraceResult result;
                if (request.responseCode == 200)
                {
                    result = BacktraceResult.FromJson(request.downloadHandler.text);

                    if (OnServerResponse != null)
                    {
                        OnServerResponse.Invoke(result);
                    }
                    if (attachments != null && attachments.Count > 0)
                    {
                        var stack = new Stack<string>(attachments);
                        yield return SendAttachment(result.RxId, stack);
                    }
                }
                else
                {
                    PrintLog(request);
                    var exception = new Exception(request.error);
                    result = BacktraceResult.OnError(exception);
                    if (OnServerError != null)
                    {
                        OnServerError.Invoke(exception);
                    }
                }

                if (callback != null)
                {
                    callback.Invoke(result);
                }

                if (EnablePerformanceStatistics)
                {
                    stopWatch.Stop();
                    Debug.Log(string.Format("Backtrace - JSON send time: {0}ms", MetricsHelper.GetPerformanceInfo(stopWatch)));
                }
                yield return result;
            }
        }

        private void PrintLog(UnityWebRequest request)
        {
            string responseText = Encoding.UTF8.GetString(request.downloadHandler.data);
            Debug.LogWarning(string.Format("{0}{1}", string.Format("[Backtrace]::Reponse code: {0}, Response text: {1}",
                    request.responseCode,
                    responseText),
                "\n Please check provided url to Backtrace service or learn more from our integration guide: https://help.backtrace.io/integration-guides/game-engines/unity-integration-guide"));
        }

        private IEnumerator SendAttachment(string rxId, Stack<string> attachments)
        {
            if (attachments != null && attachments.Count > 0)
            {
                var attachment = attachments.Pop();
                if (File.Exists(attachment))
                {
                    string fileName = Path.GetFileName(attachment);
                    string serverUrl = GetAttachmentUploadUrl(rxId, fileName);
                    using (var request = new UnityWebRequest(serverUrl, "POST"))
                    {

#if UNITY_2018_4_OR_NEWER
                        if (_ignoreSslValidation)
                        {
                            request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                        }
#endif
                        request.timeout = 45000;
                        byte[] bodyRaw = File.ReadAllBytes(attachment);
                        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        yield return request.SendWebRequest();

                        if (request.responseCode != 200)
                        {
                            PrintLog(request);
                        }
                    }
                }
                yield return SendAttachment(rxId, attachments);
            }
        }

        private string GetAttachmentUploadUrl(string rxId, string attachmentName)
        {
            return string.IsNullOrEmpty(_credentials.Token)
                ? string.Format("{0}&object={1}&attachment_name={2}", _credentials.BacktraceHostUri.AbsoluteUri, rxId,
                    UrlEncode(attachmentName))
                : string.Format("{0}/api/post?token={1}&object={2}&attachment_name={3}",
                    _credentials.BacktraceHostUri.AbsoluteUri, _credentials.Token, rxId, UrlEncode(attachmentName));

        }

        private static readonly string reservedCharacters = "!*'();:@&=+$,/?%#[]";

        private string GetParametrizedQuery(Dictionary<string, string> queryAttributes)
        {
            var uriBuilder = new UriBuilder(_serverurl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var queryAttribute in queryAttributes)
            {
                query[queryAttribute.Key] = UrlEncode(queryAttribute.Value);
            }
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri.ToString();
        }

        private static string UrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (char @char in value)
            {
                if (reservedCharacters.IndexOf(@char) == -1)
                {
                    sb.Append(@char);
                }
                else
                {
                    sb.AppendFormat("%{0:X2}", (int)@char);
                }
            }
            return sb.ToString();
        }
    }
}