using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    internal interface IBacktraceDatabaseContext : IDisposable
    {
        /// <summary>
        /// Add new record to Database
        /// </summary>
        /// <param name="backtraceData">Diagnostic data</param>
        BacktraceDatabaseRecord Add(BacktraceData backtraceData);

        /// <summary>
        /// Add new data to database
        /// </summary>
        /// <param name="backtraceDatabaseRecord">Database record</param>
        BacktraceDatabaseRecord Add(BacktraceDatabaseRecord backtraceDatabaseRecord);

        /// <summary>
        /// Get first record or null
        /// </summary>
        /// <returns>First existing record in database store</returns>
        BacktraceDatabaseRecord FirstOrDefault();

        /// <summary>
        /// Get last record or null
        /// </summary>
        /// <returns>Last existing record in database store</returns>
        BacktraceDatabaseRecord LastOrDefault();

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
        /// Check if any similar record exists
        /// </summary>
        /// <param name="n">Compared record</param>
        bool Any(BacktraceDatabaseRecord n);

        /// <summary>
        /// Check if any similar record exists
        /// </summary>
        bool Any();

        /// <summary>
        /// Get total count of records
        /// </summary>
        /// <returns>Total number of records</returns>
        int Count();

        /// <summary>
        /// Clear database
        /// </summary>
        void Clear();

        /// <summary>
        /// Increment record time for all records
        /// </summary>
        void IncrementBatchRetry();

        /// <summary>
        /// Get database size
        /// </summary>
        /// <returns>Database size</returns>
        long GetSize();

        /// <summary>
        /// Get total number of records stored in database
        /// </summary>
        /// <returns>Total number of records</returns>
        int GetTotalNumberOfRecords();

        /// <summary>
        /// Remove last record in database. 
        /// </summary>
        /// <returns>If algorithm can remove last record, method return true. Otherwise false</returns>
        bool RemoveLastRecord();
    }
}
