using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity
{
    /// <summary>
    /// Base Backtrace .NET Client 
    /// </summary>
    public class BacktraceClient : MonoBehaviour, IBacktraceClient
    {
        public BacktraceConfiguration Configuration;

        /// <summary>
        /// Backtrace database instance that allows to manage minidump files 
        /// </summary>
        public IBacktraceDatabase Database;

        private IBacktraceApi _backtraceApi;
        /// <summary>
        /// Set an event executed when received bad request, unauthorize request or other information from server
        /// </summary>
        public Action<Exception> OnServerError
        {
            get
            {
                return BacktraceApi.OnServerError;
            }

            set
            {
                BacktraceApi.OnServerError = value;
            }
        }

        /// <summary>
        /// Set an event executed when Backtrace API return information about send report
        /// </summary>
        public Action<BacktraceResult> OnServerResponse
        {
            get
            {
                return BacktraceApi.OnServerResponse;
            }

            set
            {
                BacktraceApi.OnServerResponse = value;
            }
        }

        /// <summary>
        /// Get or set minidump type
        /// </summary>
        public MiniDumpType MiniDumpType { get; set; } = MiniDumpType.Normal;

        /// <summary>
        /// Set event executed when client site report limit reached
        /// </summary>
        public Action<BacktraceReport> OnClientReportLimitReached
        {
            set
            {
                BacktraceApi.SetClientRateLimitEvent(value);
            }
        }

        /// <summary>
        /// Set event executed before sending data to Backtrace API
        /// </summary>
        public Func<BacktraceData, BacktraceData> BeforeSend = null;

        /// <summary>
        /// Set event executed when unhandled application exception event catch exception
        /// </summary>
        public Action<Exception> OnUnhandledApplicationException = null;

        /// <summary>
        /// Get custom client attributes. Every argument stored in dictionary will be send to Backtrace API
        /// </summary>
        public readonly Dictionary<string, object> Attributes;


        /// <summary>
        /// Instance of BacktraceApi that allows to send data to Backtrace API
        /// </summary>
        internal IBacktraceApi BacktraceApi
        {
            get
            {
                return _backtraceApi;
            }

            set
            {
                _backtraceApi = value;
                Database?.SetApi(_backtraceApi);
            }
        }

        private void Awake()
        {
            Database = GetComponent<BacktraceDatabase>();
            if (Configuration == null || !Configuration.IsValid())
            {
                Debug.LogWarning("Configuration doesn't exists or provided serverurl/token are invalid");
                return;
            }
            if (Configuration.HandleUnhandledExceptions)
            {
                HandleUnhandledExceptions();
            }
            BacktraceApi = new BacktraceApi(
                credentials: new BacktraceCredentials(Configuration.ServerUrl, Configuration.Token),
                reportPerMin: Convert.ToUInt32(Configuration.ReportPerMin));

            Database?.SetApi(BacktraceApi);
        }

        /// <summary>
        /// Change maximum number of reportrs sending per one minute
        /// </summary>
        /// <param name="reportPerMin">Number of reports sending per one minute. If value is equal to zero, there is no request sending to API. Value have to be greater than or equal to 0</param>
        public void SetClientReportLimit(uint reportPerMin)
        {
            BacktraceApi.SetClientRateLimit(reportPerMin);
        }

        public void Send(string message, List<string> attachmentPaths = null, Dictionary<string,object> attributes = null)
        {
            var report = new BacktraceReport(
                message: message,
                attachmentPaths: attachmentPaths,
                attributes: attributes);
            Send(report);
        }

        public void Send(Exception exception, List<string> attachmentPaths = null, Dictionary<string,object> attributes = null)
        {
            var report = new BacktraceReport(
                exception: exception, 
                attributes: attributes,
                attachmentPaths: attachmentPaths);
            Send(report);            
        }

        /// <summary>
        /// Send a report to Backtrace API
        /// </summary>
        /// <param name="report">Report to send</param>
        public void Send(BacktraceReport report, Action<BacktraceResult> sendCallback = null)
        {
            var record = Database?.Add(report, Attributes, MiniDumpType);
            //create a JSON payload instance
            var data = record?.BacktraceData ?? report.ToBacktraceData(Attributes);
            //valid user custom events
            data = BeforeSend?.Invoke(data) ?? data;

            if (BacktraceApi == null)
            {
                record?.Dispose();
                Debug.LogWarning("Backtrace API not exisits. Please validate client token or server url!");
                return;
            }

            StartCoroutine(BacktraceApi.Send(data, (BacktraceResult result) =>
            {
                record?.Dispose();
                if (result?.Status == BacktraceResultStatus.Ok)
                {
                    Database.Delete(record);
                }
                //check if there is more errors to send
                //handle inner exception
                HandleInnerException(report, (BacktraceResult innerResult) =>
                {
                    result.InnerExceptionResult = innerResult;
                });
                sendCallback?.Invoke(result);
            }));
        }

        public void HandleUnhandledExceptions()
        {
            Application.logMessageReceived += HandleException;
        }

        private void HandleException(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                var exception = new BacktraceUnhandledException(condition, stackTrace);
                var report = new BacktraceReport(exception);
                Send(report);
            }
        }

        /// <summary>
        /// Handle inner exception in current backtrace report
        /// if inner exception exists, client should send report twice - one with current exception, one with inner exception
        /// </summary>
        /// <param name="report">current report</param>
        private IEnumerator HandleInnerException(BacktraceReport report, Action<BacktraceResult> callback)
        {
            var innerExceptionReport = report.CreateInnerReport();
            if (innerExceptionReport == null)
            {
                yield return null;
            }
            Send(innerExceptionReport, callback);
        }
    }
}
