using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using System.Collections.Generic;
using System.IO;

namespace Backtrace.Unity.Interfaces
{
    internal interface IBacktraceDatabaseFileContext
    {
        /// <summary>
        /// Screenshot quality
        /// </summary>
        int ScreenshotQuality { get; set; }

        /// <summary>
        /// Screenshot max height - based on screenshot max height, algorithm calculates
        /// ratio, that allows to calculate screenshot max width
        /// </summary>
        int ScreenshotMaxHeight { get; set; }

        /// <summary>
        /// Get all valid physical records stored in database directory
        /// </summary>
        /// <returns>All existing physical records</returns>
        IEnumerable<FileInfo> GetRecords();

        /// <summary>
        /// Get all physical files stored in database directory
        /// </summary>
        /// <returns>All existing physical files</returns>
        IEnumerable<FileInfo> GetAll();

        /// <summary>
        /// Valid all database files consistency
        /// </summary>
        bool ValidFileConsistency();

        /// <summary>
        /// Remove orphaned files existing in database directory
        /// </summary>
        /// <param name="existingRecords">Existing entries in BacktraceDatabaseContext</param>
        void RemoveOrphaned(IEnumerable<BacktraceDatabaseRecord> existingRecords);

        /// <summary>
        /// Remove all files from database directory
        /// </summary>
        void Clear();

        /// <summary>
        /// Deletes backtrace database record from persistent data storage
        /// </summary>
        /// <param name="record">Database record</param>
        void Delete(BacktraceDatabaseRecord record);

        /// <summary>
        /// Generates list of attachments for current diagnostic data record
        /// </summary>
        /// <param name="data">Backtrace data</param>
        IEnumerable<string> GenerateRecordAttachments(BacktraceData data);

        /// <summary>
        /// Saves BacktraceDatabaseRerord on the hard drive
        /// </summary>
        /// <param name="record">BacktraceDatabaseRecord</param>
        /// <returns>true if file context was able to save data on the hard drive. Otherwise false</returns>
        bool Save(BacktraceDatabaseRecord record);

        /// <summary>
        /// Determine if BacktraceDatabaseRecord is valid.
        /// </summary>
        /// <param name="record">Database record</param>
        /// <returns>True, if the record exists. Otherwise false.</returns>
        bool IsValidRecord(BacktraceDatabaseRecord record);
    }
}
