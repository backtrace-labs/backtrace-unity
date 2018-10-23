using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Newtonsoft.Json;
using System;
using UnityEngine;

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
        public BacktraceResult Send(BacktraceData data)
        {
            //check rate limiting
            bool watcherValidation = reportLimitWatcher.WatchReport(data.Report);
            if (!watcherValidation)
            {
                return BacktraceResult.OnLimitReached(data.Report);
            }
            var jsop = JsonUtility.ToJson(data);
            var annotationsFromNewtonsoft = JsonConvert.SerializeObject(data.Annotations);
            var annotations = JsonUtility.ToJson(data.Annotations);

            // execute user custom request handler
            throw new NotImplementedException();
            //if (RequestHandler != null)
            //{
            //    return RequestHandler?.Invoke(_serverurl, FormDataHelper.GetContentTypeWithBoundary(Guid.NewGuid()), data);
            //}
            ////set submission data
            //string json = JsonConvert.SerializeObject(data);
            //return Send(Guid.NewGuid(), json, data.Report?.AttachmentPaths ?? new List<string>(), data.Report);
        }

        //private BacktraceResult Send(Guid requestId, string json, List<string> attachments, BacktraceReport report)
        //{
        //    var formData = FormDataHelper.GetFormData(json, attachments, requestId);
        //    string contentType = FormDataHelper.GetContentTypeWithBoundary(requestId);
        //    var request = WebRequest.Create(_serverurl) as HttpWebRequest;

        //    //Set up the request properties.
        //    request.Method = "POST";
        //    request.ContentType = contentType;
        //    request.ContentLength = formData.Length;
        //    try
        //    {
        //        using (Stream requestStream = request.GetRequestStream())
        //        {
        //            requestStream.Write(formData, 0, formData.Length);
        //            requestStream.Close();
        //        }
        //        return ReadServerResponse(request, report);
        //    }
        //    catch (Exception exception)
        //    {
        //        OnServerError?.Invoke(exception);
        //        return BacktraceResult.OnError(report, exception);
        //    }
        //}

        ///// <summary>
        ///// Handle server respond for synchronous request
        ///// </summary>
        ///// <param name="request">Current HttpWebRequest</param>
        //private BacktraceResult ReadServerResponse(HttpWebRequest request, BacktraceReport report)
        //{
        //    using (WebResponse webResponse = request.GetResponse() as HttpWebResponse)
        //    {
        //        StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
        //        string fullResponse = responseReader.ReadToEnd();
        //        var response = JsonConvert.DeserializeObject<BacktraceResult>(fullResponse);
        //        response.BacktraceReport = report;
        //        OnServerResponse?.Invoke(response);
        //        return response;
        //    }
        //}
        //#endregion
        ///// <summary>
        ///// Get serialization settings
        ///// </summary>
        ///// <returns></returns>
        //private JsonSerializerSettings JsonSerializerSettings { get; } = new JsonSerializerSettings
        //{
        //    NullValueHandling = NullValueHandling.Ignore,
        //    DefaultValueHandling = DefaultValueHandling.Ignore
        //};


        public void SetClientRateLimitEvent(Action<BacktraceReport> onClientReportLimitReached)
        {
            reportLimitWatcher.OnClientReportLimitReached = onClientReportLimitReached;
        }

        public void SetClientRateLimit(uint rateLimit)
        {
            reportLimitWatcher.SetClientReportLimit(rateLimit);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}