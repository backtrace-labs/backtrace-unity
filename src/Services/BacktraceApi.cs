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


        private readonly BacktraceCredentials _credentials;


#if UNITY_2018_4_OR_NEWER

        private readonly bool _ignoreSslValidation;
        /// <summary>
        /// Create a new instance of Backtrace API
        /// </summary>
        /// <param name="credentials">API credentials</param>
        public BacktraceApi(BacktraceCredentials credentials, bool ignoreSslValidation = false)
        {
            _credentials = credentials;
            if (_credentials == null)
            {
                throw new ArgumentException(string.Format("{0} cannot be null", "BacktraceCredentials"));
            }

            _ignoreSslValidation = ignoreSslValidation;
            _serverurl = credentials.GetSubmissionUrl();
        }
#else
        /// <summary>
        /// Create a new instance of Backtrace API
        /// </summary>
        /// <param name="credentials">API credentials</param>
        public BacktraceApi(BacktraceCredentials credentials)
        {
            _credentials = credentials;
            if (_credentials == null)
            {
                throw new ArgumentException(string.Format("{0} cannot be null", "BacktraceCredentials"));
            }

            _serverurl = credentials.GetSubmissionUrl();
        }
        
#endif

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
                requestUrl += string.Format("{0}_mod_duplicate={1}", startingChar, deduplication);
            }
            using (var request = new UnityWebRequest(requestUrl, "POST"))
            {
#if UNITY_2018_4_OR_NEWER
                if (_ignoreSslValidation)
                {
                    request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                }
#endif

                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                BacktraceResult result;
                if (request.responseCode == 200)
                {
                    result = new BacktraceResult();
                    if (OnServerResponse != null) OnServerResponse.Invoke(result);
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
                    if (OnServerError != null) OnServerError.Invoke(exception);
                }

                if (callback != null) callback.Invoke(result);
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
                if (System.IO.File.Exists(attachment))
                {
                    string fileName = System.IO.Path.GetFileName(attachment);
                    string serverUrl = GetAttachmentUploadUrl(rxId, fileName);
                    Debug.Log("Server url = " + serverUrl);
                    using (var request = new UnityWebRequest(serverUrl, "POST"))
                    {

#if UNITY_2018_4_OR_NEWER
                        if (_ignoreSslValidation)
                        {
                            request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                        }
#endif
                        byte[] bodyRaw = System.IO.File.ReadAllBytes(attachment);
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
            return _credentials == null || string.IsNullOrEmpty(_credentials.Token)
                ? string.Format("{0}&object={1}&attachment_name={2}", _credentials.BacktraceHostUri.AbsoluteUri, rxId,
                    UrlEncode(attachmentName))
                : string.Format("{0}/api/post?token={1}&object={2}&attachment_name={3}",
                    _credentials.BacktraceHostUri.AbsoluteUri, _credentials.Token, rxId, UrlEncode(attachmentName));

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