using Backtrace.Unity.Types;
using System;

namespace Backtrace.Unity.Model.Database
{
    /// <summary>
    /// Backtrace library database settings
    /// </summary>
    public class BacktraceDatabaseSettings
    {
        private readonly BacktraceConfiguration _configuration;
        public BacktraceDatabaseSettings(string databasePath, BacktraceConfiguration configuration)
        {
            if (configuration == null || string.IsNullOrEmpty(databasePath))
            {
                return;
            }

            DatabasePath = databasePath;
            _configuration = configuration;
        }
        /// <summary>
        /// Directory path where reports and minidumps are stored
        /// </summary>
        public string DatabasePath
        {
            get; private set;
        }

        /// <summary>
        /// Maximum number of stored reports in Database. If value is equal to zero, then limit not exists
        /// </summary>
        public uint MaxRecordCount
        {
            get
            {
                return Convert.ToUInt32(_configuration.MaxRecordCount);
            }
        }

        /// <summary>
        /// Maximum database size in MB. If value is equal to zero, then size is unlimited
        /// </summary>
        public long MaxDatabaseSize
        {
            get
            {
                //convert megabyte to bytes
                return _configuration.MaxDatabaseSize * 1000 * 1000;
            }
        }

        /// <summary>
        /// Automatically send data from Backtrace database
        /// </summary>
        public bool AutoSendMode
        {
            get
            {
                return _configuration.AutoSendMode;
            }
        }

        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        public uint RetryInterval
        {
            get
            {
                return Convert.ToUInt32(_configuration.RetryInterval);
            }
        }

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        public uint RetryLimit
        {
            get
            {
                return Convert.ToUInt32(_configuration.RetryLimit);
            }
        }

        /// <summary>
        /// Deduplication strategy
        /// </summary>
        public DeduplicationStrategy DeduplicationStrategy
        {
            get
            {
                return _configuration.DeduplicationStrategy;
            }
        }

        /// <summary>
        /// Generate screenshot on exception
        /// </summary>
        public bool GenerateScreenshotOnException
        {
            get
            {
                return _configuration.GenerateScreenshotOnException;
            }
        }

        /// <summary>
        /// Add Unity log file to Backtrace report
        /// </summary>
        public bool AddUnityLogToReport
        {
            get
            {
#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
                return _configuration.AddUnityLogToReport;
#else
                return false;
#endif
            }
        }

        public RetryOrder RetryOrder
        {
            get
            {
                return _configuration.RetryOrder;
            }
        }

        public MiniDumpType MinidumpType
        {
            get
            {

#if UNITY_STANDALONE_WIN
                return _configuration.MinidumpType;
#else
                return MiniDumpType.None;

#endif
            }
        }
    }
}