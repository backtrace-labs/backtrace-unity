using Backtrace.Unity.Types;
using System;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    [Serializable]
    public class BacktraceDatabaseConfiguration : BacktraceClientConfiguration
    {
        public bool Enabled = false;
        /// <summary>
        /// Directory path where reports and minidumps are stored
        /// </summary>
        public string DatabasePath;

        /// <summary>
        /// Resend report when http client throw exception
        /// </summary>
        public bool AutoSendMode = true;

        public bool CreateDatabase = false;

        public bool ValidDatabasePath()
        {
            if (string.IsNullOrEmpty(DatabasePath))
            {
                return false;
            }

            string databasePathCopy = DatabasePath;
            if (!Path.IsPathRooted(databasePathCopy))
            {
                databasePathCopy = Path.GetFullPath(Path.Combine(Application.dataPath, databasePathCopy));
            }
            Enabled = Directory.Exists(databasePathCopy);
            return true;
        }

        /// <summary>
        /// Maximum number of stored reports in Database. If value is equal to zero, then limit not exists
        /// </summary>
        public int MaxRecordCount;

        /// <summary>
        /// Database size in MB
        /// </summary>
        public long MaxDatabaseSize;

        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        public int RetryInterval = 60;

        /// <summary>
        /// Maximum number of retrie
        /// </summary>
        public int RetryLimit = 3;

        /// <summary>
        /// Retry order
        /// </summary>
        public RetryOrder RetryOrder;
    }
}