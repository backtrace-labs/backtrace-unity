using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Runtime.Native;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
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
                {
                    return BacktraceApi.OnServerError;
                }
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
                {
                    return BacktraceApi.RequestHandler;
                }
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
                {
                    return BacktraceApi.OnServerResponse;
                }
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
            get
            {
                return _onClientReportLimitReached;
            }
        }

        /// <summary>
        /// Set event executed before sending data to Backtrace API
        /// </summary>
        public Func<BacktraceData, BacktraceData> BeforeSend = null;


        /// <summary>
        // Return true to ignore a report, return false to handle the report
        // and generate one for the error
        /// </summary>
        public Func<ReportFilterType, Exception, string, bool> SkipReport = null;

        /// <summary>
        /// Set event executed when unhandled application exception event catch exception
        /// </summary>
        public Action<Exception> OnUnhandledApplicationException = null;

        private INativeClient _nativeClient;

        public bool EnablePerformanceStatistics
        {
            get
            {
                return Configuration.PerformanceStatistics;
            }
        }

        public int GameObjectDepth
        {
            get
            {
                return Configuration.GameObjectDepth == 0
                ? 16 // default maximum game object size
                : Configuration.GameObjectDepth;
            }
        }


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
                {
                    Database.SetApi(_backtraceApi);
                }
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
                {
                    Database.SetReportWatcher(_reportLimitWatcher);
                }
            }
        }

        private BacktraceLogManager _backtraceLogManager;

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

            CaptureUnityMessages();
            _reportLimitWatcher = new ReportLimitWatcher(Convert.ToUInt32(Configuration.ReportPerMin));


            BacktraceApi = new BacktraceApi(
                credentials: new BacktraceCredentials(Configuration.GetValidServerUrl()),

#if UNITY_2018_4_OR_NEWER
                ignoreSslValidation: Configuration.IgnoreSslValidation
#else
                ignoreSslValidation: false
#endif
                );
            BacktraceApi.EnablePerformanceStatistics = Configuration.PerformanceStatistics;

            if (!Configuration.DestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
                _instance = this;
            }
            Database = GetComponent<BacktraceDatabase>();
            if (Database != null)
            {
                Database.Reload();
                Database.SetApi(BacktraceApi);
                Database.SetReportWatcher(_reportLimitWatcher);
            }

            _nativeClient = NativeClientFactory.GetNativeClient(Configuration, name);

            if (Configuration.SendUnhandledGameCrashesOnGameStartup && isActiveAndEnabled)
            {
                var nativeCrashUplaoder = new NativeCrashUploader();
                nativeCrashUplaoder.SetBacktraceApi(BacktraceApi);
                StartCoroutine(nativeCrashUplaoder.SendUnhandledGameCrashesOnGameStartup());
            }
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
            if (!Enabled)
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
        /// <param name="attributes">List of report attributes</param>
        public void Send(string message, List<string> attachmentPaths = null, Dictionary<string, string> attributes = null)
        {
            if (!ShouldSendReport(message, attachmentPaths, attributes))
            {
                return;
            }
            var report = new BacktraceReport(
              message: message,
              attachmentPaths: attachmentPaths,
              attributes: attributes);
            _backtraceLogManager.Enqueue(report);

            SendReport(report);
        }

        /// <summary>
        /// Send an exception to Backtrace API
        /// </summary>
        /// <param name="exception">Report exception</param>
        /// <param name="attachmentPaths">List of attachments</param>
        /// <param name="attributes">List of report attributes</param
        public void Send(Exception exception, List<string> attachmentPaths = null, Dictionary<string, string> attributes = null)
        {
            if (!ShouldSendReport(exception, attachmentPaths, attributes))
            {
                return;
            }

            var report = new BacktraceReport(exception, attributes, attachmentPaths);
            _backtraceLogManager.Enqueue(report);
            SendReport(report);
        }

        /// <summary>
        /// Send a report to Backtrace API
        /// </summary>
        /// <param name="report">Report to send</param>
        /// <param name="sendCallback">Send report callback</param>
        public void Send(BacktraceReport report, Action<BacktraceResult> sendCallback = null)
        {
            if (!ShouldSendReport(report))
            {
                return;
            }
            _backtraceLogManager.Enqueue(report);
            SendReport(report, sendCallback);
        }

        /// <summary>
        /// Send a report to Backtrace API after first type of report validation rules
        /// </summary>
        /// <param name="report">Backtrace report</param>
        /// <param name="sendCallback">send callback</param>
        private void SendReport(BacktraceReport report, Action<BacktraceResult> sendCallback = null)
        {
            if (BacktraceApi == null)
            {
                Debug.LogWarning("Backtrace API doesn't exist. Please validate client token or server url!");
                return;
            }
            StartCoroutine(CollectDataAndSend(report, sendCallback));
        }

        /// <summary>
        /// Collect diagnostic data and send to API
        /// </summary>
        /// <param name="report">Backtrace Report</param>
        /// <param name="sendCallback">Coroutine callback</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator CollectDataAndSend(BacktraceReport report, Action<BacktraceResult> sendCallback = null)
        {
            var queryAttributes = new Dictionary<string, string>();
            var stopWatch = EnablePerformanceStatistics
                ? System.Diagnostics.Stopwatch.StartNew()
                : new System.Diagnostics.Stopwatch();

            BacktraceData data = SetupBacktraceData(report);

            if (EnablePerformanceStatistics)
            {
                stopWatch.Stop();
                queryAttributes["performance.report"] = stopWatch.GetMicroseconds();
            }

            if (BeforeSend != null)
            {
                data = BeforeSend.Invoke(data);
                if (data == null)
                {
                    yield break;
                }
            }
            BacktraceDatabaseRecord record = null;

            if (Database != null)
            {
                yield return new WaitForEndOfFrame();
                if (EnablePerformanceStatistics)
                {
                    stopWatch.Restart();
                }
                record = Database.Add(data);
                // handle situation when database refuse to store report.
                if (record != null)
                {
                    //Extend backtrace data with additional attachments from backtrace database
                    data = record.BacktraceData;
                    if (EnablePerformanceStatistics)
                    {
                        stopWatch.Stop();
                        queryAttributes["performance.database"] = stopWatch.GetMicroseconds();
                    }


                    if (record.Duplicated)
                    {
                        yield break;
                    }
                }
            }

            yield return new WaitForEndOfFrame();
            if (EnablePerformanceStatistics)
            {
                stopWatch.Restart();
            }
            // avoid serializing data twice
            // if record is here we should try to send json data that are available in record
            // otherwise we can still use BacktraceData.ToJson().            
            string json = record != null
                ? record.BacktraceDataJson()
                : data.ToJson();

            if (EnablePerformanceStatistics)
            {
                stopWatch.Stop();
                queryAttributes["performance.json"] = stopWatch.GetMicroseconds();
            }

            //backward compatibility 
            if (RequestHandler != null)
            {
                yield return RequestHandler.Invoke(BacktraceApi.ServerUrl, data);
                yield break;
            }

            if (data.Deduplication != 0)
            {
                queryAttributes["_mod_duplicate"] = data.Deduplication.ToString();
            }

            StartCoroutine(BacktraceApi.Send(json, data.Attachments, queryAttributes, (BacktraceResult result) =>
            {
                if (record != null)
                {
                    record.Dispose();
                    if (result.Status == BacktraceResultStatus.Ok && Database != null)
                    {
                        Database.Delete(record);
                    }
                }
                //check if there is more errors to send
                //handle inner exception
                HandleInnerException(report, (BacktraceResult innerResult) =>
                {
                    result.InnerExceptionResult = innerResult;
                });

                if (sendCallback != null)
                {
                    sendCallback.Invoke(result);
                }
            }));
        }

        /// <summary>
        /// Collect additional report information from client and convert report to backtrace data
        /// </summary>
        /// <param name="report">Backtrace report</param>
        /// <returns>Backtrace data</returns>
        private BacktraceData SetupBacktraceData(BacktraceReport report)
        {

            // apply _mod fingerprint attribute when client should use
            // normalized exception message instead environment stack trace
            // for exceptions without stack trace.
            if (Configuration.UseNormalizedExceptionMessage)
            {
                report.SetReportFingerPrintForEmptyStackTrace();
            }

            // add environment information to backtrace report
            var sourceCode = _backtraceLogManager.Disabled
                ? new BacktraceUnityMessage(report).ToString()
                : _backtraceLogManager.ToSourceCode();

            report.AssignSourceCodeToReport(sourceCode);

            var reportAttributes = new Dictionary<string, string>();
            // extend client attributes with native attributes
            if (_nativeClient != null)
            {
                reportAttributes = _nativeClient.GetAttributes();
            }

            return report.ToBacktraceData(reportAttributes, GameObjectDepth);
        }

#if UNITY_ANDROID
        /// <summary>
        /// ANR Detection event. This method will be replaced by Backtrace-Android soon native API.
        /// </summary>
        /// <param name="stackTrace">Main thread stack trace</param>
        internal void OnAnrDetected(string stackTrace)
        {
            const string anrMessage = "ANRException: Blocked thread detected";
            _backtraceLogManager.Enqueue(new BacktraceUnityMessage(anrMessage, stackTrace, LogType.Error));
            var hang = new BacktraceUnhandledException(anrMessage, stackTrace);
            SendUnhandledException(hang);
        }
#endif

        /// <summary>
        /// Handle Untiy unhandled exceptions
        /// </summary>
        private void CaptureUnityMessages()
        {
            _backtraceLogManager = new BacktraceLogManager(Configuration.NumberOfLogs);
            if (Configuration.HandleUnhandledExceptions || Configuration.NumberOfLogs != 0)
            {
                Application.logMessageReceived += HandleUnityMessage;
            }
        }

        /// <summary>
        /// Catch Unity logger data and create Backtrace reports for log type that represents exception or error
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="stackTrace">Log stack trace</param>
        /// <param name="type">log type</param>
        internal void HandleUnityMessage(string message, string stackTrace, LogType type)
        {
            var unityMessage = new BacktraceUnityMessage(message, stackTrace, type);
            _backtraceLogManager.Enqueue(unityMessage);
            if (Configuration.HandleUnhandledExceptions && unityMessage.IsUnhandledException())
            {
                var exception = new BacktraceUnhandledException(unityMessage.Message, unityMessage.StackTrace);
                SendUnhandledException(exception);
            }
        }

        private void SendUnhandledException(BacktraceUnhandledException exception)
        {
            if (OnUnhandledApplicationException != null)
            {
                OnUnhandledApplicationException.Invoke(exception);
            }
            if (ShouldSendReport(exception, null, null))
            {
                SendReport(new BacktraceReport(exception));
            }
        }

        private bool ShouldSendReport(Exception exception, List<string> attachmentPaths, Dictionary<string, string> attributes)
        {
            // guess report type
            var filterType = ReportFilterType.Exception;
            if (exception is BacktraceUnhandledException)
            {
                filterType = (exception as BacktraceUnhandledException).Classifier == "ANRException"
                    ? ReportFilterType.Hang
                    : ReportFilterType.UnhandledException;
            }


            if (ShouldSkipReport(filterType, exception, string.Empty))
            {
                return false;
            }
            //check rate limiting
            bool shouldProcess = _reportLimitWatcher.WatchReport(new DateTime().Timestamp());
            if (shouldProcess)
            {
                return true;
            }
            if (OnClientReportLimitReached != null)
            {
                var report = new BacktraceReport(
                  exception: exception,
                  attachmentPaths: attachmentPaths,
                  attributes: attributes);
                _onClientReportLimitReached.Invoke(report);
            }
            return false;
        }

        private bool ShouldSendReport(string message, List<string> attachmentPaths, Dictionary<string, string> attributes)
        {
            if (ShouldSkipReport(ReportFilterType.Message, null, message))
            {
                return false;
            }

            //check rate limiting
            bool shouldProcess = _reportLimitWatcher.WatchReport(new DateTime().Timestamp());
            if (shouldProcess)
            {
                return true;
            }
            if (OnClientReportLimitReached != null)
            {
                var report = new BacktraceReport(
                  message: message,
                  attachmentPaths: attachmentPaths,
                  attributes: attributes);
                _onClientReportLimitReached.Invoke(report);
            }
            return false;
        }

        private bool ShouldSendReport(BacktraceReport report)
        {
            if (ShouldSkipReport(
                    report.ExceptionTypeReport
                        ? ReportFilterType.Exception
                        : ReportFilterType.Message,
                    report.Exception,
                    report.Message))
            {
                return false;
            }
            //check rate limiting
            bool shouldProcess = _reportLimitWatcher.WatchReport(new DateTime().Timestamp());
            if (shouldProcess)
            {
                return true;
            }
            if (OnClientReportLimitReached != null)
            {
                _onClientReportLimitReached.Invoke(report);
            }
            return false;
        }


        /// <summary>
        /// Handle inner exception in current backtrace report
        /// if inner exception exists, client should send report twice - one with current exception, one with inner exception
        /// </summary>
        /// <param name="report">current report</param>
        private void HandleInnerException(BacktraceReport report, Action<BacktraceResult> callback)
        {
            var innerExceptionReport = report.CreateInnerReport();
            if (innerExceptionReport != null && ShouldSendReport(innerExceptionReport))
            {
                SendReport(innerExceptionReport, callback);
            }
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


        /// <summary>
        /// Check if client should skip current report
        /// </summary>
        /// <param name="type">Report type</param>
        /// <param name="exception">Exception object</param>
        /// <param name="message">String message</param>
        /// <returns>true if client should skip report. Otherwise false.</returns>
        private bool ShouldSkipReport(ReportFilterType type, Exception exception, string message)
        {
            return Configuration.ReportFilterType.HasFlag(type)
                || (SkipReport != null && SkipReport.Invoke(type, exception, message));

        }

    }
}
