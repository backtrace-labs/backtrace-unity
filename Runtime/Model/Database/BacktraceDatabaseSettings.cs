using Backtrace.Unity.Types;
using System;

namespace Backtrace.Unity.Model.Database
{
    /// <summary>
    /// Backtrace library database settings
    /// </summary>
    public class BacktraceDatabaseSettings
    {
        public BacktraceDatabaseSettings(BacktraceConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            DatabasePath = configuration.DatabasePath;
            MaxRecordCount = Convert.ToUInt32(configuration.MaxRecordCount);
            MaxDatabaseSize = configuration.MaxDatabaseSize;
            AutoSendMode = configuration.AutoSendMode;
            RetryInterval = Convert.ToUInt32(configuration.RetryInterval);
            RetryLimit = Convert.ToUInt32(configuration.RetryLimit);
            RetryOrder = configuration.RetryOrder;
            DeduplicationStrategy = configuration.DeduplicationStrategy;
            GenerateScreenshotOnException = configuration.GenerateScreenshotOnException;
        }
        /// <summary>
        /// Directory path where reports and minidumps are stored
        /// </summary>
        public string DatabasePath { get; private set; }

        /// <summary>
        /// Maximum number of stored reports in Database. If value is equal to zero, then limit not exists
        /// </summary>
        public uint MaxRecordCount { get; set; }

        /// <summary>
        /// Database size in MB
        /// </summary>
        private long _maxDatabaseSize = 0;

        /// <summary>
        /// Maximum database size in MB. If value is equal to zero, then size is unlimited
        /// </summary>
        public long MaxDatabaseSize
        {
            get
            {
                //convert megabyte to bytes
                return _maxDatabaseSize * 1000 * 1000;
            }
            set
            {
                _maxDatabaseSize = value;
            }
        }

        /// <summary>
        /// Resend report when http client throw exception
        /// </summary>
        public bool AutoSendMode { get; set; }

        /// <summary>
        /// Retry behaviour
        /// </summary>
        public RetryBehavior RetryBehavior = RetryBehavior.ByInterval;

        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        public uint RetryInterval = 5;

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        public uint RetryLimit = 3;

        /// <summary>
        /// Deduplication strategy
        /// </summary>
        public DeduplicationStrategy DeduplicationStrategy = DeduplicationStrategy.None;

        /// <summary>
        /// Generate screenshot on exception
        /// </summary>
        public bool GenerateScreenshotOnException;

        public RetryOrder RetryOrder = RetryOrder.Queue;
    }
}