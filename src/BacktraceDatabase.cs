using Backtrace.Unity.Common;
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
    public class BacktraceDatabase : MonoBehaviour, IBacktraceDatabase
    {
        private bool _timerBackgroundWork = false;

        public BacktraceDatabaseConfiguration Configuration;

        /// <summary>
        /// Database settings
        /// </summary>
        private BacktraceDatabaseSettings DatabaseSettings { get; set; }

        private float _lastConnection = 0;
        /// <summary>
        /// Backtrace Api instance. Use BacktraceApi to send data to Backtrace server
        /// </summary>
        public IBacktraceApi BacktraceApi { get; set; }

        /// <summary>
        /// Database context - in memory cache and record operations
        /// </summary>
        internal IBacktraceDatabaseContext BacktraceDatabaseContext { get; set; }

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
        private bool _enable = false;

        private void Awake()
        {
            DatabaseSettings = new BacktraceDatabaseSettings(Configuration);
            if(Configuration == null)
            {
                return;
            }
            if (Configuration.CreateDatabase)
            {
                Directory.CreateDirectory(Configuration.DatabasePath);
                _enable = Configuration.ValidDatabasePath();
            }
            else
            {
                _enable = Configuration.Enabled;
            }

            if (!_enable)
            {
                return;
            }

            BacktraceDatabaseContext = new BacktraceDatabaseContext(DatabasePath, DatabaseSettings.RetryLimit, DatabaseSettings.RetryOrder);
            BacktraceDatabaseFileContext = new BacktraceDatabaseFileContext(DatabasePath, DatabaseSettings.MaxDatabaseSize, DatabaseSettings.MaxRecordCount);

            if (Configuration == null || !Configuration.IsValid())
            {
                Debug.Log("Configuration doesn't exists or provided serverurl/token are invalid");
                return;
            }
            BacktraceApi = new BacktraceApi(Configuration.ToCredentials(), Convert.ToUInt32(Configuration.ReportPerMin));
        }

        private void Update()
        {
            if (!_enable)
            {
                return;
            }
            if (Time.time - _lastConnection > DatabaseSettings.RetryInterval)
            {
                if (!BacktraceDatabaseContext.Any() || _timerBackgroundWork)
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
            if (!_enable)
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
            if (!_enable || backtraceReport == null)
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
            if (miniDumpType != MiniDumpType.None)
            {
                string minidumpPath = GenerateMiniDump(backtraceReport, miniDumpType);
                if (!string.IsNullOrEmpty(minidumpPath))
                {
                    backtraceReport.SetMinidumpPath(minidumpPath);
                }
            }

            var data = backtraceReport.ToBacktraceData(attributes);
            return BacktraceDatabaseContext.Add(data);
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
            if (!_enable || !BacktraceDatabaseContext.Any())
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
            //meanwhile someone delete data from a disk
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
                         }
                         record = BacktraceDatabaseContext.FirstOrDefault();
                         SendData(record);
                     }));
            }

        }

        /// <summary>
        /// Create new minidump file in database directory path. Minidump file name is a random Guid
        /// </summary>
        /// <param name="backtraceReport">Current report</param>
        /// <param name="miniDumpType">Generated minidump type</param>
        /// <returns>Path to minidump file</returns>
        private string GenerateMiniDump(BacktraceReport backtraceReport, MiniDumpType miniDumpType)
        {
            //note that every minidump file generated by app ends with .dmp extension
            //its important information if you want to clear minidump file
            string minidumpDestinationPath = Path.Combine(DatabaseSettings.DatabasePath, $"{backtraceReport.Uuid}-dump.dmp");
            MinidumpException minidumpExceptionType = backtraceReport.ExceptionTypeReport
                ? MinidumpException.Present
                : MinidumpException.None;

            bool minidumpSaved = MinidumpHelper.Write(
                filePath: minidumpDestinationPath,
                options: miniDumpType,
                exceptionType: minidumpExceptionType);

            return minidumpSaved
                ? minidumpDestinationPath
                : string.Empty;
        }

        /// <summary>
        /// Get total number of records in database
        /// </summary>
        /// <returns>Total number of records</returns>
        internal int Count()
        {
            return BacktraceDatabaseContext.Count();
        }

        /// <summary>
        /// Detect all orphaned minidump and files
        /// </summary>
        private void RemoveOrphaned()
        {
            var records = BacktraceDatabaseContext.Get();
            BacktraceDatabaseFileContext.RemoveOrphaned(records);
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
                if (!record.Valid())
                {
                    record.Delete();
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
    }
}
