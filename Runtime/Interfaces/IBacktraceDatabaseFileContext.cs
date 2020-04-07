using Backtrace.Unity.Model.Database;
using System.Collections.Generic;
using System.IO;

namespace Backtrace.Unity.Interfaces
{
    internal interface IBacktraceDatabaseFileContext
    {
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
    }
}
