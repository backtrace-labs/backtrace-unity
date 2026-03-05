using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.Storage;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Runtime.Native;
using Backtrace.Unity.Services;
using Backtrace.Unity.WebGL;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace Backtrace.Unity
{
    /// <summary>
    /// Backtrace Database 
    /// </summary>
    [RequireComponent(typeof(BacktraceClient))]
    public class BacktraceDatabase : MonoBehaviour, IBacktraceDatabase
    {
        private bool _timerBackgroundWork = false;

#if UNITY_WEBGL
        private WebGLOfflineDatabase _webglOfflineDatabase;
        private Coroutine _webglOfflineReplayCoroutine;
        private bool _webglOfflineReplayInProgress;
        private float _webglLastReplayTime;
        private const float WebGLInflightTimeoutSeconds = 30f;
        private readonly Dictionary<string, float> _webglInflightUuids = new Dictionary<string, float>();
#endif

        public BacktraceConfiguration Configuration;

        private BacktraceBreadcrumbs _breadcrumbs;

        private BacktraceClient _client;
        /// <summary>
        /// Backtrace Breadcrumbs
        /// </summary>
        public IBacktraceBreadcrumbs Breadcrumbs
        {
            get
            {
                if (_breadcrumbs == null)
                {
                    if (Enable && Configuration.EnableBreadcrumbsSupport && BacktraceBreadcrumbs.CanStoreBreadcrumbs(Configuration.LogLevel, Configuration.BacktraceBreadcrumbsLevel))
                    {
                        _breadcrumbs = new BacktraceBreadcrumbs(
                            new BacktraceStorageLogManager(Configuration.GetFullDatabasePath()),
                            Configuration.BacktraceBreadcrumbsLevel,
                            Configuration.LogLevel);
                    }
                }
                return _breadcrumbs;
            }
        }

        internal static float LastFrameTime = 0;

        /// <summary>
        /// Internal database path 
        /// </summary>
        public string DatabasePath { get; protected set; }

        /// <summary>
        /// Attachment support: Screenshot quality
        /// </summary>
        public int ScreenshotQuality
        {
            get
            {
                return BacktraceDatabaseFileContext.ScreenshotQuality;
            }
            set
            {
                BacktraceDatabaseFileContext.ScreenshotQuality = value;
            }
        }

        /// <summary>
        /// Attachment support: Screenshot max height - based on screenshot max height, algorithm calculates
        /// ratio, that allows to calculate screenshot max width
        /// </summary>
        public int ScreenshotMaxHeight
        {
            get
            {
                return BacktraceDatabaseFileContext.ScreenshotMaxHeight;
            }
            set
            {
                BacktraceDatabaseFileContext.ScreenshotMaxHeight = value;
            }
        }


        /// <summary>
        /// Backtrace database instance.
        /// </summary>
        private static BacktraceDatabase _instance;

        /// <summary>
        ///  Backtrace database instance accessor. Please use this property to access
        ///  BacktraceDatabase instance from other scene. This property will return value only
        ///  when you mark option "DestroyOnLoad" to false.
        /// </summary>
        public static BacktraceDatabase Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Backtrace Database deduplication strategy
        /// </summary>
        public DeduplicationStrategy DeduplicationStrategy
        {
            get
            {
                if (BacktraceDatabaseContext == null)
                    return DeduplicationStrategy.None;

                return BacktraceDatabaseContext.DeduplicationStrategy;
            }
            set
            {
                if (!Enable)
                {
                    throw new InvalidOperationException("Backtrace Database is disabled");
                }
                BacktraceDatabaseContext.DeduplicationStrategy = value;
            }
        }
        /// <summary>
        /// Database settings
        /// </summary>
        protected BacktraceDatabaseSettings DatabaseSettings { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        private float _lastConnection;

        /// <summary>
        /// Backtrace Api instance. Use BacktraceApi to send data to Backtrace server
        /// </summary>
        public IBacktraceApi BacktraceApi { get; set; }

        /// <summary>
        /// Database context - in memory cache and record operations
        /// </summary>
        protected virtual IBacktraceDatabaseContext BacktraceDatabaseContext { get; set; }

        /// <summary>
        /// File context - file collection operations
        /// </summary>
        internal IBacktraceDatabaseFileContext BacktraceDatabaseFileContext { get; set; }


        /// <summary>
        /// Determine if BacktraceDatabase is enable and library can store reports
        /// </summary>
        public bool Enable { get; private set; }


        /// <summary>
        /// Reload Backtrace database configuration. Reloading configuration is required, when you change 
        /// BacktraceDatabase configuration options.
        /// </summary>
        public void Reload()
        {
            // validate configuration
            if (_client == null)
            {
                _client = GetComponent<BacktraceClient>();
            }
            if (Configuration == null && _client != null)
            {
                Configuration = _client.Configuration;
            }

            // Multi-scene support if another persistent instance already exists, we disable this instance.
            if (Instance != null && Instance != this)
            {
                Enable = false;
                return;
            }

            if (Configuration == null || !Configuration.IsValid())
            {
                Enable = false;
                return;
            }

            // We keep a report-limit watcher even if the on-disk database cannot be initialized.
            var reportPerMin = Configuration.ReportPerMin < 0 ? 0 : Configuration.ReportPerMin;
            if (_reportLimitWatcher == null)
            {
                _reportLimitWatcher = new ReportLimitWatcher(Convert.ToUInt32(reportPerMin));
            }
            else
            {
                _reportLimitWatcher.SetClientReportLimit(Convert.ToUInt32(reportPerMin));
            }

#if UNITY_WEBGL
            // Install browser lifecycle hooks to flush IDBFS.
            BacktraceWebGLSync.TryInstallPageLifecycleHooks();

            // Initialize the PlayerPrefs fallback queue.
            EnsureWebGLOfflineDatabase();
#endif

#if UNITY_SWITCH
            Enable = false;
#else
            Enable = Configuration.Enabled && InitializeDatabasePaths();
#endif

            // Multi-scene support follows the configuration scope whenever offline persistence is enabled.
            if (Configuration.Enabled)
            {
                SetupMultisceneSupport();
            }

            if (!Enable)
            {
                if (Configuration.Enabled)
                {
                    Debug.LogWarning("Cannot initialize database - invalid path to database. Database is disabled");
                }
                return;
            }

            //setup database object
            DatabaseSettings = new BacktraceDatabaseSettings(DatabasePath, Configuration);
            _lastConnection = Time.unscaledTime;
            LastFrameTime = Time.unscaledTime;
            //Setup database context
            BacktraceDatabaseContext = new BacktraceDatabaseContext(DatabaseSettings);
            BacktraceDatabaseFileContext = new BacktraceDatabaseFileContext(DatabaseSettings);
            if (BacktraceApi == null)
            {
                BacktraceApi = new BacktraceApi(Configuration.ToCredentials());
            }
        }

        /// <summary>
        /// Backtrace database on disable event
        /// </summary>
        public void OnDisable()
        {
            Enable = false;

#if UNITY_WEBGL
            StopWebGLOfflineReplay();
            BacktraceWebGLSync.TrySyncFileSystem(true);
#endif
        }

        /// <summary>
        /// Backtrace database awake event
        /// </summary>
        private void Awake()
        {
            Reload();
        }

        /// <summary>
        /// Backtrace database update event
        /// </summary>
        internal void Update()
        {
#if UNITY_WEBGL
            TickWebGLSupport();
#endif

            if (!Enable)
            {
                return;
            }
            if (_breadcrumbs != null)
            {
                _breadcrumbs.Update(Time.unscaledTime);
            }
            LastFrameTime = Time.unscaledTime;
            if (!DatabaseSettings.AutoSendMode)
            {
                return;
            }

            if (Time.unscaledTime - _lastConnection > DatabaseSettings.RetryInterval)
            {
                _lastConnection = Time.unscaledTime;
                if (_timerBackgroundWork || !BacktraceDatabaseContext.Any())
                {
                    return;
                }

                _timerBackgroundWork = true;
                SendData(BacktraceDatabaseContext.FirstOrDefault());
                _timerBackgroundWork = false;
            }
        }

        private void Start()
        {
#if UNITY_WEBGL
            // Flush persisted WebGL offline reports as soon as the database component starts.
            TickWebGLSupport(forceImmediate: true);
#endif

            if (!Enable)
            {
                return;
            }
            string breadcrumbPath = string.Empty;
            string breadcrumbArchive = string.Empty;

            if (Breadcrumbs != null)
            {
                breadcrumbPath = Breadcrumbs.GetBreadcrumbLogPath();
                breadcrumbArchive = Breadcrumbs.Archive();
            }
            // load reports from hard drive
            LoadReports(breadcrumbPath, breadcrumbArchive);
            // remove orphaned files
            RemoveOrphaned();

            // send minidump files generated by unity engine or unity game, not captured by Windows native integration
            // this integration should start before native integration and before breadcrumbs integration
            // to allow algorithm to send breadcrumbs file - if the breadcrumb file is available
#if UNITY_STANDALONE_WIN

            var isEnabled = Enable && Configuration.SendUnhandledGameCrashesOnGameStartup && isActiveAndEnabled;
            var hasNativeConfiguration = _client && _client.NativeClient != null && _client.NativeClient is IStartupMinidumpSender;
            if (isEnabled && hasNativeConfiguration)
            {
                var client = _client.NativeClient as IStartupMinidumpSender;
                var attachments = _client.GetNativeAttachments();
                if(!string.IsNullOrEmpty(breadcrumbArchive))
                {
                    attachments.Add(breadcrumbArchive);
                }
                StartCoroutine(client.SendMinidumpOnStartup(
                        clientAttachments: attachments,
                        backtraceApi: BacktraceApi));
            }
#endif
            // enable breadcrumb support after finishing loading reports
            EnableBreadcrumbsSupport();
            if (DatabaseSettings.AutoSendMode)
            {
                _lastConnection = Time.unscaledTime;
                SendData(BacktraceDatabaseContext.FirstOrDefault());
            }
        }

        /// <summary>
        /// Set BacktraceApi instance
        /// </summary>
        /// <param name="backtraceApi">BacktraceApi instance</param>
        public void SetApi(IBacktraceApi backtraceApi)
        {
            BacktraceApi = backtraceApi;
        }

        /// <summary>
        /// Validate if BacktraceDatabase is enabled
        /// </summary>
        /// <returns>true if BacktraceDatabase is enabled. Otherwise false.</returns>
        public bool Enabled()
        {
            return Enable;
        }

        /// <summary>
        /// Get settings 
        /// </summary>
        /// <returns>Current database settings</returns>
        public BacktraceDatabaseSettings GetSettings()
        {
            return DatabaseSettings;
        }


        /// <summary>
        /// Delete all existing files and directories in current database directory
        /// </summary>
        public void Clear()
        {
            if (BacktraceDatabaseContext != null)
            {
                BacktraceDatabaseContext.Clear();
            }

            if (BacktraceDatabaseFileContext != null)
            {
                BacktraceDatabaseFileContext.Clear();
            }

#if UNITY_WEBGL
            BacktraceWebGLSync.TrySyncFileSystem(true);
#endif
        }

        /// <summary>
        /// Add new report to BacktraceDatabase
        /// </summary>
        public BacktraceDatabaseRecord Add(BacktraceData data, bool @lock = true)
        {
            if (data == null || !Enable)
            {
                return null;
            }
            //remove old reports (if database is full)
            //and check database health state
            var validationResult = ValidateDatabaseSize();
            if (!validationResult)
            {
                return null;
            }

            // validate if record already exists in the database object
            var hash = BacktraceDatabaseContext.GetHash(data);
            if (!string.IsNullOrEmpty(hash))
            {
                var existingRecord = BacktraceDatabaseContext.GetRecordByHash(hash);
                if (existingRecord != null)
                {
                    BacktraceDatabaseContext.AddDuplicate(existingRecord);
                    return existingRecord;
                }
            }

            //add built-in attachments
            var attachments = BacktraceDatabaseFileContext.GenerateRecordAttachments(data);
            for (int attachmentIndex = 0; attachmentIndex < attachments.Count(); attachmentIndex++)
            {
                if (!string.IsNullOrEmpty(attachments.ElementAt(attachmentIndex)))
                {
                    data.Attachments.Add(attachments.ElementAt(attachmentIndex));
                }
            }
            // add to fresh new record breadcrumb attachment
            if (Breadcrumbs != null)
            {
                data.Attachments.Add(Breadcrumbs.GetBreadcrumbLogPath());
                data.Attributes.Attributes["breadcrumbs.lastId"] = Breadcrumbs.BreadcrumbId().ToString("F0", CultureInfo.InvariantCulture);
            }

            // now we now we're adding new unique report to database
            var record = new BacktraceDatabaseRecord(data)
            {
                Hash = hash
            };

            // save record on the hard drive and add it to database context
            var saveResult = BacktraceDatabaseFileContext.Save(record);
            if (!saveResult)
            {
                // file context won't remove json object that wasn't stored in the previous method
                // but will clean up attachments associated with this record.
                BacktraceDatabaseFileContext.Delete(record);
                return null;
            }

            BacktraceDatabaseContext.Add(record);
            if (!@lock)
            {
                record.Unlock();
            }

#if UNITY_WEBGL
            BacktraceWebGLSync.TrySyncFileSystem();
#endif

            return record;
        }

        /// <summary>
        /// Add new report to BacktraceDatabase
        /// </summary>
        [Obsolete("Please use Add method with Backtrace data parameter instead")]
        public BacktraceDatabaseRecord Add(BacktraceReport backtraceReport, Dictionary<string, string> attributes, MiniDumpType miniDumpType = MiniDumpType.None)
        {
            if (!Enable || backtraceReport == null)
            {
                return null;
            }
            var data = backtraceReport.ToBacktraceData(attributes, Configuration.GameObjectDepth);
            return Add(data);
        }


        /// <summary>
        /// Get all stored records in BacktraceDatabase
        /// </summary>
        /// <returns>All stored records in BacktraceDatabase</returns>
        public IEnumerable<BacktraceDatabaseRecord> Get()
        {
            if (BacktraceDatabaseContext != null)
                return BacktraceDatabaseContext.Get();

            return new List<BacktraceDatabaseRecord>();
        }

        /// <summary>
        /// Delete single record from database
        /// </summary>
        /// <param name="record">Record to delete</param>
        public void Delete(BacktraceDatabaseRecord record)
        {
            if (BacktraceDatabaseContext != null)
            {
                BacktraceDatabaseContext.Delete(record);
            }
            if (BacktraceDatabaseFileContext != null)
            {
                BacktraceDatabaseFileContext.Delete(record);
            }

#if UNITY_WEBGL
            BacktraceWebGLSync.TrySyncFileSystem();
#endif
        }

        /// <summary>
        /// Send and delete all records from database
        /// </summary>
        public void Flush()
        {
            if (!Enable || !BacktraceDatabaseContext.Any())
            {
                return;
            }
            FlushRecord(BacktraceDatabaseContext.FirstOrDefault());
        }

        /// <summary>
        /// Try to send all data from database
        /// </summary>
        public void Send()
        {
            if (!Enable || !BacktraceDatabaseContext.Any())
            {
                return;
            }

            SendData(BacktraceDatabaseContext.FirstOrDefault());
        }

        private void FlushRecord(BacktraceDatabaseRecord record)
        {
            if (record == null)
            {
                return;
            }
            var stopWatch = Configuration.PerformanceStatistics
                ? System.Diagnostics.Stopwatch.StartNew()
                : null;

            var backtraceData = record.BacktraceDataJson();
            Delete(record);
            var queryAttributes = new Dictionary<string, string>();
            if (Configuration.PerformanceStatistics)
            {
                stopWatch.Stop();
                queryAttributes["performance.database.flush"] = stopWatch.GetMicroseconds();
            }

            if (backtraceData == null)
            {
                return;
            }

            queryAttributes["_mod_duplicate"] = record.Count.ToString(CultureInfo.InvariantCulture);

            StartCoroutine(
                BacktraceApi.Send(backtraceData, record.Attachments, queryAttributes, (BacktraceResult result) =>
                {
                    record = BacktraceDatabaseContext.FirstOrDefault();
                    FlushRecord(record);
                }));
        }

        private void SendData(BacktraceDatabaseRecord record)
        {
            if (record == null)
            {
                return;
            }
            var stopWatch = Configuration.PerformanceStatistics
               ? System.Diagnostics.Stopwatch.StartNew()
               : null;

            var backtraceData = record != null ? record.BacktraceDataJson() : null;
            //check if report exists on hard drive 
            // to avoid situation when someone manually remove data
            if (string.IsNullOrEmpty(backtraceData))
            {
                Delete(record);
            }
            else
            {
                var queryAttributes = new Dictionary<string, string>();
                if (Configuration.PerformanceStatistics)
                {
                    stopWatch.Stop();
                    queryAttributes["performance.database.send"] = stopWatch.GetMicroseconds();
                }
                queryAttributes["_mod_duplicate"] = record.Count.ToString(CultureInfo.InvariantCulture);

                StartCoroutine(
                     BacktraceApi.Send(backtraceData, record.Attachments, queryAttributes, (BacktraceResult sendResult) =>
                     {
                         record.Unlock();
                         // Only delete the record when the send was successful.
                         // For errors and rate limiting, we keep the record for retry.
                         if (sendResult != null && (sendResult.Status == BacktraceResultStatus.Ok || sendResult.Status == BacktraceResultStatus.Empty))
                         {
                             Delete(record);
                         }
                         else if (sendResult != null && sendResult.Status == BacktraceResultStatus.LimitReached)
                         {
                             // Server rate-limited here, we keep the record and stop this cycle.
                             // We do NOT count this against the retry limit.
                             return;
                         }
                         else
                         {
                             IncrementBatchRetry();
                             return;
                         }
                         bool shouldProcess = _reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp());
                         if (!shouldProcess)
                         {
                             return;
                         }
                         record = BacktraceDatabaseContext.FirstOrDefault();
                         SendData(record);
                     }));
            }
        }

        /// <summary>
        /// Get total number of records in database
        /// </summary>
        /// <returns>Total number of records</returns>
        public int Count()
        {
            return BacktraceDatabaseContext.Count();
        }

        /// <summary>
        /// Detect all orphaned minidump and files
        /// </summary>
        protected virtual void RemoveOrphaned()
        {
            var records = BacktraceDatabaseContext.Get();
            BacktraceDatabaseFileContext.RemoveOrphaned(records);
        }

        /// <summary>
        /// Setup multiscene support
        /// </summary>
        protected virtual void SetupMultisceneSupport()
        {
            if (Configuration.DestroyOnLoad)
            {
                return;
            }
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }

        /// <summary>
        /// Validate database directory. 
        /// This method will try to create database directory if "CreateDatabase" option is set tot true.
        /// </summary>
        /// <returns>Success when directory exists, otherwise false</returns>
        protected virtual bool InitializeDatabasePaths()
        {
            if (!Configuration.Enabled)
            {
                return false;
            }
            DatabasePath = Configuration.GetFullDatabasePath();
            if (string.IsNullOrEmpty(DatabasePath))
            {
                Debug.LogWarning("Backtrace database path is empty or unavailable.");
                return false;
            }

            var databaseDirExists = Directory.Exists(DatabasePath);

            // handle situation when Backtrace plugin should create database directory
            if (!databaseDirExists && Configuration.CreateDatabase)
            {
                try
                {
                    var dirInfo = Directory.CreateDirectory(DatabasePath);
                    databaseDirExists = dirInfo.Exists;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            if (!databaseDirExists)
            {
                Debug.LogWarning(string.Format("Backtrace database path doesn't exist. Database path: {0}", DatabasePath));

            }
            return databaseDirExists;

        }

        /// <summary>
        /// Load all records stored in database path
        /// </summary>
        protected virtual void LoadReports(string breadcrumbPath, string breadcrumbArchive)
        {
            if (!Enable)
            {
                return;
            }
            var files = BacktraceDatabaseFileContext.GetRecords().ToArray();
            if (files.Length == 0)
            {
                return;
            }
            var shouldUseArchiveBreadcrumbArchive = !string.IsNullOrEmpty(breadcrumbArchive);
            
            foreach (var file in files)
            {
                var record = BacktraceDatabaseRecord.ReadFromFile(file);
                if (record == null)
                {
                    continue;
                }
                if (!BacktraceDatabaseFileContext.IsValidRecord(record))
                {
                    BacktraceDatabaseFileContext.Delete(record);
                    continue;
                }

                // Use always the breadcrumb archive instead of the old breadcrumb file.
                if (shouldUseArchiveBreadcrumbArchive)
                {
                    bool replacementResult = record.Attachments.Remove(breadcrumbPath);
                    if (replacementResult)
                    {
                        record.Attachments.Add(breadcrumbArchive);
                    }
                }

                BacktraceDatabaseContext.Add(record);
                ValidateDatabaseSize();
                record.Unlock();
            }
        }
        /// <summary>
        /// Validate database size - check how many records are stored 
        /// in database and how much records need space.
        /// If space or number of records are invalid
        /// database will remove old reports
        /// </summary>
        private bool ValidateDatabaseSize()
        {
            //check how many records are stored in database
            //remove in case when we want to store one more than expected number
            //If record count == 0 then we ignore this condition
            var noMoreSpaceForReport = ReachedMaximumNumberOfRecords();

            //check database size. If database size == 0 then we ignore this condition
            //remove all records till database use enough space
            var noMoreSpace = ReachedDiskSpaceLimit();
            if (noMoreSpaceForReport || noMoreSpace)
            {
                //if your database is entry or every record is locked
                //deletePolicyRetry avoid infinity loop
                int deletePolicyRetry = 5;
                while (ReachedDiskSpaceLimit() || ReachedMaximumNumberOfRecords())
                {
                    var lastRecord = BacktraceDatabaseContext.LastOrDefault();
                    if (lastRecord != null)
                    {
                        BacktraceDatabaseContext.Delete(lastRecord);
                        BacktraceDatabaseFileContext.Delete(lastRecord);
                    }
                    deletePolicyRetry--;
                    if (deletePolicyRetry == 0)
                    {
                        break;
                    }
                }
                return deletePolicyRetry != 0;
            }
            return true;
        }

        private bool ReachedDiskSpaceLimit()
        {
            return DatabaseSettings.MaxDatabaseSize != 0 && BacktraceDatabaseContext.GetSize() > DatabaseSettings.MaxDatabaseSize;
        }

        private bool ReachedMaximumNumberOfRecords()
        {
            return BacktraceDatabaseContext.Count() + 1 > DatabaseSettings.MaxRecordCount && DatabaseSettings.MaxRecordCount != 0;
        }

        /// <summary>
        /// Valid database consistency requirements
        /// </summary>
        public bool ValidConsistency()
        {
            return BacktraceDatabaseFileContext.ValidFileConsistency();
        }

        /// <summary>
        /// Get database size
        /// </summary>
        /// <returns></returns>
        public long GetDatabaseSize()
        {
            return BacktraceDatabaseContext.GetSize();
        }

#if UNITY_WEBGL
        /// <summary>
        /// Persist the report to the WebGL PlayerPrefs-backed offline queue before sending.
        /// This avoids data loss when the send callback never executes (tab close, background, hard crash).
        /// </summary>
        internal void WebGLPersistBeforeSend(BacktraceData data, string json)
        {
            if (data == null || string.IsNullOrEmpty(json))
            {
                return;
            }

            if (Configuration == null || !Configuration.Enabled)
            {
                return;
            }

            EnsureWebGLOfflineDatabase();

            if (_webglOfflineDatabase == null)
            {
                return;
            }

            var uuid = data.Uuid.ToString();
            MarkWebGLInflight(uuid);

            // Avoid enqueueing the same UUID multiple times.
            if (_webglOfflineDatabase.Contains(uuid))
            {
                return;
            }

            _webglOfflineDatabase.Enqueue(data.Uuid, json, data.Attachments, data.Deduplication);
        }

        /// <summary>
        /// Update WebGL offline queue state after an immediate send attempt completes.
        /// </summary>
        internal void WebGLHandleSendResult(BacktraceData data, BacktraceResult result)
        {
            if (data == null)
            {
                return;
            }

            var uuid = data.Uuid.ToString();
            ClearWebGLInflight(uuid);

            if (Configuration == null || !Configuration.Enabled)
            {
                return;
            }

            EnsureWebGLOfflineDatabase();

            if (_webglOfflineDatabase == null)
            {
                return;
            }

            if (result != null && (result.Status == BacktraceResultStatus.Ok || result.Status == BacktraceResultStatus.Empty))
            {
                _webglOfflineDatabase.Remove(uuid);
                return;
            }

            // If the server is rate limiting us, we keep the record but don't count this against retry limit.
            if (result != null && result.Status == BacktraceResultStatus.LimitReached)
            {
                return;
            }

            // Count the attempt for other retryable failures.
            _webglOfflineDatabase.IncrementAttempts(uuid);
        }

        private void EnsureWebGLOfflineDatabase()
        {
            if (Configuration == null || !Configuration.Enabled)
            {
                return;
            }

            if (_webglOfflineDatabase == null)
            {
                _webglOfflineDatabase = new WebGLOfflineDatabase(Configuration);
                // Compact and enforce bounds once at initialization.
                _webglOfflineDatabase.Compact();
            }
        }

        private void TickWebGLSupport(bool forceImmediate = false)
        {
            if (Configuration == null || !Configuration.Enabled)
            {
                return;
            }

            // Browser lifecycle hooks to flush IDBFS.
            BacktraceWebGLSync.TryInstallPageLifecycleHooks();

            EnsureWebGLOfflineDatabase();
            CleanupWebGLInflight();

            if (_webglOfflineDatabase == null || _webglOfflineDatabase.IsEmpty)
            {
                return;
            }

            if (!Configuration.AutoSendMode)
            {
                return;
            }

            if (BacktraceApi == null)
            {
                return;
            }

            var retryInterval = Mathf.Max(1f, Configuration.RetryInterval);
            var now = Time.unscaledTime;

            if (forceImmediate)
            {
                _webglLastReplayTime = now - retryInterval;
            }

            if (_webglOfflineReplayInProgress)
            {
                return;
            }

            if (now - _webglLastReplayTime < retryInterval)
            {
                return;
            }

            _webglLastReplayTime = now;
            _webglOfflineReplayCoroutine = StartCoroutine(WebGLOfflineReplay());
        }

        private IEnumerator WebGLOfflineReplay()
        {
            if (_webglOfflineReplayInProgress)
            {
                yield break;
            }

            _webglOfflineReplayInProgress = true;

            try
            {
                if (_webglOfflineDatabase == null || BacktraceApi == null)
                {
                    yield break;
                }

                var retryLimit = Mathf.Max(1, Configuration.RetryLimit);
                var retryOrder = Configuration.RetryOrder;

                while (_webglOfflineDatabase.TryPeek(retryOrder, out var record))
                {
                    if (record == null)
                    {
                        _webglOfflineDatabase.Compact();
                        yield break;
                    }

                    // Records that exceeded retry limit.
                    if (record.attempts >= retryLimit)
                    {
                        _webglOfflineDatabase.Remove(record.uuid);
                        yield return null;
                        continue;
                    }

                    // Avoid sending the same report.
                    if (IsWebGLInflight(record.uuid))
                    {
                        yield break;
                    }

                    // Client-side report rate limiting.
                    if (_reportLimitWatcher != null && !_reportLimitWatcher.WatchReport(DateTimeHelper.Timestamp(), displayMessageOnLimitHit: false))
                    {
                        yield break;
                    }

                    var queryAttributes = new Dictionary<string, string>();
                    if (record.deduplication != 0)
                    {
                        queryAttributes["_mod_duplicate"] = record.deduplication.ToString(CultureInfo.InvariantCulture);
                    }

                    BacktraceResult sendResult = null;

                    MarkWebGLInflight(record.uuid);
                    yield return BacktraceApi.Send(
                        record.json,
                        record.attachments ?? Array.Empty<string>(),
                        queryAttributes,
                        result => sendResult = result);
                    ClearWebGLInflight(record.uuid);

                    if (sendResult != null && (sendResult.Status == BacktraceResultStatus.Ok || sendResult.Status == BacktraceResultStatus.Empty))
                    {
                        _webglOfflineDatabase.Remove(record.uuid);
                        yield return null;
                        continue;
                    }

                    // Rate limiting, we stop without consuming retry attempts.
                    if (sendResult != null && sendResult.Status == BacktraceResultStatus.LimitReached)
                    {
                        yield break;
                    }

                    // Re-try other failed status attempts for WebGL offline replay.
                    _webglOfflineDatabase.IncrementAttempts(record.uuid);
                    yield break;
                }
            }
            finally
            {
                _webglOfflineReplayInProgress = false;
                _webglOfflineReplayCoroutine = null;
            }
        }

        private void StopWebGLOfflineReplay()
        {
            if (_webglOfflineReplayCoroutine != null)
            {
                StopCoroutine(_webglOfflineReplayCoroutine);
                _webglOfflineReplayCoroutine = null;
            }

            _webglOfflineReplayInProgress = false;
            CleanupWebGLInflight(forceClearAll: true);
        }

        private void MarkWebGLInflight(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            _webglInflightUuids[uuid] = Time.unscaledTime;
        }

        private void ClearWebGLInflight(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            _webglInflightUuids.Remove(uuid);
        }

        private bool IsWebGLInflight(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return false;
            }

            if (!_webglInflightUuids.TryGetValue(uuid, out var since))
            {
                return false;
            }

            if (Time.unscaledTime - since > WebGLInflightTimeoutSeconds)
            {
                _webglInflightUuids.Remove(uuid);
                return false;
            }

            return true;
        }

        private void CleanupWebGLInflight(bool forceClearAll = false)
        {
            if (_webglInflightUuids.Count == 0)
            {
                return;
            }

            if (forceClearAll)
            {
                _webglInflightUuids.Clear();
                return;
            }

            var now = Time.unscaledTime;
            var stale = new List<string>();

            foreach (var kv in _webglInflightUuids)
            {
                if (now - kv.Value > WebGLInflightTimeoutSeconds)
                {
                    stale.Add(kv.Key);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                _webglInflightUuids.Remove(stale[i]);
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                BacktraceWebGLSync.TrySyncFileSystem(true);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                BacktraceWebGLSync.TrySyncFileSystem(true);
            }
        }

        private void OnApplicationQuit()
        {
            BacktraceWebGLSync.TrySyncFileSystem(true);
        }

        private void OnDestroy()
        {
            StopWebGLOfflineReplay();
            BacktraceWebGLSync.TrySyncFileSystem(true);
        }
#endif

        private ReportLimitWatcher _reportLimitWatcher;
        public void SetReportWatcher(ReportLimitWatcher reportLimitWatcher)
        {
            _reportLimitWatcher = reportLimitWatcher;
        }

        private void IncrementBatchRetry()
        {
            var data = BacktraceDatabaseContext.GetRecordsToDelete();
            BacktraceDatabaseContext.IncrementBatchRetry();
            if (data != null && data.Count() != 0)
            {
                foreach (var item in data)
                {
                    BacktraceDatabaseFileContext.Delete(item);
                }
            }
        }
        internal string GetBreadcrumbsPath()
        {
            if (_breadcrumbs == null)
            {
                return string.Empty;
            }
            return _breadcrumbs.GetBreadcrumbLogPath();
        }

        public bool EnableBreadcrumbsSupport()
        {
            if (Breadcrumbs == null)
            {
                return false;
            }
            return _breadcrumbs.EnableBreadcrumbs();
        }
    }
}
