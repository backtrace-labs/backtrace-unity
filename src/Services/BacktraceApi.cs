using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        public Func<string, BacktraceData, BacktraceResult> RequestHandler { get; set; } = null;

        /// <summary>
        /// Event triggered when server is unvailable
        /// </summary>
        public Action<Exception> OnServerError { get; set; } = null;

        /// <summary>
        /// Event triggered when server respond to diagnostic data
        /// </summary>
        public Action<BacktraceResult> OnServerResponse { get; set; }


        /// <summary>
        /// Url to server
        /// </summary>
        private readonly Uri _serverurl;

        private readonly bool _ignoreSslValidation;

        private readonly BacktraceCredentials _credentials;
        /// <summary>
        /// Create a new instance of Backtrace API
        /// </summary>
        /// <param name="credentials">API credentials</param>
        public BacktraceApi(BacktraceCredentials credentials, bool ignoreSslValidation = false)
        {
            _credentials = credentials ?? throw new ArgumentException($"{nameof(BacktraceCredentials)} cannot be null");
            _ignoreSslValidation = ignoreSslValidation;
            _serverurl = credentials.GetSubmissionUrl();
        }

        /// <summary>
        /// Sending a diagnostic report data to server API. 
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        /// <returns>Server response</returns>
        public IEnumerator Send(BacktraceData data, Action<BacktraceResult> callback = null)
        {
            if (data == null)
            {
                yield return new BacktraceResult()
                {
                    Status = Types.BacktraceResultStatus.LimitReached
                };
            }
            if (RequestHandler != null)
            {
                yield return RequestHandler.Invoke(_serverurl.ToString(), data);
            }
            else
            {
                string json = data.ToJson();
                yield return Send(json, data.Attachments, data.Report, data.Deduplication, callback);
            }
        }

        private IEnumerator Send(string json, List<string> attachments, BacktraceReport report, int deduplication, Action<BacktraceResult> callback)
        {
            var requestUrl = _serverurl.ToString();
            if (deduplication > 0)
            {
                var startingChar = string.IsNullOrEmpty(_serverurl.Query) ? "?" : "&";
                requestUrl += $"{startingChar}_mod_duplicate={deduplication}";
            }
            using (var request = new UnityWebRequest(requestUrl, "POST"))
            {
                if (_ignoreSslValidation)
                {
                    request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                }
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
                    var response = BacktraceResult.FromJson(request.downloadHandler.text);
                    if (attachments != null && attachments.Count > 0)
                    {
                        var stack = new Stack<string>(attachments);
                        yield return SendAttachment(response.RxId, stack);
                    }
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
        }

        private void PrintLog(UnityWebRequest request)
        {
            string responseText = Encoding.UTF8.GetString(request.downloadHandler.data);
            Debug.LogError($"[Backtrace]::Reponse code: {request.responseCode}, Response text: {responseText}" +
                $"\n Please check provided url to Backtrace service or learn more from our integration guide: https://help.backtrace.io/integration-guides/game-engines/unity-integration-guide");
        }

        private IEnumerator SendAttachment(string rxId, Stack<string> attachments)
        {
            if (attachments != null && attachments.Count > 0)
            {
                var attachment = attachments.Pop();
                if (System.IO.File.Exists(attachment))
                {
                    string fileName = System.IO.Path.GetFileName(attachment);
                    string serverUrl = GetAttachmentUploadUrl(rxId, fileName);
                    using (var request = new UnityWebRequest(serverUrl, "POST"))
                    {
                        if (_ignoreSslValidation)
                        {
                            request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                        }
                        byte[] bodyRaw = System.IO.File.ReadAllBytes(attachment);
                        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        yield return request.SendWebRequest();

                        if (request.responseCode == 200)
                        {
                            PrintLog(request);
                        }
                        else
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
            return _credentials == null || string.IsNullOrEmpty(_credentials.Token)
                ? $"{_credentials.BacktraceHostUri.AbsoluteUri}?object={rxId}&attachment_name={UrlEncode(attachmentName)}"
                : $"{_credentials.BacktraceHostUri.AbsoluteUri}/api/post?token={_credentials.Token}&object={rxId}&attachment_name={UrlEncode(attachmentName)}";

        }

        private static readonly string reservedCharacters = "!*'();:@&=+$,/?%#[]";

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