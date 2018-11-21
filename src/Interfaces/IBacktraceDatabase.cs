using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    /// <summary>
    /// Backtrace Database Interface
    /// Before start: Be sure that used directory is empty!
    /// </summary>
    public interface IBacktraceDatabase
    {
        /// <summary>
        /// Start all database tasks - data storage, timers, file loading
        /// </summary>
        //void Start();

        /// <summary>
        /// Send all reports stored in BacktraceDatabase and clean database
        /// </summary>
        void Flush();

        void SetApi(IBacktraceApi backtraceApi);

        /// <summary>
        /// Remove all existing reports in BacktraceDatabase
        /// </summary>
        void Clear();

        /// <summary>
        /// Check all database consistency requirements
        /// </summary>
        /// <returns>True - if database has valid consistency requirements</returns>
        bool ValidConsistency();

        /// <summary>
        /// Add new report to Database
        /// </summary>
        BacktraceDatabaseRecord Add(BacktraceReport backtraceReport, Dictionary<string, object> attributes, MiniDumpType miniDumpType = MiniDumpType.Normal);

        /// <summary>
        /// Get all records stored in Database
        /// </summary>
        IEnumerable<BacktraceDatabaseRecord> Get();

        /// <summary>
        /// Delete database record by using BacktraceDatabaseRecord
        /// </summary>
        /// <param name="record">Database record</param>
        void Delete(BacktraceDatabaseRecord record);

        /// <summary>
        /// Get database settings
        /// </summary>
        /// <returns></returns>
        BacktraceDatabaseSettings GetSettings();

        /// <summary>
        /// Get database size
        /// </summary>
        long GetDatabaseSize();
    }
}
