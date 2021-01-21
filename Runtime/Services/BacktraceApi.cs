﻿using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [Obsolete("RequestHandler is obsolete. BacktraceApi won't be able to provide BacktraceData in every situation")]
        public Func<string, BacktraceData, BacktraceResult> RequestHandler { get; set; }

        /// <summary>
        /// Determine if BacktraceApi should display failure message on HTTP failure.
        /// </summary>
        private bool _shouldDisplayFailureMessage = true;

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
        private readonly Uri _serverUrl;

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
                return _serverUrl.ToString();
            }
        }

        /// <summary>
        /// Submission url for uploading minidump files
        /// </summary>
        private readonly string _minidumpUrl;


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
            _serverUrl = credentials.GetSubmissionUrl();
            _minidumpUrl = credentials.GetMinidumpSubmissionUrl().ToString();
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

            var minidumpBytes = File.ReadAllBytes(minidumpPath);
            if (minidumpBytes == null || minidumpBytes.Length == 0)
            {
                yield break;
            }
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("upload_file", minidumpBytes)
            };

            foreach (var file in attachments)
            {
                if (File.Exists(file) && new FileInfo(file).Length < 10000000)
                {
                    formData.Add(new MultipartFormFileSection(
                        string.Format("attachment__{0}", Path.GetFileName(file)),
                        File.ReadAllBytes(file)));
                }
            }

            yield return new WaitForEndOfFrame();
            var boundaryIdBytes = UnityWebRequest.GenerateBoundary();
            using (var request = UnityWebRequest.Post(_minidumpUrl, formData, boundaryIdBytes))
            {
#if UNITY_2018_4_OR_NEWER
                if (_ignoreSslValidation)
                {
                    request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                }
#endif
                request.SetRequestHeader("Content-Type", string.Format("multipart/form-data; boundary={0}", Encoding.UTF8.GetString(boundaryIdBytes)));
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
                    Debug.Log(string.Format("Backtrace - minidump send time: {0}μs", stopWatch.GetMicroseconds()));
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
                ? GetParametrizedQuery(_serverUrl.ToString(), queryAttributes)
                : ServerUrl;


            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("upload_file",  Encoding.UTF8.GetBytes(json), "upload_file.json", "application/json")
            };

            foreach (var file in attachments)
            {
                if (File.Exists(file) && new FileInfo(file).Length < 10000000)
                {
                    formData.Add(new MultipartFormFileSection(
                        string.Format("attachment__{0}", Path.GetFileName(file)),
                        File.ReadAllBytes(file)));
                }
            }


            var boundaryIdBytes = UnityWebRequest.GenerateBoundary();
            using (var request = UnityWebRequest.Post(requestUrl, formData, boundaryIdBytes))
            {
#if UNITY_2018_4_OR_NEWER
                if (_ignoreSslValidation)
                {
                    request.certificateHandler = new BacktraceSelfSSLCertificateHandler();
                }
#endif
                request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + Encoding.UTF8.GetString(boundaryIdBytes));
                request.timeout = 15000;
                yield return request.SendWebRequest();

                BacktraceResult result;
                if (request.responseCode == 429)
                {
                    result = new BacktraceResult()
                    {
                        Message = "Server report limit reached",
                        Status = Types.BacktraceResultStatus.LimitReached
                    };
                    if (OnServerResponse != null)
                    {
                        OnServerResponse.Invoke(result);
                    }
                }
                else if (request.responseCode == 200 && (!request.isNetworkError || !request.isHttpError))
                {
                    result = BacktraceResult.FromJson(request.downloadHandler.text);
                    _shouldDisplayFailureMessage = true;

                    if (OnServerResponse != null)
                    {
                        OnServerResponse.Invoke(result);
                    }
                }
                else
                {
                    PrintLog(request);
                    var exception = new Exception(request.error);
                    result = BacktraceResult.OnNetworkError(exception);
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
                    Debug.Log(string.Format("Backtrace - JSON send time: {0}μs", stopWatch.GetMicroseconds()));
                }
                yield return result;
            }
        }

        private void PrintLog(UnityWebRequest request)
        {
            if (!_shouldDisplayFailureMessage)
            {
                return;
            }
            _shouldDisplayFailureMessage = false;
            Debug.LogWarning(string.Format("{0}{1}", string.Format("[Backtrace]::Reponse code: {0}, Response text: {1}",
                    request.responseCode,
                    request.error),
                "\n Please check provided url to Backtrace service or learn more from our integration guide: https://support.backtrace.io/hc/en-us/articles/360040515991-Unity-Integration-Guide"));
        }

        private string GetParametrizedQuery(string serverUrl, Dictionary<string, string> queryAttributes)
        {
            var uriBuilder = new UriBuilder(serverUrl);
            if (queryAttributes == null || !queryAttributes.Any())
            {
                return uriBuilder.Uri.ToString();
            }


            StringBuilder builder = new StringBuilder();
            var shouldStartWithAnd = true;
            if (string.IsNullOrEmpty(uriBuilder.Query))
            {
                shouldStartWithAnd = false;
                builder.Append("?");
            }

            for (int queryIndex = 0; queryIndex < queryAttributes.Count; queryIndex++)
            {
                if (queryIndex != 0 || shouldStartWithAnd)
                {
                    builder.Append("&");
                }
                var queryAttribute = queryAttributes.ElementAt(queryIndex);
                builder.AppendFormat("{0}={1}", queryAttribute.Key, queryAttribute.Value);
            }
            uriBuilder.Query += builder.ToString();
            return Uri.EscapeUriString(uriBuilder.Uri.ToString());
        }
    }
}