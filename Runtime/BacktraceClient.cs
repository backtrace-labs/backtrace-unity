using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Runtime.Native;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using Backtrace.Unity.WebGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity
{
    /// <summary>
    /// Base Backtrace .NET Client 
    /// </summary>
    public class BacktraceClient : MonoBehaviour, IBacktraceClient
    {
        public const string VERSION = "3.15.1";
        internal const string DefaultBacktraceGameObjectName = "BacktraceClient";
        public BacktraceConfiguration Configuration;

        /// <summary>
        /// Breadcrumbs instance for internal use. This instance allows to make system calls
        /// </summary>
        private BacktraceBreadcrumbs _breadcrumbs;

        /// <summary>
        /// Backtrace Breadcrumbs
        /// </summary>
        public IBacktraceBreadcrumbs Breadcrumbs
        {
            get
            {
                return _breadcrumbs;
            }
        }

        public bool Enabled { get; private set; }

        private AttributeProvider _attributeProvider;
        /// <summary>
        /// Client attribute provider
        /// </summary>
        internal AttributeProvider AttributeProvider
        {
            get
            {
                if (_attributeProvider == null)
                {
                    _attributeProvider = new AttributeProvider();
                }
                return _attributeProvider;
            }
            set
            {
                _attributeProvider = value;
            }
        }

#if UNITY_ANDROID
        private bool _useProguard = false;

        /// <summary>
        /// Allow to enable Proguard support for captured Exceptions.
        /// </summary>
        /// <param name="symbolicationId">Proguard map symbolication id</param>
        public void UseProguard(String symbolicationId) {
            _useProguard = true;
            AttributeProvider["symbolication_id"] = symbolicationId;
        }
#endif

#if !UNITY_WEBGL
        private BacktraceMetrics _metrics;

        /// <summary>
        /// Backtrace metrics instance
        /// </summary>
        public IBacktraceMetrics Metrics
        {
            get
            {
                if (_metrics == null && Configuration != null && Configuration.EnableMetricsSupport)
                {
                    var universeName = Configuration.GetUniverseName();
                    var token = Configuration.GetToken();

                    _metrics = new BacktraceMetrics(
                        AttributeProvider,
                        Configuration.GetEventAggregationIntervalTimerInMs(),
                        BacktraceMetrics.GetDefaultUniqueEventsUrl(universeName, token),
                        BacktraceMetrics.GetDefaultSummedEventsUrl(universeName, token))
                    {
                        IgnoreSslValidation = Configuration.IgnoreSslValidation
                    };
                }
                return _metrics;
            }
        }
#endif
        /// <summary>
        /// Random object instance
        /// </summary>
        internal System.Random Random
        {
            get
            {
                if (_random == null)
                {
                    _random = new System.Random();
                }
                return _random;
            }
        }

        private System.Random _random;

        internal Stack<BacktraceReport> BackgroundExceptions = new Stack<BacktraceReport>();

        private readonly object _backgroundExceptionsLock = new object();
        private readonly object _backtraceLogManagerLock = new object();
        private readonly object _unityLogSuppressionLock = new object();
        private readonly Queue<UnityLogSuppression> _unityLogSuppressions =
            new Queue<UnityLogSuppression>();
        private BacktraceUnityLogHandler _unityLogHandler;
        private ILogHandler _previousUnityLogHandler;

        private sealed class UnityLogSuppression
        {
            public List<string> MessagePrefixes;
            public LogType Type;
            public double ExpiresAtMs;
        }

        /// <summary>
        /// Client report attachments
        /// </summary>
        private HashSet<string> _clientReportAttachments;


        /// <summary>
        /// Attribute object accessor
        /// </summary>
        public string this[string index]
        {
            get
            {
                return AttributeProvider[index];
            }
            set
            {
                SetAttribute(index, value);
            }
        }

        /// <summary>
        /// Add attachment to managed reports.
        /// Note: this option won't add attachment to your native reports. You can add attachments to
        /// native reports only on BacktraceClient initialization.
        /// </summary>
        /// <param name="pathToAttachment">Path to attachment</param>
        public void AddAttachment(string pathToAttachment)
        {
            _clientReportAttachments.Add(pathToAttachment);
        }

        /// <summary>
        /// Returns list of defined path to attachments stored by Backtrace client.
        /// </summary>
        /// <returns>List of client attachments</returns>
        public IEnumerable<string> GetAttachments()
        {
            return _clientReportAttachments;
        }

        /// <summary>
        /// Set a client attribute that will be included in every report
        /// </summary>
        /// <param name="key">Attribute name</param>
        /// <param name="value">Attribute value</param>
        /// <returns>True, if the value was added. Otherwise false.</returns>
        public bool SetAttribute(string key, string value) 
        {
            if (string.IsNullOrEmpty(key)) 
            {
                return false;
            }
            
            AttributeProvider[key] = value;
            if (_nativeClient != null)
            {
                _nativeClient.SetAttribute(key, value);
            }
            return true;
        }

        /// <summary>
        /// Set client attributes that will be included in every report
        /// </summary>
        /// <param name="attributes">attributes dictionary</param>
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            foreach (var attribute in attributes)
            {
                SetAttribute(attribute.Key, attribute.Value);
            }
        }

        /// <summary>
        /// Number of client attributes
        /// </summary>
        public int GetAttributesCount()
        {
            return AttributeProvider.Count();
        }

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
        /// Backtrace log manager
        /// </summary>
        private BacktraceLogManager _backtraceLogManager;

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

        internal INativeClient NativeClient
        {
            get
            {
                return _nativeClient;
            }
        }

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

        /// <summary>
        /// Initialize new Backtrace integration
        /// </summary>
        /// <param name="configuration">Backtrace configuration scriptable object</param>
        /// <param name="attributes">Client side attributes</param>
        /// param name="attachments">List of attachments </param>
        /// <param name="gameObjectName">game object name</param>
        /// <returns>Backtrace client</returns>
        public static BacktraceClient Initialize(BacktraceConfiguration configuration, Dictionary<string, string> attributes = null, string gameObjectName = DefaultBacktraceGameObjectName)
        {
            if (string.IsNullOrEmpty(gameObjectName))
            {
                throw new ArgumentException("Missing game object name");
            }

            if (configuration == null || string.IsNullOrEmpty(configuration.ServerUrl))
            {
                throw new ArgumentException("Missing valid configuration");
            }

            if (Instance != null)
            {
                return Instance;
            }
            var backtrackGameObject = new GameObject(gameObjectName, typeof(BacktraceClient), typeof(BacktraceDatabase));
            BacktraceClient backtraceClient = backtrackGameObject.GetComponent<BacktraceClient>();
            backtraceClient.Configuration = configuration;
            if (configuration.Enabled)
            {
                BacktraceDatabase backtraceDatabase = backtrackGameObject.GetComponent<BacktraceDatabase>();
                backtraceDatabase.Configuration = configuration;
            }
            backtrackGameObject.SetActive(true);
            backtraceClient.Refresh();
            backtraceClient.SetAttributes(attributes);

            return backtraceClient;
        }

        /// <summary>
        /// Initialize new Backtrace integration with database path. Note - database path will be auto created by Backtrace Unity plugin
        /// </summary>
        /// <param name="url">Server url</param>
        /// <param name="databasePath">Database path</param>
        /// <param name="attributes">Client side attributes</param>
        /// <param name="gameObjectName">game object name</param>
        /// <returns>Backtrace client</returns>
        public static BacktraceClient Initialize(string url, string databasePath, Dictionary<string, string> attributes = null, string gameObjectName = DefaultBacktraceGameObjectName)
        {
            return Initialize(url, databasePath, attributes, null, gameObjectName);
        }

        /// <summary>
        /// Initialize new Backtrace integration with database path. Note - database path will be auto created by Backtrace Unity plugin
        /// </summary>
        /// <param name="url">Server url</param>
        /// <param name="databasePath">Database path</param>
        /// <param name="attributes">Client side attributes</param>
        /// <param name="attachments">Paths to attachments that Backtrace client will include in managed/native reports</param>
        /// <param name="gameObjectName">game object name</param>
        /// <returns>Backtrace client</returns>
        public static BacktraceClient Initialize(string url, string databasePath, Dictionary<string, string> attributes = null, string[] attachments = null, string gameObjectName = DefaultBacktraceGameObjectName)
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.ServerUrl = url;
            configuration.AttachmentPaths = attachments;
            configuration.Enabled = true;
            configuration.DatabasePath = databasePath;
            configuration.CreateDatabase = true;
            return Initialize(configuration, attributes, gameObjectName);
        }

        /// <summary>
        /// Initialize new Backtrace integration
        /// </summary>
        /// <param name="url">Server url</param>
        /// <param name="attributes">Client side attributes</param>
        /// <param name="gameObjectName">game object name</param>
        /// <returns>Backtrace client</returns>
        public static BacktraceClient Initialize(string url, Dictionary<string, string> attributes = null, string gameObjectName = DefaultBacktraceGameObjectName)
        {
            return Initialize(url, attributes, new string[0], gameObjectName);
        }

        /// <summary>
        /// Initialize new Backtrace integration
        /// </summary>
        /// <param name="url">Server url</param>
        /// <param name="attributes">Client side attributes</param>
        /// <param name="attachments">Paths to attachments that Backtrace client will include in managed/native reports</param>
        /// <param name="gameObjectName">game object name</param>
        /// <returns>Backtrace client</returns>
        public static BacktraceClient Initialize(string url, Dictionary<string, string> attributes = null, string[] attachments = null, string gameObjectName = DefaultBacktraceGameObjectName)
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.ServerUrl = url;
            configuration.AttachmentPaths = attachments;
            // For WebGL builds, we enable offline storage by default.
            // WebGL network conditions and browser lifecycle events can drop reports unless persistence is enabled.
#if UNITY_WEBGL
            configuration.Enabled = true;
#else
            configuration.Enabled = false;
#endif
            return Initialize(configuration, attributes, gameObjectName);
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

            if (Instance != null)
            {
                return;
            }

            Enabled =
#if UNITY_EDITOR
                !Configuration.DisableInEditor;
#else
                true;
#endif
            _current = Thread.CurrentThread;
            CaptureUnityMessages();
            _reportLimitWatcher = new ReportLimitWatcher(Convert.ToUInt32(Configuration.ReportPerMin));
            _clientReportAttachments = Configuration.GetAttachmentPaths();

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

#if !UNITY_WEBGL
            EnableMetrics(false);
#endif
            string breadcrumbsPath = string.Empty;
            if (Configuration.Enabled)
            {
                Database = GetComponent<BacktraceDatabase>();
                if (Database != null)
                {
                    Database.Reload();
                    _breadcrumbs = (BacktraceBreadcrumbs)Database.Breadcrumbs;
                    Database.SetApi(BacktraceApi);
                    Database.SetReportWatcher(_reportLimitWatcher);
                    if (_breadcrumbs != null)
                    {
                        breadcrumbsPath = _breadcrumbs.GetBreadcrumbLogPath();
                    }
                }
            }
            if (Database != null)
            {
                // send minidump files generated by unity engine or unity game, not captured by Windows native integration
                // this integration should start before native integration and before breadcrumbs integration
                // to allow algorithm to send breadcrumbs file - if the breadcrumb file is available
                var scopedAttributes = AttributeProvider.GenerateAttributes(false);

                var nativeAttachments = GetNativeAttachments();
                // avoid adding breadcurmbs file earlier - to avoid managing breadcrumb file in two places
                // in windows managed integration with unity crash handler
                // breadcrumb path is required by native integration and will be added just before native integration initialization
                if (!string.IsNullOrEmpty(breadcrumbsPath))
                {
                    nativeAttachments.Add(breadcrumbsPath);
                }
                _nativeClient = NativeClientFactory.CreateNativeClient(Configuration, name, _breadcrumbs, scopedAttributes, nativeAttachments);
                AttributeProvider.AddDynamicAttributeProvider(_nativeClient);
            }
        }
        public bool EnableBreadcrumbsSupport()
        {
            if (Database == null)
            {
                return false;
            }
            return Database.EnableBreadcrumbsSupport();
        }
#if !UNITY_WEBGL
        public bool EnableMetrics()
        {
            return EnableMetrics(true);
        }
        private bool EnableMetrics(bool enableIfConfigurationIsDisabled = true)
        {
            if (!Configuration.EnableMetricsSupport)
            {
                if (!enableIfConfigurationIsDisabled)
                {
                    return false;
                }
                Debug.LogWarning("Event aggregation configuration was disabled. Enabling it manually via API");
            }
            return EnableMetrics(BacktraceMetrics.DefaultUniqueAttributeName);
        }

        public bool EnableMetrics(string uniqueAttributeName = BacktraceMetrics.DefaultUniqueAttributeName)
        {
            var universeName = Configuration.GetUniverseName();
            if (string.IsNullOrEmpty(universeName))
            {
                Debug.LogWarning("Cannot initialize event aggregation - Unknown Backtrace URL.");
                return false;
            }
            var token = Configuration.GetToken();
            EnableMetrics(
                BacktraceMetrics.GetDefaultUniqueEventsUrl(universeName, token),
                BacktraceMetrics.GetDefaultSummedEventsUrl(universeName, token),
                Configuration.GetEventAggregationIntervalTimerInMs(),
                uniqueAttributeName);
            return true;
        }

        public bool EnableMetrics(string uniqueEventsSubmissionUrl, string summedEventsSubmissionUrl, uint timeIntervalInSec = BacktraceMetrics.DefaultTimeIntervalInSec, string uniqueAttributeName = BacktraceMetrics.DefaultUniqueAttributeName)
        {
            if (_metrics != null)
            {
                Debug.LogWarning("Backtrace metrics support is already enabled. Please use BacktraceClient.Metrics.");
                return false;
            }
            _metrics = new BacktraceMetrics(
                attributeProvider: AttributeProvider,
                timeIntervalInSec: timeIntervalInSec,
                uniqueEventsSubmissionUrl: uniqueEventsSubmissionUrl,
                summedEventsSubmissionUrl: summedEventsSubmissionUrl
                )
            {
                StartupUniqueAttributeName = uniqueAttributeName,
                IgnoreSslValidation = Configuration.IgnoreSslValidation
            };
            StartupMetrics();
            return true;
        }

        private void StartupMetrics()
        {
            AttributeProvider.AddScopedAttributeProvider(Metrics);
            _metrics.SendStartupEvent();
        }

#endif

        private void OnApplicationQuit()
        {
            if (_nativeClient != null)
            {
                _nativeClient.Disable();
            }
        }

        private void Awake()
        {
            if (_breadcrumbs != null)
            {
                _breadcrumbs.FromMonoBehavior("Application awake", LogType.Assert, null);
            }
            Refresh();
        }

        /// <summary>
        /// Update native client internals.
        /// </summary>
        private void LateUpdate()
        {
            if (_nativeClient != null)
            {
                _nativeClient.Update(Time.unscaledTime);
            }

#if !UNITY_WEBGL
            if (_metrics != null)
            {
                _metrics.Tick(Time.unscaledTime);
            }
#endif

            while (true)
            {
                BacktraceReport report = null;
                lock (_backgroundExceptionsLock)
                {
                    if (BackgroundExceptions.Count == 0)
                    {
                        break;
                    }
                    report = BackgroundExceptions.Pop();
                }
                if (report != null)
                {
                    // Use SendReport instead of Send because skip/rate-limit rules
                    // were already applied before the report entered the background queue.
                    SendReport(report);
                }
            }
        }

        private void OnDestroy()
        {
            Enabled = false;
            if (_breadcrumbs != null)
            {
                _breadcrumbs.FromMonoBehavior("Backtrace Client: OnDestroy", LogType.Warning, null);
                _breadcrumbs.UnregisterEvents();
            }
            _instance = null;
            RestoreUnityLogHandlerExceptionCapture();
            Application.logMessageReceived -= HandleUnityMessage;
            Application.logMessageReceivedThreaded -= HandleUnityBackgroundException;
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
            Application.lowMemory -= HandleLowMemory;
#endif
            if (_nativeClient != null)
            {
                _nativeClient.Disable();
            }
        }

        /// <summary>
        /// Change maximum number of reportrs sending per one minute
        /// </summary>
        /// <param name="reportPerMin">Number of reports sending per one minute. If value is equal to zero, there is no request sending to API. Value have to be greater than or equal to 0</param>
        public void SetClientReportLimit(uint reportPerMin)
        {
            if (!Enabled)
            {
                Debug.LogWarning("Please enable BacktraceClient first.");
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
            if (_breadcrumbs != null)
            {
                _breadcrumbs.FromBacktrace(report);
            }
            EnqueueBacktraceLog(report);
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
            if (_breadcrumbs != null)
            {
                _breadcrumbs.FromBacktrace(report);
            }
            EnqueueBacktraceLog(report);
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
            if (_breadcrumbs != null)
            {
                _breadcrumbs.FromBacktrace(report);
            }
            EnqueueBacktraceLog(report);
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
            if (!Enabled)
            {
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
        private IEnumerator CollectDataAndSend(BacktraceReport report, Action<BacktraceResult> sendCallback)
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

            bool databaseEnabled = Database != null && Database.Enabled();
            if (databaseEnabled)
            {
                yield return WaitForFrame.Wait();
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
                        record.Unlock();
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
            }
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
            yield return WaitForFrame.Wait();
            if (string.IsNullOrEmpty(json))
            {
                yield break;
            }

            //backward compatibility 
            if (RequestHandler != null)
            {
                yield return RequestHandler.Invoke(BacktraceApi.ServerUrl, data);
                yield break;
            }

            if (data.Deduplication != 0)
            {
                queryAttributes["_mod_duplicate"] = data.Deduplication.ToString(CultureInfo.InvariantCulture);
            }

#if UNITY_WEBGL
            // When the on-disk BacktraceDatabase is unavailable/disabled on WebGL, we persist the report before sending. 
            // This ensures that reports are not lost in cases where the browser is offline and the send callback never executes (tab close, crash, navigation).
            if (!databaseEnabled &&
                Configuration != null &&
                Configuration.Enabled &&
                RequestHandler == null &&
                Database is BacktraceDatabase webglDatabase)
            {
                webglDatabase.WebGLPersistBeforeSend(data, json);
            }
#endif

            StartCoroutine(BacktraceApi.Send(json, data.Attachments, queryAttributes, (BacktraceResult result) =>
            {
                if (record != null)
                {
                    record.Unlock();
                    if (databaseEnabled && (result.Status == BacktraceResultStatus.Ok || result.Status == BacktraceResultStatus.Empty))
                    {
                        Database.Delete(record);
                    }
                }

#if UNITY_WEBGL
                if (!databaseEnabled &&
                    Configuration != null &&
                    Configuration.Enabled &&
                    RequestHandler == null &&
                    Database is BacktraceDatabase webglDatabase)
                {
                    webglDatabase.WebGLHandleSendResult(data, result);
                }
#endif

                //check if there is more errors to send
                //handle inner exception
                HandleInnerException(report);

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

            // add environment information to backtrace report
            var sourceCode = GetBacktraceSourceCode(report);

            report.AssignSourceCodeToReport(sourceCode);
            // apply _mod fingerprint attribute when client should use
            // normalized exception message instead environment stack trace
            // for exceptions without stack trace.
            report.SetReportFingerprint(Configuration.UseNormalizedExceptionMessage);
            report.AttachmentPaths.AddRange(_clientReportAttachments);

            // pass copy of dictionary to prevent overriding client attributes
            var result = report.ToBacktraceData(null, GameObjectDepth);
            AttributeProvider.AddAttributes(result.Attributes.Attributes);

            return result;
        }

#if UNITY_ANDROID
        /// <summary>
        /// ANR Detection event. This method will be replaced by Backtrace-Android soon native API.
        /// </summary>
        /// <param name="stackTrace">Main thread stack trace</param>
        internal void OnAnrDetected(string stackTrace)
        {
            if (!Enabled)
            {
                Debug.LogWarning("Please enable BacktraceClient first.");
                return;
            }

            const string anrMessage = "ANRException: Blocked thread detected";
            var hang = new BacktraceUnhandledException(anrMessage, stackTrace);
            if (Breadcrumbs != null)
            {
                Breadcrumbs.FromMonoBehavior(anrMessage, LogType.Warning, new Dictionary<string, string> { { "stackTrace", stackTrace } });
            }
            var report = new BacktraceReport(hang);
            if (_useProguard) {
                report.UseSymbolication("proguard");
            }
            SendUnhandledExceptionReport(report);
        }

        /// <summary>
        /// Handle background exceptions with single exception message (that contains information about exception message and stack trace) 
        /// </summary>
        /// <param name="backgroundExceptionMessage">exception message</param>
        internal void HandleUnhandledExceptionsFromAndroidBackgroundThread(string backgroundExceptionMessage)
        {
            var splitIndex = backgroundExceptionMessage.IndexOf('\n');
            if (splitIndex == -1)
            {
                Debug.LogWarning(string.Format("Received incorrect background exception message. Message: {0}", backgroundExceptionMessage));
                return;
            }
            var message = backgroundExceptionMessage.Substring(0, splitIndex);
            var stackTrace = backgroundExceptionMessage.Substring(splitIndex);
            var report = new BacktraceReport(new BacktraceUnhandledException(message, stackTrace));
            if (_useProguard) {
                report.UseSymbolication("proguard");
            }
            
            if (Database != null)
            {
                var backtraceData = report.ToBacktraceData(null, GameObjectDepth);
                AttributeProvider.AddAttributes(backtraceData.Attributes.Attributes);
                Database.Add(backtraceData);
            }
            else
            {
                SendUnhandledExceptionReport(report);
            }
            var androidNativeClient = _nativeClient as Runtime.Native.Android.NativeClient;
            if (androidNativeClient != null)
            {
                androidNativeClient.FinishUnhandledBackgroundException();
            }
        }
#endif

        private bool IsCurrentThreadMainThread()
        {
            return _current == null ||
                Thread.CurrentThread.ManagedThreadId == _current.ManagedThreadId;
        }

        private bool ShouldUseUnityLogHandlerExceptionCapture()
        {
            if (Configuration == null)
            {
                return false;
            }
            if (Configuration.UnityLogHandlerExceptionCapture ==
                BacktraceUnityLogHandlerExceptionCaptureMode.Disabled)
            {
                return false;
            }
            if (Configuration.UnityLogHandlerExceptionCapture ==
                BacktraceUnityLogHandlerExceptionCaptureMode.Enabled)
            {
                return true;
            }
#if UNITY_WEBGL
            return true;
#else
            return false;
#endif
        }

        private void InstallUnityLogHandlerExceptionCapture()
        {
            if (!ShouldUseUnityLogHandlerExceptionCapture())
            {
                return;
            }
            var currentLogHandler = Debug.unityLogger.logHandler;
            if (currentLogHandler == null)
            {
                return;
            }
            var existingBacktraceHandler = currentLogHandler as BacktraceUnityLogHandler;
            if (existingBacktraceHandler != null)
            {
                _unityLogHandler = existingBacktraceHandler;
                _previousUnityLogHandler = existingBacktraceHandler.InnerLogHandler;
                return;
            }
            _previousUnityLogHandler = currentLogHandler;
            _unityLogHandler = new BacktraceUnityLogHandler(this, currentLogHandler);
            Debug.unityLogger.logHandler = _unityLogHandler;
        }

        private void RestoreUnityLogHandlerExceptionCapture()
        {
            if (_unityLogHandler == null)
            {
                return;
            }
            if (Debug.unityLogger.logHandler == _unityLogHandler &&
                _previousUnityLogHandler != null)
            {
                Debug.unityLogger.logHandler = _previousUnityLogHandler;
            }
            _unityLogHandler = null;
            _previousUnityLogHandler = null;
        }

        internal bool TryCaptureUnityLogHandlerException(
            Exception exception,
            UnityEngine.Object context)
        {
            if (!Enabled ||
                exception == null ||
                !Configuration.HandleUnhandledExceptions ||
                !ShouldUseUnityLogHandlerExceptionCapture())
            {
                return false;
            }
            if (BacktraceApi == null || _reportLimitWatcher == null)
            {
                return false;
            }
            var isMainThread = IsCurrentThreadMainThread();
            var contextName = GetUnityContextName(context, isMainThread);
            var attributes = BacktraceUnityLogCapture.CreateLogHandlerExceptionAttributes(
                exception,
                contextName,
                isMainThread);
            var report = new BacktraceReport(exception, attributes);
            report.Attributes["error.type"] = BacktraceDefaultClassifierTypes.UnhandledExceptionType;
            report.Attributes["error.message"] =
                BacktraceUnityLogCapture.NormalizeUnityExceptionMessage(exception);
            report.AddAnnotation(
                BacktraceUnityLogCapture.UnityLogHandlerExceptionAnnotationName,
                BacktraceUnityLogCapture.CreateLogHandlerExceptionAnnotation(
                    exception,
                    contextName));
            SendUnhandledExceptionReport(report);
            return true;
        }

        private string GetUnityContextName(UnityEngine.Object context, bool isMainThread)
        {
            if (!isMainThread || context == null)
            {
                return string.Empty;
            }
            try
            {
                return context.name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal void SuppressNextUnityLogReport(
            Exception exception,
            LogType type,
            int ttlMilliseconds = 1000)
        {
            var prefixes = BacktraceUnityLogCapture.CreateExceptionMessagePrefixes(exception);
            if (prefixes == null || prefixes.Count == 0)
            {
                return;
            }
            lock (_unityLogSuppressionLock)
            {
                _unityLogSuppressions.Enqueue(new UnityLogSuppression
                {
                    MessagePrefixes = prefixes,
                    Type = type,
                    ExpiresAtMs = DateTimeHelper.TimestampMs() + Math.Max(ttlMilliseconds, 1)
                });
            }
        }

        private bool ShouldSuppressUnityLogReport(string message, LogType type)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }
            lock (_unityLogSuppressionLock)
            {
                if (_unityLogSuppressions.Count == 0)
                {
                    return false;
                }
                var now = DateTimeHelper.TimestampMs();
                while (_unityLogSuppressions.Count > 0 &&
                       _unityLogSuppressions.Peek().ExpiresAtMs < now)
                {
                    _unityLogSuppressions.Dequeue();
                }
                var matched = false;
                var remaining = _unityLogSuppressions.Count;
                for (var i = 0; i < remaining; i++)
                {
                    var suppression = _unityLogSuppressions.Dequeue();
                    if (!matched &&
                        suppression.Type == type &&
                        MatchesAnyPrefix(message, suppression.MessagePrefixes))
                    {
                        matched = true;
                        continue;
                    }
                    _unityLogSuppressions.Enqueue(suppression);
                }
                return matched;
            }
        }

        private static bool MatchesAnyPrefix(string message, IList<string> prefixes)
        {
            if (string.IsNullOrEmpty(message) || prefixes == null)
            {
                return false;
            }
            for (var i = 0; i < prefixes.Count; i++)
            {
                var prefix = prefixes[i];
                if (!string.IsNullOrEmpty(prefix) &&
                    message.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private void EnqueueBacktraceLog(BacktraceReport report)
        {
            if (report == null || _backtraceLogManager == null)
            {
                return;
            }
            lock (_backtraceLogManagerLock)
            {
                _backtraceLogManager.Enqueue(report);
            }
        }

        private void EnqueueBacktraceLog(BacktraceUnityMessage unityMessage)
        {
            if (unityMessage == null || _backtraceLogManager == null)
            {
                return;
            }
            lock (_backtraceLogManagerLock)
            {
                _backtraceLogManager.Enqueue(unityMessage);
            }
        }

        private string GetBacktraceSourceCode(BacktraceReport report)
        {
            lock (_backtraceLogManagerLock)
            {
                return _backtraceLogManager.Disabled
                    ? new BacktraceUnityMessage(report).ToString()
                    : _backtraceLogManager.ToSourceCode();
            }
        }

        private BacktraceReport CreateUnityLogBacktraceReport(
            BacktraceUnhandledException exception,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
            var report = new BacktraceReport(exception, attributes);
            report.Attributes["error.message"] = message;
            report.AddAnnotation(
                BacktraceUnityLogCapture.UnityLogCaptureAnnotationName,
                BacktraceUnityLogCapture.CreateUnityLogAnnotation(
                    message,
                    stackTrace,
                    type,
                    isMainThread,
                    capturePath));
#if UNITY_WEBGL
            AttachWebGLJavaScriptStackIfNeeded(report, stackTrace, type);
#endif
            return report;
        }

#if UNITY_WEBGL
        private void AttachWebGLJavaScriptStackIfNeeded(
            BacktraceReport report,
            string unityStackTrace,
            LogType type)
        {
            if (report == null ||
                Configuration == null ||
                Configuration.WebGLJavaScriptStackFallback ==
                    BacktraceWebGLJavaScriptStackFallbackMode.Disabled)
            {
                return;
            }
            if (!BacktraceUnityLogCapture.IsStacklessUnityLogReport(unityStackTrace, type))
            {
                return;
            }
            var javascriptStack = BacktraceWebGLJavaScriptStack.Capture();
            var hasJavaScriptStack = !string.IsNullOrEmpty(javascriptStack);
            report.Attributes["backtrace.webgl.javascript_stack.present"] =
                BacktraceUnityLogCapture.ToInvariantString(hasJavaScriptStack);
            report.Attributes["backtrace.webgl.javascript_stack.kind"] =
                "javascript_stack_at_backtrace_capture_time";
            if (!hasJavaScriptStack)
            {
                return;
            }
            report.AddAnnotation(
                BacktraceWebGLJavaScriptStack.AnnotationName,
                BacktraceWebGLJavaScriptStack.CreateAnnotation(javascriptStack));
        }
#endif

        private bool ShouldSendUnhandledReport(
            BacktraceReport report,
            bool invokeSkipApi = true)
        {
            if (report == null)
            {
                return false;
            }
            var filterType = GetFilterTypeForUnhandledReport(report);
            if (invokeSkipApi &&
                ShouldSkipReport(filterType, report.Exception, string.Empty))
            {
                return false;
            }
            var shouldProcess = _reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp());
            if (shouldProcess)
            {
                if (!IsCurrentThreadMainThread())
                {
                    report.Attributes["exception.thread"] =
                        Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                    lock (_backgroundExceptionsLock)
                    {
                        BackgroundExceptions.Push(report);
                    }
                    return false;
                }
                return true;
            }
            if (OnClientReportLimitReached != null)
            {
                _onClientReportLimitReached.Invoke(report);
            }
            return false;
        }

        private static ReportFilterType GetFilterTypeForUnhandledReport(
            BacktraceReport report)
        {
            var unhandledException = report.Exception as BacktraceUnhandledException;
            if (unhandledException != null)
            {
                return unhandledException.Classifier == "ANRException"
                    ? ReportFilterType.Hang
                    : unhandledException.Type == LogType.Exception
                        ? ReportFilterType.UnhandledException
                        : ReportFilterType.Error;
            }
            string capturePath;
            if (report.Attributes != null &&
                report.Attributes.TryGetValue("backtrace.unity.capture_path", out capturePath) &&
                capturePath == BacktraceUnityLogCapture.CapturePathUnityLogHandlerLogException)
            {
                return ReportFilterType.UnhandledException;
            }
            return report.ExceptionTypeReport
                ? ReportFilterType.Exception
                : ReportFilterType.Message;
        }

        private Thread _current;

        /// <summary>
        /// Handle Unity unhandled exceptions
        /// </summary>
        private void CaptureUnityMessages()
        {
            _backtraceLogManager = new BacktraceLogManager(Configuration.NumberOfLogs);
            if (Configuration.HandleUnhandledExceptions)
            {
                InstallUnityLogHandlerExceptionCapture();
                Application.logMessageReceived += HandleUnityMessage;
                Application.logMessageReceivedThreaded += HandleUnityBackgroundException;
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
                Application.lowMemory += HandleLowMemory;
#endif
            }
        }

        internal void OnApplicationPause(bool pause)
        {
            if (_breadcrumbs != null)
            {
                _breadcrumbs.FromMonoBehavior("Application pause", LogType.Assert, new Dictionary<string, string> { { "paused", pause.ToString(CultureInfo.InvariantCulture).ToLower() } });
            }
            if (_nativeClient != null)
            {
                _nativeClient.PauseAnrThread(pause);
            }
        }

        internal void HandleUnityBackgroundException(string message, string stackTrace, LogType type)
        {
            if (Thread.CurrentThread == _current)
            {
                return;
            }
            HandleUnityMessage(
                message,
                stackTrace,
                type,
                false,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceivedThreaded);
        }

#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        internal void HandleLowMemory()
        {
            if (!Enabled)
            {
                Debug.LogWarning("Please enable BacktraceClient first.");
                return;
            }
            if (Configuration.OomReports && _nativeClient != null)
            {
                // inform native layer about oom error
                _nativeClient.OnOOM();

            }
        }
#endif

        /// <summary>
        /// Catch Unity logger data and create Backtrace reports for log type that represents exception or error
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="stackTrace">Log stack trace</param>
        /// <param name="type">log type</param>
        internal void HandleUnityMessage(string message, string stackTrace, LogType type)
        {
            HandleUnityMessage(
                message,
                stackTrace,
                type,
                IsCurrentThreadMainThread(),
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived);
        }

        private void HandleUnityMessage(
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            if (!Enabled)
            {
                return;
            }
            var unityMessage = new BacktraceUnityMessage(message, stackTrace, type);
            EnqueueBacktraceLog(unityMessage);

            if (!Configuration.HandleUnhandledExceptions)
            {
                return;
            }
            if (string.IsNullOrEmpty(message) ||
                (type != LogType.Error && type != LogType.Exception))
            {
                return;
            }
            if (ShouldSuppressUnityLogReport(message, type))
            {
                return;
            }
            BacktraceUnhandledException exception = null;
            var invokeSkipApi = true;
            if (type == LogType.Error)
            {
                if (Configuration.ReportFilterType.HasFlag(ReportFilterType.Error))
                {
                    return;
                }
                if (SamplingShouldSkip())
                {
                    if (SkipReport != null)
                    {
                        exception = new BacktraceUnhandledException(message, stackTrace)
                        {
                            Type = type
                        };
                        if (ShouldSkipReport(ReportFilterType.Error, exception, string.Empty))
                        {
                            return;
                        }
                        invokeSkipApi = false;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (exception == null)
            {
                exception = new BacktraceUnhandledException(message, stackTrace)
                {
                    Type = type
                };
            }
            var report = CreateUnityLogBacktraceReport(
                exception,
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
#if UNITY_ANDROID
            if (exception.NativeStackTrace && _useProguard)
            {
                report.UseSymbolication("proguard");
            }
#endif

            SendUnhandledExceptionReport(report, invokeSkipApi);
        }

        /// <summary>
        /// Skip sending report when sampling hit. This feature is enabled only for unhandled exception handler
        /// </summary>
        /// <returns>True, when client should skip report, otherwise false.</returns>
        private bool SamplingShouldSkip()
        {
            if (!Configuration || Configuration.Sampling == 1)
            {
                return false;
            }
            var value = Random.NextDouble();
            return value > Configuration.Sampling;
        }

        private void SendUnhandledExceptionReport(BacktraceReport report, bool invokeSkipApi = true)
        {
            if (report == null)
            {
                return;
            }
            if (OnUnhandledApplicationException != null && report.Exception != null)
            {
                OnUnhandledApplicationException.Invoke(report.Exception);
            }
            if (ShouldSendUnhandledReport(report, invokeSkipApi))
            {
                SendReport(report);
            }
        }

        private bool ShouldSendReport(Exception exception, List<string> attachmentPaths, Dictionary<string, string> attributes, bool invokeSkipApi = true)
        {
            // guess report type
            var filterType = ReportFilterType.Exception;
            if (exception is BacktraceUnhandledException)
            {
                var unhandledException = (exception as BacktraceUnhandledException);
                filterType = unhandledException.Classifier == "ANRException"
                    ? ReportFilterType.Hang
                    : unhandledException.Type == LogType.Exception ? ReportFilterType.UnhandledException : ReportFilterType.Error;
            }

            if (invokeSkipApi && ShouldSkipReport(filterType, exception, string.Empty))
            {
                return false;
            }

            //check rate limiting
            bool shouldProcess = _reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp());
            if (shouldProcess)
            {
                // This condition checks if we should send exception from current thread
                // if comparision result confirm that we're trying to send an exception from different
                // thread than main, we should add the exception object to the exception list 
                // and let update method send data to Backtrace.
                if (Thread.CurrentThread.ManagedThreadId != _current.ManagedThreadId)
                {
                    var report = new BacktraceReport(exception, attributes, attachmentPaths);
                    report.Attributes["exception.thread"] = Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                    lock (_backgroundExceptionsLock)
                    {
                        BackgroundExceptions.Push(report);
                    }
                    return false;
                }
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
            bool shouldProcess = _reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp());
            if (shouldProcess)
            {
                // This condition checks if we should send exception from current thread
                // if comparision result confirm that we're trying to send an exception from different
                // thread than main, we should add the exception object to the exception list 
                // and let update method send data to Backtrace.
                if (Thread.CurrentThread.ManagedThreadId != _current.ManagedThreadId)
                {
                    var report = new BacktraceReport(message, attributes, attachmentPaths);
                    report.Attributes["exception.thread"] = Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                    lock (_backgroundExceptionsLock)
                    {
                        BackgroundExceptions.Push(report);
                    }
                    return false;
                }
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
            bool shouldProcess = _reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp());
            if (shouldProcess)
            {
                // This condition checks if we should send exception from current thread
                // if comparision result confirm that we're trying to send an exception from different
                // thread than main, we should add the exception object to the exception list 
                // and let update method send data to Backtrace.
                if (Thread.CurrentThread.ManagedThreadId != _current.ManagedThreadId)
                {
                    report.Attributes["exception.thread"] = Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                    lock (_backgroundExceptionsLock)
                    {
                        BackgroundExceptions.Push(report);
                    }
                    return false;
                }
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
        private void HandleInnerException(BacktraceReport report)
        {
            var innerExceptionReport = report.CreateInnerReport();
            if (innerExceptionReport != null && ShouldSendReport(innerExceptionReport))
            {
                SendReport(innerExceptionReport);
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
            if (!Enabled)
            {
                return false;
            }

            return Configuration.ReportFilterType.HasFlag(type)
                || (SkipReport != null && SkipReport.Invoke(type, exception, message));

        }

        internal IList<string> GetNativeAttachments()
        {
            return _clientReportAttachments
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(System.IO.Path.GetFileName, StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        }
    }
}
