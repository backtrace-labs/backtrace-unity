using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs.Storage;
using Backtrace.Unity.Model.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Backtrace.Unity.Services
{
    /// <summary>
    /// BacktraceDatabase class for file collection operations
    /// </summary>
    internal class BacktraceDatabaseFileContext : IBacktraceDatabaseFileContext
    {

        private string[] _possibleDatabaseExtension = new string[] { ".dmp", ".json", ".jpg", ".log" };

        /// <summary>
        /// Maximum database size
        /// </summary>
        private readonly long _maxDatabaseSize;

        /// <summary>
        /// Maximum number of records in database
        /// </summary>
        private readonly uint _maxRecordNumber;

        /// <summary>
        /// Database directory info
        /// </summary>
        private readonly DirectoryInfo _databaseDirectoryInfo;

        /// <summary>
        /// Regex for filter physical database records
        /// </summary>
        private const string RecordFilterRegex = "*-record.json";

        /// <summary>
        /// Attachment manager
        /// </summary>
        internal readonly BacktraceDatabaseAttachmentManager _attachmentManager;

        /// <summary>
        /// Path to database directory
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// Screenshot quality
        /// </summary>
        public int ScreenshotQuality
        {
            get
            {
                return _attachmentManager.ScreenshotQuality;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException($"{nameof(value)} has to be greater than 0");
                }
                if (value > 100)
                {
                    throw new ArgumentException($"{nameof(value)} cannot be larger than 100");
                }
                _attachmentManager.ScreenshotQuality = value;
            }
        }

        /// <summary>
        /// Screenshot max height - based on screenshot max height, algorithm calculates
        /// ratio, that allows to calculate screenshot max width
        /// </summary>
        public int ScreenshotMaxHeight
        {
            get
            {
                return _attachmentManager.ScreenshotMaxHeight;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException($"{nameof(value)} has to be greater than 0");
                }
                _attachmentManager.ScreenshotMaxHeight = value;
            }
        }

        /// <summary>
        /// Initialize new BacktraceDatabaseFileContext instance
        /// </summary>
        public BacktraceDatabaseFileContext(BacktraceDatabaseSettings settings)
        {
            _attachmentManager = new BacktraceDatabaseAttachmentManager(settings);
            _maxDatabaseSize = settings.MaxDatabaseSize;
            _maxRecordNumber = settings.MaxRecordCount;
            _path = settings.DatabasePath;
            _databaseDirectoryInfo = new DirectoryInfo(_path);
        }

        /// <summary>
        /// Get all physical files stored in database directory
        /// </summary>
        /// <returns>All existing physical files</returns>
        public IEnumerable<FileInfo> GetAll()
        {
            return _databaseDirectoryInfo.GetFiles();
        }

        /// <summary>
        /// Get all valid physical records stored in database directory
        /// </summary>
        /// <returns>All existing physical records</returns>
        public IEnumerable<FileInfo> GetRecords()
        {
            return _databaseDirectoryInfo
                .GetFiles(RecordFilterRegex, SearchOption.TopDirectoryOnly)
                .OrderBy(n => n.CreationTime);
        }

        /// <summary>
        /// Remove orphaned files existing in database directory
        /// </summary>
        public void RemoveOrphaned(IEnumerable<BacktraceDatabaseRecord> existingRecords)
        {
            var recordStringIds = existingRecords.Select(n => n.Id.ToString());
            var files = GetAll();
            for (int fileIndex = 0; fileIndex < files.Count(); fileIndex++)
            {
                var file = files.ElementAt(fileIndex);
                //check if file should be stored in database
                //database only store data in json and files in dmp extension
                try
                {
                    // prevent from removing breadcrumbs file
                    if (file.Name.StartsWith(BacktraceStorageLogManager.BreadcrumbLogFileName))
                    {
                        continue;
                    }
                    if (!_possibleDatabaseExtension.Any(n => n == file.Extension))
                    {
                        file.Delete();
                        continue;
                    }
                    //get id from file name
                    //substring from position 0 to position from character '-' contains id
                    var name = file.Name.LastIndexOf('-');
                    // file can store invalid record because our regex don't match
                    // in this case we remove invalid file
                    if (name == -1)
                    {
                        file.Delete();
                        continue;
                    }
                    var stringGuid = file.Name.Substring(0, name);
                    if (!recordStringIds.Contains(stringGuid))
                    {
                        file.Delete();
                    }
                }
#pragma warning disable CS0168
                catch (Exception e)
                {
#if DEBUG
                    Debug.Log(e.ToString());
#endif
                    Debug.LogWarning(string.Format("Cannot remove file in path: {0}", file.FullName));
                }
#pragma warning restore CS0168
            }
        }

        /// <summary>
        /// Valid all files consistencies
        /// </summary>
        public bool ValidFileConsistency()
        {
            // Get array of all files
            FileInfo[] files = _databaseDirectoryInfo.GetFiles();

            // Calculate total bytes of all files in a loop.
            long size = 0;
            long totalRecordFiles = 0;
            foreach (var file in files)
            {
                if (Regex.Match(file.FullName, RecordFilterRegex).Success)
                {
                    totalRecordFiles++;

                    if (_maxRecordNumber > totalRecordFiles)
                    {
                        return false;
                    }
                }
                size += file.Length;
                if (size > _maxDatabaseSize)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Remove all files from database directory
        /// </summary>
        public void Clear()
        {
            // Get array of all files
            FileInfo[] files = _databaseDirectoryInfo.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                file.Delete();
            }
        }

        /// <summary>
        /// Deletes backtrace database record from persistent data storage
        /// </summary>
        /// <param name="record">Database record</param>
        public void Delete(BacktraceDatabaseRecord record)
        {
            // remove json objects
            Delete(record.DiagnosticDataPath);
            Delete(record.RecordPath);
            //remove database attachments
            if (record.Attachments == null || record.Attachments.Count == 0)
            {
                return;
            }
            // remove associated attachments
            foreach (var attachment in record.Attachments)
            {
                Delete(attachment);
            }
        }

        /// <summary>
        /// Validate if attachment is placed in Backtrace database.
        /// </summary>
        /// <param name="path">Path to attachment</param>
        /// <returns>True if attachment is in backtrace-database directory. Otherwise false.</returns>
        private bool IsDatabaseDependency(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }
            return Path.GetDirectoryName(path) == _path && !path.EndsWith(BacktraceStorageLogManager.BreadcrumbLogFileName);
        }

        /// <summary>
        /// Deletes files that are generated by the BacktraceDatabase object
        /// </summary>
        /// <param name="path">Path to file</param>
        private void Delete(string path)
        {
            try
            {
                if (IsDatabaseDependency(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException e)
            {
                Debug.Log(string.Format("File {0} is in use. Message: {1}", path, e.Message));
            }
            catch (Exception e)
            {
                Debug.Log(string.Format("Cannot delete file: {0}. Message: {1}", path, e.Message));
            }
        }

        /// <summary>
        /// Returns list of attachments generated for current diagnostic data
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        /// <returns></returns>
        public IEnumerable<string> GenerateRecordAttachments(BacktraceData data)
        {
            return _attachmentManager.GetReportAttachments(data);
        }

        /// <summary>
        /// Saves BacktraceDatabaseRecord on the hard drive
        /// </summary>
        /// <param name="record">BacktraceDatabaseRecord</param>
        /// <returns>true if file context was able to save data on the hard drive. Otherwise false</returns>
        public bool Save(BacktraceDatabaseRecord record)
        {
            try
            {
                var jsonPrefix = record.BacktraceData.UuidString;
                record.DiagnosticDataJson = record.BacktraceData.ToJson();
                record.DiagnosticDataPath = Path.Combine(_path, string.Format("{0}-attachment.json", jsonPrefix));
                record.Size += Save(record.DiagnosticDataJson, record.DiagnosticDataPath);

                // update record size based on the attachment information
                if (record.Attachments != null && record.Attachments.Count != 0)
                {
                    foreach (var attachment in record.Attachments)
                    {
                        if (IsDatabaseDependency(attachment))
                        {
                            record.Size += new FileInfo(attachment).Length;
                        }
                    }
                }
                record.RecordPath = Path.Combine(_path, string.Format("{0}-record.json", jsonPrefix));
                var recordJson = record.ToJson();
                record.Size += UTF8Encoding.Unicode.GetByteCount(recordJson);
                Save(recordJson, record.RecordPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Backtrace: Cannot save record on the hard drive. Reason: {e.Message}");
                Delete(record);

                return false;
            }
        }

        /// <summary>
        /// Save single file from database record
        /// </summary>
        /// <param name="json">single file (json/dmp)</param>
        /// <param name="destPath">file path</param>
        /// <returns>Saved file size</returns>
        private int Save(string json, string destPath)
        {
            if (string.IsNullOrEmpty(json))
            {
                return 0;
            }
            byte[] file = Encoding.UTF8.GetBytes(json);

            using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(file, 0, file.Length);
            }
            return file.Length;
        }

        /// <summary>
        /// Determine if BacktraceDatabaseRecord is valid.
        /// </summary>
        /// <param name="record">Database record</param>
        /// <returns>True, if the record exists. Otherwise false.</returns>
        public bool IsValidRecord(BacktraceDatabaseRecord record)
        {
            if (record == null)
            {
                return false;
            }
            return File.Exists(record.DiagnosticDataPath);
        }
    }
}
