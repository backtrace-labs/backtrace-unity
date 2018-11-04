using Backtrace.Unity.Types;
using System;
using System.IO;
using UnityEditor;
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

        public bool ValidDatabasePath()
        {
            if (string.IsNullOrEmpty(DatabasePath))
            {
                return false;
            }

            string databasePathCopy = DatabasePath;
            if (!Path.IsPathRooted(databasePathCopy))
            {
                Debug.Log(Application.dataPath);
                databasePathCopy = Path.GetFullPath(Path.Combine(Application.dataPath, databasePathCopy));
            }
            Enabled =  Directory.Exists(databasePathCopy);
            return Enabled;
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
        /// Resend report when http client throw exception
        /// </summary>
        public bool AutoSendMode = true;

        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        public int RetryInterval;

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        public int RetryLimit;

        /// <summary>
        /// Retry order
        /// </summary>
        public RetryOrder RetryOrder;
    }
}