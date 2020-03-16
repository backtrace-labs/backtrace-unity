using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
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
        public bool Enabled { get; private set; }

        /// <summary>
        /// Backtrace client instance.
        /// </summary>
        private static BacktraceClient _instance;

        /// <summary>
        ///  Backtrace client instance accessor. Please use this property to access
        ///  BacktraceClient instance from other scene. This property will return value only
        ///  when you mark option "DestroyOnLoad" to false.
        /// </summary>
        public static BacktraceClient Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Backtrace database instance that allows to manage minidump files 
        /// </summary>
        public IBacktraceDatabase Database;

        private IBacktraceApi _backtraceApi;

        private ReportLimitWatcher _reportLimitWatcher;
        /// <summary>
        /// Set an event executed when received bad request, unauthorize request or other information from server
        /// </summary>
        public Action<Exception> OnServerError
        {
            get
            {
                if (BacktraceApi != null)
                    return BacktraceApi.OnServerError;
                return null;
            }

            set
            {
                if (ValidClientConfiguration())
                {
                    BacktraceApi.OnServerError = value;
                }
            }
        }

        public Func<string, BacktraceData, BacktraceResult> RequestHandler
        {
            get
            {
                if (BacktraceApi != null)
                    return BacktraceApi.RequestHandler;
                return null;
            }
            set
            {
                if (ValidClientConfiguration())
                {
                    BacktraceApi.RequestHandler = value;
                }
            }
        }

        /// <summary>
        /// Set an event executed when Backtrace API return information about send report
        /// </summary>
        public Action<BacktraceResult> OnServerResponse
        {
            get
            {
                if (BacktraceApi != null)
                    return BacktraceApi.OnServerResponse;
                return null;
            }

            set
            {
                if (ValidClientConfiguration())
                {
                    BacktraceApi.OnServerResponse = value;
                }
            }
        }

        /// <summary>
        /// Get or set minidump type
        /// </summary>
        public MiniDumpType MiniDumpType = MiniDumpType.None;

        /// <summary>
        /// Set event executed when client site report limit reached
        /// </summary>
        internal Action<BacktraceReport> _onClientReportLimitReached = null;

        /// <summary>
        /// Set event executed when client site report limit reached
        /// </summary>
        public Action<BacktraceReport> OnClientReportLimitReached
        {
            set
            {
                if (ValidClientConfiguration())
                {
                    _onClientReportLimitReached = value;
                }
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
        /// Get custom client attributes. Every argument stored in dictionary will be send to Backtrace API.
        /// Backtrace unity-plugin allows you to store in attributes only primitive values. Plugin will skip loading 
        /// complex object.
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

                if (Database != null)
                    Database.SetApi(_backtraceApi);
            }
        }

        internal ReportLimitWatcher ReportLimitWatcher
        {
            get
            {
                return _reportLimitWatcher;
            }
            set
            {
                _reportLimitWatcher = value;

                if (Database != null)
                    Database.SetReportWatcher(_reportLimitWatcher);
            }
        }

        public void OnDisable()
        {
            Enabled = false;
        }

        public void Refresh()
        {
            if (Configuration == null || !Configuration.IsValid())
            {
                return;
            }
            
            Enabled = true;
            Annotations.GameObjectDepth = Configuration.GameObjectDepth;
            HandleUnhandledExceptions();
            _reportLimitWatcher = new ReportLimitWatcher(Convert.ToUInt32(Configuration.ReportPerMin));
            BacktraceApi = new BacktraceApi(
                credentials: new BacktraceCredentials(Configuration.GetValidServerUrl()),
                ignoreSslValidation: Configuration.IgnoreSslValidation);

            if (Configuration.DestroyOnLoad == false)
            {
                DontDestroyOnLoad(gameObject);
                _instance = this;
            }
            Database = GetComponent<BacktraceDatabase>();
            if (Database == null)
            {
                return;
            }
            Database.Reload();
            Database.SetApi(BacktraceApi);
            Database.SetReportWatcher(_reportLimitWatcher);

        }

        private void Awake()
        {
            Refresh();
        }

        /// <summary>
        /// Change maximum number of reportrs sending per one minute
        /// </summary>
        /// <param name="reportPerMin">Number of reports sending per one minute. If value is equal to zero, there is no request sending to API. Value have to be greater than or equal to 0</param>
        public void SetClientReportLimit(uint reportPerMin)
        {
            if (Enabled == false)
            {
                Debug.LogWarning("Please enable BacktraceClient first - Please validate Backtrace client initializaiton in Unity IDE.");
                return;
            }
            _reportLimitWatcher.SetClientReportLimit(reportPerMin);
        }

        /// <summary>
        /// Send a message report to Backtrace API
        /// </summary>
        /// <param name="message">Report message</param>
        /// <param name="attachmentPaths">List of attachments</param>
        /// <param name="attributes">List of report attributes</param
        public void Send(string message, List<string> attachmentPaths = null, Dictionary<string, object> attributes = null)
        {
            if (Enabled == false)
            {
                Debug.LogWarning("Please enable BacktraceClient first - Please validate Backtrace client initializaiton in Unity IDE.");
                return;
            }
            //check rate limiting
            bool limitHit = _reportLimitWatcher.WatchReport(new DateTime().Timestamp());
            if (limitHit == false && _onClientReportLimitReached == null)
            {
                _reportLimitWatcher.DisplayReportLimitHitMessage();
                return;
            }
            var report = new BacktraceReport(
              message: message,
              attachmentPaths: attachmentPaths,
              attributes: attributes);
            if (!limitHit)
            {
                if (_onClientReportLimitReached != null)
                    _onClientReportLimitReached.Invoke(report);

                _reportLimitWatcher.DisplayReportLimitHitMessage();
                return;
            }

            SendReport(report);
        }

        /// <summary>
        /// Send an exception to Backtrace API
        /// </summary>
        /// <param name="exception">Report exception</param>
        /// <param name="attachmentPaths">List of attachments</param>
        /// <param name="attributes">List of report attributes</param
        public void Send(Exception exception, List<string> attachmentPaths = null, Dictionary<string, object> attributes = null)
        {
            if (Enabled == false)
            {
                Debug.LogWarning("Please enable BacktraceClient first - Please validate Backtrace client initializaiton in Unity IDE.");
                return;
            }
            //check rate limiting
            bool limitHit = _reportLimitWatcher.WatchReport(new DateTime().Timestamp());
            if (limitHit == false && _onClientReportLimitReached == null)
            {
                _reportLimitWatcher.DisplayReportLimitHitMessage();
                return;
            }
            var report = new BacktraceReport(
              exception: exception,
              attachmentPaths: attachmentPaths,
              attributes: attributes);
            if (!limitHit)
            {
                if (_onClientReportLimitReached != null)
                    _onClientReportLimitReached.Invoke(report);

                _reportLimitWatcher.DisplayReportLimitHitMessage();
                return;
            }
            SendReport(report);
        }

        /// <summary>
        /// Send a report to Backtrace API
        /// </summary>
        /// <param name="report">Report to send</param>
        /// <param name="sendCallback">Send report callback</param>
        public void Send(BacktraceReport report, Action<BacktraceResult> sendCallback = null)
        {
            if (Enabled == false)
            {
                Debug.LogWarning("Please enable BacktraceClient first - Please validate Backtrace client initializaiton in Unity IDE.");
                return;
            }
            //check rate limiting
            bool limitHit = _reportLimitWatcher.WatchReport(report);
            if (!limitHit)
            {
                if (_onClientReportLimitReached != null)
                    _onClientReportLimitReached.Invoke(report);

                if (sendCallback != null)
                    sendCallback.Invoke(BacktraceResult.OnLimitReached(report));

                _reportLimitWatcher.DisplayReportLimitHitMessage();
                return;
            }
            SendReport(report, sendCallback);
        }

        /// <summary>
        /// Send a report to Backtrace API after first type of report validation rules
        /// </summary>
        /// <param name="report">Backtrace report</param>
        /// <param name="sendCallback">send callback</param>
        private void SendReport(BacktraceReport report, Action<BacktraceResult> sendCallback = null)
        {
            var record = Database != null ? Database.Add(report, Attributes, MiniDumpType) : null;
            //create a JSON payload instance
            BacktraceData data = null;

            data = (record != null ? record.BacktraceData : null) ?? report.ToBacktraceData(Attributes);
            //valid user custom events
            data = (BeforeSend != null ? BeforeSend.Invoke(data) : null) ?? data;

            if (BacktraceApi == null)
            {
                if (record != null)
                    record.Dispose();

                Debug.LogWarning("Backtrace API doesn't exist. Please validate client token or server url!");
                return;
            }
            StartCoroutine(BacktraceApi.Send(data, (BacktraceResult result) =>
            {
                if (record != null)
                {
                    record.Dispose();
                    //Database?.IncrementRecordRetryLimit(record);
                }
                if(result!=null)
                if (result.Status == BacktraceResultStatus.Ok)
                {
                    if (Database != null)
                        Database.Delete(record);
                }
                //check if there is more errors to send
                //handle inner exception
                HandleInnerException(report, (BacktraceResult innerResult) =>
                {
                    result.InnerExceptionResult = innerResult;
                });

                if (sendCallback != null)
                    sendCallback.Invoke(result);
            }));

        }

        /// <summary>
        /// Handle Untiy unhandled exceptions
        /// </summary>
        public void HandleUnhandledExceptions()
        {
            if (Enabled == false)
            {
                Debug.LogWarning("Cannot set unhandled exception handler for not enabled Backtrace client instance");
                return;
            }
            if (Configuration.HandleUnhandledExceptions)
            {
                Application.logMessageReceived += HandleException;
            }
        }




        /// <summary>
        /// Catch Unity logger data and create Backtrace reports for log type that represents exception or error
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="stackTrace">Log stack trace</param>
        /// <param name="type">log type</param>
        private void HandleException(string message, string stackTrace, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error)
                && (!string.IsNullOrEmpty(message) && !message.StartsWith("[Backtrace]::")))
            {
                var exception = new BacktraceUnhandledException(message, stackTrace);

                if (OnUnhandledApplicationException != null)
                    OnUnhandledApplicationException.Invoke(exception);

                Send(exception);
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

        /// <summary>
        /// Validate if current client configuration is valid 
        /// </summary>
        /// <returns>True if client allows to setup events, otherwise false</returns>
        private bool ValidClientConfiguration()
        {
            var invalidConfiguration = BacktraceApi == null || !Enabled;
            if (invalidConfiguration)
            {
                Debug.LogWarning("Cannot set method if configuration contain invalid url to Backtrace server or client is disabled");
            }
            return !invalidConfiguration;
        }


    }
}
