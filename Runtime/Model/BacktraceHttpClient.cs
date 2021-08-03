using Backtrace.Unity.Extensions;
using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Backtrace Http client - HTTP client interface that wraps Unity web requests
    /// </summary>
    internal sealed class BacktraceHttpClient : IBacktraceHttpClient
    {
        /// <summary>
        /// Ignore ssl validation flag
        /// </summary>
        public bool IgnoreSslValidation { get; set; }

        /// <summary>
        /// Name reserved file with diagnostic data - JSON diagnostic data/minidump file
        /// </summary>
        private const string DiagnosticFileName = "upload_file";

        /// <summary>
        /// Request timeout
        /// </summary>
        private const int RequestTimeout = 15000;

        /// <summary>
        /// Post Backtrace JObject to server
        /// </summary>
        /// <param name="submissionUrl">Submission URL</param>
        /// <param name="jObject">Backtrace JObject</param>
        /// <param name="onComplete">On complete callback</param>
        /// <returns>Async operation</returns>
        public void Post(string submissionUrl, BacktraceJObject jObject, Action<long, bool, string> onComplete)
        {
            UnityWebRequest request = new UnityWebRequest(submissionUrl, "POST")
            {
                timeout = RequestTimeout
            };
            request.IgnoreSsl(IgnoreSslValidation);

            var bytes = Encoding.UTF8.GetBytes(jObject.ToJson());
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bytes);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetJsonContentType();
            var asyncOperation = request.SendWebRequest();
            asyncOperation.completed += (AsyncOperation operation) =>
            {
                var statusCode = request.responseCode;
                var response = request.downloadHandler.text;
                var networkError = request.ReceivedNetworkError();
                request.Dispose();
                if (onComplete != null)
                {
                    onComplete.Invoke(statusCode, networkError, response);
                }
            };
        }

        /// <summary>
        /// Post multipart form data with JSON object to server.
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <param name="attachments">List of attachemnt paths</param>
        /// <param name="queryAttributes">query attributes</param>
        /// <returns>async operation</returns>
        public UnityWebRequest Post(string submissionUrl, string json, IEnumerable<string> attachments, IDictionary<string, string> attributes)
        {
            return Post(submissionUrl, CreateJsonFormData(Encoding.UTF8.GetBytes(json), attachments, attributes));
        }

        /// <summary>
        /// Post multipart form data with minidump file to server.
        /// </summary>
        /// <param name="minidump">minidump bytes</param>
        /// <param name="attachments">List of attachemnt paths</param>
        /// <param name="queryAttributes">query attributes</param>
        /// <returns>async operation</returns>
        public UnityWebRequest Post(string submissionUrl, byte[] minidump, IEnumerable<string> attachments, IDictionary<string, string> attributes)
        {
            return Post(submissionUrl, CreateMinidumpFormData(minidump, attachments, attributes));
        }

        private UnityWebRequest Post(string submissionUrl, List<IMultipartFormSection> formData)
        {
            var boundaryIdBytes = UnityWebRequest.GenerateBoundary();

            var request = UnityWebRequest.Post(submissionUrl, formData, boundaryIdBytes);
            request.timeout = RequestTimeout;
            request.IgnoreSsl(IgnoreSslValidation);
            request.SetMultipartFormData(boundaryIdBytes);
            return request;
        }

        /// <summary>
        /// Generate JSON form data
        /// </summary>
        /// <param name="json">Diagnostic JSON bytes</param>
        /// <param name="attachments">List of attachments</param>
        /// <returns>Diagnostic JSON form data</returns>
        private List<IMultipartFormSection> CreateJsonFormData(byte[] json, IEnumerable<string> attachments, IDictionary<string, string> attributes)
        {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(DiagnosticFileName,  json, string.Format("{0}.json",DiagnosticFileName), "application/json")
            };
            AddAttributesToFormData(formData, attributes);
            AddAttachmentToFormData(formData, attachments);
            return formData;
        }

        /// <summary>
        /// Create minidump form data
        /// </summary>
        /// <param name="minidump">Minidump bytes</param>
        /// <param name="attachments">list of attachments</param>
        /// <returns>Minidump form data</returns>
        private List<IMultipartFormSection> CreateMinidumpFormData(byte[] minidump, IEnumerable<string> attachments, IDictionary<string, string> attributes)
        {

            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(DiagnosticFileName, minidump),
            };
            AddAttributesToFormData(formData, attributes);
            AddAttachmentToFormData(formData, attachments);
            return formData;
        }

        private void AddAttributesToFormData(List<IMultipartFormSection> formData, IDictionary<string, string> attributes)
        {
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {

                    if (string.IsNullOrEmpty(attribute.Value))
                    {
                        continue;
                    }
                    formData.Add(new MultipartFormDataSection(attribute.Key, attribute.Value));
                }
            }
        }

        private void AddAttachmentToFormData(List<IMultipartFormSection> formData, IEnumerable<string> attachments)
        {
            if (attachments == null)
            {
                return;
            }
            // make sure attachments are not bigger than 10 Mb.
            const int maximumAttachmentSize = 10000000;
            const string attachmentPrefix = "attachment_";

            var uniqueAttachments = new HashSet<string>(attachments.Reverse());
            var addedFiles = new Dictionary<string, int>();

            foreach (var file in uniqueAttachments)
            {
                if (string.IsNullOrEmpty(file) || File.Exists(file) == false || new FileInfo(file).Length > maximumAttachmentSize)
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);
                if (addedFiles.ContainsKey(fileName))
                {
                    addedFiles[fileName]++;
                    fileName = string.Format("{0}({1}){2}", Path.GetFileName(fileName), addedFiles[fileName], Path.GetExtension(fileName));
                }
                else
                {
                    addedFiles[fileName] = 0;
                }

                formData.Add(new MultipartFormFileSection(
                    string.Format("{0}{1}", attachmentPrefix, fileName),
                    File.ReadAllBytes(file)));

            }
        }
    }
}
