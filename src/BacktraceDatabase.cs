using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.IO;
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
                return BacktraceDatabaseContext?.DeduplicationStrategy ?? DeduplicationStrategy.None;
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
        /// Database path
        /// </summary>
        private string DatabasePath
        {
            get
            {
                return DatabaseSettings.DatabasePath;
            }
        }

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
            if (Configuration == null || !Configuration.IsValid())
            {
                Debug.LogWarning("Configuration doesn't exists or provided serverurl/token are invalid");
                Enable = false;
                return;
            }

            //setup database object
            DatabaseSettings = new BacktraceDatabaseSettings(Configuration);
            Enable = Configuration.Enabled && BacktraceConfiguration.ValidateDatabasePath(Configuration.DatabasePath);
            if (!Enable)
            {
                Debug.LogWarning("Cannot initialize database - invalid database configuration. Database is disabled");
                return;
            }
            CreateDatabaseDirectory();
            SetupMultisceneSupport();
            _lastConnection = Time.time;

            //Setup database context
            BacktraceDatabaseContext = new BacktraceDatabaseContext(DatabasePath, DatabaseSettings.RetryLimit, DatabaseSettings.RetryOrder, DatabaseSettings.DeduplicationStrategy);
            BacktraceDatabaseFileContext = new BacktraceDatabaseFileContext(DatabasePath, DatabaseSettings.MaxDatabaseSize, DatabaseSettings.MaxRecordCount);
            BacktraceApi = new BacktraceApi(Configuration.ToCredentials());
            _reportLimitWatcher = new ReportLimitWatcher(Convert.ToUInt32(Configuration.ReportPerMin));

        }
        
        /// <summary>
        /// Backtrace database on disable event
        /// </summary>
        public void OnDisable()
        {
            Debug.LogWarning("Disabling BacktraceDatabase integration");
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
        private void Update()
        {
            if (!Enable)
            {
                return;
            }
            if (Time.time - _lastConnection > DatabaseSettings.RetryInterval)
            {
                _lastConnection = Time.time;
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
            if (DatabaseSettings.AutoSendMode)
            {
                _lastConnection = Time.time;
            }
            // load reports from hard drive
            LoadReports();
            // remove orphaned files
            RemoveOrphaned();
            SendData(BacktraceDatabaseContext.FirstOrDefault());
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
            BacktraceDatabaseContext?.Clear();
            BacktraceDatabaseFileContext?.Clear();
        }

        /// <summary>
        /// Add new report to BacktraceDatabase
        /// </summary>
        public BacktraceDatabaseRecord Add(BacktraceReport backtraceReport, Dictionary<string, object> attributes, MiniDumpType miniDumpType = MiniDumpType.Normal)
        {
            if (!Enable || backtraceReport == null)
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
            var data = backtraceReport.ToBacktraceData(attributes);
            return BacktraceDatabaseContext.Add(data, miniDumpType);
        }


        /// <summary>
        /// Get all stored records in BacktraceDatabase
        /// </summary>
        /// <returns>All stored records in BacktraceDatabase</returns>
        public IEnumerable<BacktraceDatabaseRecord> Get()
        {
            return BacktraceDatabaseContext?.Get() ?? new List<BacktraceDatabaseRecord>();
        }

        /// <summary>
        /// Delete single record from database
        /// </summary>
        /// <param name="record">Record to delete</param>
        public void Delete(BacktraceDatabaseRecord record)
        {
            BacktraceDatabaseContext?.Delete(record);
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

        private void FlushRecord(BacktraceDatabaseRecord record)
        {
            if (record == null)
            {
                return;
            }
            var backtraceData = record.BacktraceData;
            Delete(record);
            if (backtraceData == null)
            {
                return;
            }
            StartCoroutine(
                BacktraceApi.Send(backtraceData, (BacktraceResult result) =>
                {
                    record = BacktraceDatabaseContext.FirstOrDefault();
                    FlushRecord(record);
                }));
        }

        private void SendData(BacktraceDatabaseRecord record)
        {
            var backtraceData = record?.BacktraceData;
            //check if report exists on hard drive 
            // to avoid situation when someone manually remove data
            if (backtraceData == null || backtraceData.Report == null)
            {
                Delete(record);
            }
            else
            {
                StartCoroutine(
                     BacktraceApi.Send(backtraceData, (BacktraceResult sendResult) =>
                     {
                         if (sendResult.Status == BacktraceResultStatus.Ok)
                         {
                             Delete(record);
                         }
                         else
                         {
                             record.Dispose();
                             BacktraceDatabaseContext.IncrementBatchRetry();
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
            if (Configuration.DestroyOnLoad == true)
            {
                return;
            }
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }


        /// <summary>
        /// Create database directory
        /// </summary>
        protected virtual void CreateDatabaseDirectory()
        {
            if (Configuration.CreateDatabase != true)
            {
                return;
            }
            if (string.IsNullOrEmpty(Configuration.DatabasePath))
            {
                Enable = false;
                throw new InvalidOperationException("Cannot create Backtrace datase directory. Database directory is null or empty");
            }
            Directory.CreateDirectory(Configuration.DatabasePath);
        }

        /// <summary>
        /// Load all records stored in database path
        /// </summary>
        private void LoadReports()
        {
            var files = BacktraceDatabaseFileContext.GetRecords();
            foreach (var file in files)
            {
                var record = BacktraceDatabaseRecord.ReadFromFile(file);
                if (record == null)
                {
                    continue;
                }
                record.DatabasePath(DatabasePath);
                if (!record.Valid())
                {
                    try
                    {
                        record.Delete();
                    }
                    catch (Exception)
                    {
                        Debug.LogWarning($"Cannot remove file from database. File name: {file.FullName}");
                    }
                    continue;
                }
                BacktraceDatabaseContext.Add(record);
                ValidateDatabaseSize();
                record.Dispose();
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
            if (BacktraceDatabaseContext.Count() + 1 > DatabaseSettings.MaxRecordCount && DatabaseSettings.MaxRecordCount != 0)
            {
                if (!BacktraceDatabaseContext.RemoveLastRecord())
                {
                    return false;
                }
            }

            //check database size. If database size == 0 then we ignore this condition
            //remove all records till database use enough space
            if (DatabaseSettings.MaxDatabaseSize != 0 && BacktraceDatabaseContext.GetSize() > DatabaseSettings.MaxDatabaseSize)
            {
                //if your database is entry or every record is locked
                //deletePolicyRetry avoid infinity loop
                int deletePolicyRetry = 5;
                while (BacktraceDatabaseContext.GetSize() > DatabaseSettings.MaxDatabaseSize)
                {
                    BacktraceDatabaseContext.RemoveLastRecord();
                    deletePolicyRetry--;
                    if (deletePolicyRetry != 0)
                    {
                        break;
                    }
                }
                return deletePolicyRetry != 0;
            }
            return true;
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

    }
}
