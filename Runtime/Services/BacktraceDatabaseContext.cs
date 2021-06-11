using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Services
{
    /// <summary>
    /// Backtrace Database Context
    /// </summary>
    internal class BacktraceDatabaseContext : IBacktraceDatabaseContext
    {
        /// <summary>
        /// Database cache
        /// </summary>
        internal IDictionary<int, List<BacktraceDatabaseRecord>> BatchRetry { get; private set; } = new Dictionary<int, List<BacktraceDatabaseRecord>>();

        /// <summary>
        /// Total database size on hard drive
        /// </summary>
        internal long TotalSize = 0;

        /// <summary>
        /// Total records in BacktraceDatabase
        /// </summary>
        internal int TotalRecords = 0;

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        private readonly int _retryNumber;

        /// <summary>
        /// Record order
        /// </summary>
        internal RetryOrder RetryOrder { get; set; }

        /// <summary>
        /// Deduplicaiton strategy
        /// </summary>
        public DeduplicationStrategy DeduplicationStrategy { get; set; }

        /// <summary>
        /// Initialize new instance of Backtrace Database Context
        /// </summary>
        /// <param name="settings">Database settings</param>
        public BacktraceDatabaseContext(BacktraceDatabaseSettings settings)
        {
            _retryNumber = checked((int)settings.RetryLimit);
            RetryOrder = settings.RetryOrder;
            DeduplicationStrategy = settings.DeduplicationStrategy;
            SetupBatch();
        }

        /// <summary>
        /// Setup cache 
        /// </summary>
        private void SetupBatch()
        {
            if (_retryNumber == 0)
            {
                throw new ArgumentException(string.Format("{0} have to be greater than 0!", "_retryNumber"));
            }
            for (int i = 0; i < _retryNumber; i++)
            {
                BatchRetry[i] = new List<BacktraceDatabaseRecord>();
            }
        }

        /// <summary>
        /// Generate hash for current diagnostic data
        /// </summary>
        /// <param name="backtraceData">Diagnostic data </param>
        /// <returns>hash for current backtrace data</returns>
        public string GetHash(BacktraceData backtraceData)
        {
            var fingerprint = backtraceData == null ? string.Empty : backtraceData.Report.Fingerprint ?? string.Empty;
            if (!string.IsNullOrEmpty(fingerprint))
            {
                return fingerprint;
            }
            if (DeduplicationStrategy == DeduplicationStrategy.None)
            {
                return string.Empty;
            }

            var deduplicationModel = new DeduplicationModel(backtraceData, DeduplicationStrategy);
            return deduplicationModel.GetSha();
        }

        /// <summary>
        /// Returns record by record's hash
        /// </summary>
        /// <param name="hash">Hash associated to the record</param>
        /// <returns>Database record, if record with associated hash exists.</returns>
        public BacktraceDatabaseRecord GetRecordByHash(string hash)
        {
            for (int batchIndex = 0; batchIndex < BatchRetry.Count; batchIndex++)
            {
                for (int recordIndex = 0; recordIndex < BatchRetry[batchIndex].Count; recordIndex++)
                {
                    if (BatchRetry[batchIndex][recordIndex].Hash == hash)
                    {
                        var result = BatchRetry[batchIndex][recordIndex];
                        result.Locked = true;
                        return result;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Add existing record to database
        /// </summary>
        /// <param name="backtraceRecord">Database record</param>
        public BacktraceDatabaseRecord Add(BacktraceDatabaseRecord backtraceRecord)
        {
            if (backtraceRecord == null)
            {
                throw new NullReferenceException("BacktraceDatabaseRecord");
            }
            //lock record, because Add method returns record
            backtraceRecord.Locked = true;
            //increment total size of database
            TotalSize += backtraceRecord.Size;
            //add record to first batch
            BatchRetry[0].Add(backtraceRecord);
            //increment total records
            TotalRecords++;
            return backtraceRecord;
        }

        /// <summary>
        /// Check if any record exists
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public bool Any(BacktraceDatabaseRecord record)
        {
            return BatchRetry.SelectMany(n => n.Value).Any(n => n.Id == record.Id);
        }

        /// <summary>
        /// Check if any record exists
        /// </summary>
        public bool Any()
        {
            return TotalRecords != 0;
        }

        /// <summary>
        /// Delete existing record from database
        /// </summary>
        /// <param name="record">Database records to delete</param>
        public void Delete(BacktraceDatabaseRecord record)
        {
            if (record == null)
            {
                return;
            }
            for (int keyIndex = 0; keyIndex < BatchRetry.Keys.Count; keyIndex++)
            {
                var key = BatchRetry.Keys.ElementAt(keyIndex);
                for (int batchIndex = 0; batchIndex < BatchRetry[key].Count; batchIndex++)
                {
                    var value = BatchRetry[key].ElementAt(batchIndex);
                    if (value.Id == record.Id)
                    {
                        //delete value from current batch
                        BatchRetry[key].Remove(value);
                        //decrement all records
                        if (value.Count > 0)
                        {
                            TotalRecords = TotalRecords - value.Count;
                        }
                        else
                        {
                            TotalRecords--;
                        }
                        //decrement total size of database
                        TotalSize -= value.Size;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Increment retry time for current record
        /// </summary>
        public void IncrementBatchRetry()
        {
            RemoveMaxRetries();
            IncrementBatches();
        }

        /// <summary>
        /// Increment each batch
        /// </summary>
        private void IncrementBatches()
        {
            for (int i = _retryNumber - 2; i >= 0; i--)
            {
                var temp = BatchRetry[i];
                BatchRetry[i] = new List<BacktraceDatabaseRecord>();
                BatchRetry[i + 1] = temp;
            }
        }

        /// <summary>
        /// Remove last batch
        /// </summary>
        private void RemoveMaxRetries()
        {
            var currentBatch = BatchRetry[_retryNumber - 1];
            var total = currentBatch.Count;
            for (int i = 0; i < total; i++)
            {
                var value = currentBatch[i];
                if (value.Count > 0)
                {
                    TotalRecords = TotalRecords - value.Count;
                }
                else
                {
                    TotalRecords--;
                }
                TotalSize -= value.Size;
            }
        }

        /// <summary>
        /// Get all database records
        /// </summary>
        /// <returns>all existing database records</returns>
        public IEnumerable<BacktraceDatabaseRecord> Get()
        {
            return BatchRetry.SelectMany(n => n.Value);
        }

        /// <summary>
        /// Get total number of records in database
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            var result = 0;
            for (int batchIndex = 0; batchIndex < BatchRetry.Count; batchIndex++)
            {
                for (int reportIndex = 0; reportIndex < BatchRetry[batchIndex].Count; reportIndex++)
                {
                    result += BatchRetry[batchIndex][reportIndex].Count;
                }
            }
            return result;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            TotalRecords = 0;
            BatchRetry.Clear();
        }

        /// <summary>
        /// Delete all records from database
        /// </summary>
        public void Clear()
        {
            var records = BatchRetry.SelectMany(n => n.Value);
            TotalRecords = 0;
            TotalSize = 0;
            //clear all existing batches
            foreach (var batch in BatchRetry)
            {
                batch.Value.Clear();
            }
        }

        /// <summary>
        /// Get last exising database record. Method returns record based on order in Database
        /// </summary>
        /// <returns>First Backtrace database record</returns>
        public BacktraceDatabaseRecord LastOrDefault()
        {
            return RetryOrder == RetryOrder.Stack
                    ? GetLastRecord()
                    : GetFirstRecord();
        }

        /// <summary>
        /// Get first exising database record. Method returns record based on order in Database
        /// </summary>
        /// <returns>First Backtrace database record</returns>
        public BacktraceDatabaseRecord FirstOrDefault()
        {
            return RetryOrder == RetryOrder.Queue
                    ? GetFirstRecord()
                    : GetLastRecord();
        }

        /// <summary>
        /// Get first Backtrace database record by predicate funciton
        /// </summary>
        /// <param name="predicate">Filter function</param>
        /// <returns>Backtrace Database record</returns>
        public BacktraceDatabaseRecord FirstOrDefault(Func<BacktraceDatabaseRecord, bool> predicate)
        {
            return BatchRetry
                .SelectMany(n => n.Value)
                .FirstOrDefault(predicate);
        }

        /// <summary>
        /// Get first record in in-cache BacktraceDatabase
        /// </summary>
        /// <returns>First database record</returns>
        private BacktraceDatabaseRecord GetFirstRecord()
        {
            //get all batches (from the beginning)
            for (int i = 0; i < _retryNumber; i++)
            {
                //if batch has any record that is not used
                //set lock to true 
                //and return file
                if (BatchRetry.ContainsKey(i) && BatchRetry[i].Any(n => !n.Locked))
                {
                    var record = BatchRetry[i].FirstOrDefault(n => !n.Locked);
                    if (record == null)
                    {
                        return null;
                    }
                    record.Locked = true;
                    return record;
                }
            }
            return null;
        }

        /// <summary>
        /// Get last record in in-cache BacktraceDatabase
        /// </summary>
        /// <returns>Last database record</returns>
        private BacktraceDatabaseRecord GetLastRecord()
        {
            for (int i = _retryNumber - 1; i >= 0; i--)
            {
                if (BatchRetry[i].Any(n => !n.Locked))
                {
                    var record = BatchRetry[i].Last(n => !n.Locked);
                    record.Locked = true;
                    return record;
                }
            }
            return null;
        }

        /// <summary>
        /// Get database size
        /// </summary>
        /// <returns>database size</returns>
        public long GetSize()
        {
            return TotalSize;
        }

        /// <summary>
        /// Get total number of records
        /// </summary>
        /// <returns>Total number of records</returns>
        public int GetTotalNumberOfRecords()
        {
            return Count();
        }

        public IEnumerable<BacktraceDatabaseRecord> GetRecordsToDelete()
        {
            return BatchRetry[_retryNumber - 1];
        }

        public void AddDuplicate(BacktraceDatabaseRecord record)
        {
            record.Increment();
            TotalRecords++;
        }
    }
}
