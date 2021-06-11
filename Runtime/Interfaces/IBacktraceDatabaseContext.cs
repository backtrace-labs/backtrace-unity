using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    public interface IBacktraceDatabaseContext : IDisposable
    {

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
        /// Get first record or null
        /// </summary>
        /// <returns>First existing record in database store</returns>
        BacktraceDatabaseRecord FirstOrDefault(Func<BacktraceDatabaseRecord, bool> predicate);

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
        [Obsolete("Please use Count method instead")]
        int GetTotalNumberOfRecords();

        /// <summary>
        /// Context deduplication strategy
        /// </summary>
        DeduplicationStrategy DeduplicationStrategy { get; set; }

        /// <summary>
        /// Returns path to files from last batch
        /// </summary>
        /// <returns>Path to files available in the last batch</returns>
        IEnumerable<BacktraceDatabaseRecord> GetRecordsToDelete();

        /// <summary>
        /// Get hash from backtrace data based on the database deduplication rules
        /// </summary>
        /// <param name="backtraceData">Backtrace diagnostic object</param>
        /// <returns>hash</returns>
        string GetHash(BacktraceData backtraceData);

        /// <summary>
        /// Returns database record based on the backtrace data hash
        /// </summary>
        /// <param name="hash">Diagnostic object hash</param>
        /// <returns>Record if record associated to the hash exists</returns>
        BacktraceDatabaseRecord GetRecordByHash(string hash);

        /// <summary>
        /// Add duplicate to database context
        /// </summary>
        /// <param name="record">Duplicated record</param>
        void AddDuplicate(BacktraceDatabaseRecord record);
    }
}