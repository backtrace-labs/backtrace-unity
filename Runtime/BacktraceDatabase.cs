using Backtrace.Unity.Common;
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.Storage;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        public BacktraceConfiguration Configuration;

        private BacktraceBreadcrumbs _breadcrumbs;

        /// <summary>
        /// Backtrace Breadcrumbs
        /// </summary>
        public IBacktraceBreadcrumbs Breadcrumbs
        {
            get
            {
                if (_breadcrumbs == null)
                {
                    if (Enable && Configuration.EnableBreadcrumbsSupport)
                    {
                        _breadcrumbs = new BacktraceBreadcrumbs(new BacktraceStorageLogManager(Configuration.GetFullDatabasePath()));
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
            if (Configuration == null)
            {
                Configuration = GetComponent<BacktraceClient>().Configuration;
            }
            if (Instance != null)
            {
                return;
            }
            if (Configuration == null || !Configuration.IsValid())
            {
                Enable = false;
                return;
            }

#if UNITY_SWITCH
            Enable = false;
#else
            Enable = Configuration.Enabled && InitializeDatabasePaths();
#endif
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
            SetupMultisceneSupport();
            _lastConnection = Time.unscaledTime;
            LastFrameTime = Time.unscaledTime;
            //Setup database context
            BacktraceDatabaseContext = new BacktraceDatabaseContext(DatabaseSettings);
            BacktraceDatabaseFileContext = new BacktraceDatabaseFileContext(DatabaseSettings);
            BacktraceApi = new BacktraceApi(Configuration.ToCredentials());
            _reportLimitWatcher = new ReportLimitWatcher(Convert.ToUInt32(Configuration.ReportPerMin));
        }

        /// <summary>
        /// Backtrace database on disable event
        /// </summary>
        public void OnDisable()
        {
            Enable = false;
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
            if (!Enable)
            {
                return;
            }
            _breadcrumbs?.Update();
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
            if (!Enable)
            {
                return;
            }
            // load reports from hard drive
            LoadReports();
            // remove orphaned files
            RemoveOrphaned();
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

            if (BacktraceDatabaseContext != null)
            {
                BacktraceDatabaseFileContext.Clear();
            }
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
                ? new System.Diagnostics.Stopwatch()
                : System.Diagnostics.Stopwatch.StartNew();

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
               : new System.Diagnostics.Stopwatch();

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
                         if (sendResult.Status != BacktraceResultStatus.ServerError && sendResult.Status != BacktraceResultStatus.NetworkError)
                         {
                             Delete(record);
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
                Debug.LogWarning("Backtrace database path is empty.");
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
        protected virtual void LoadReports()
        {
            if (!Enable)
            {
                return;
            }
            var files = BacktraceDatabaseFileContext.GetRecords();
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

        public bool EnableBreadcrumbsSupport()
        {
            return Breadcrumbs?.EnableBreadcrumbs(Configuration.BacktraceBreadcrumbsLevel, Configuration.LogLevel) ?? false;
        }
    }
}
