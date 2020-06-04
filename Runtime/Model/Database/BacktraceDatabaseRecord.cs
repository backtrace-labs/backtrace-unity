using Backtrace.Unity.Interfaces.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Backtrace.Unity.Model.Database
{
    /// <summary>
    /// Single record in BacktraceDatabase
    /// </summary>
    public class BacktraceDatabaseRecord : IDisposable
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id = Guid.NewGuid();

        /// <summary>
        /// Check if current record is in use
        /// </summary>
        internal bool Locked = false;

        /// <summary>
        /// Path to json stored all information about current record
        /// </summary>
        internal string RecordPath { get; set; }

        /// <summary>
        /// Path to a diagnostic data json
        /// </summary>
        internal string DiagnosticDataPath { get; set; }

        /// <summary>
        /// Path to minidump file
        /// </summary>
        internal string MiniDumpPath { get; set; }

        /// <summary>
        /// Total size of record
        /// </summary>
        internal long Size { get; set; }

        /// <summary>
        /// Record hash
        /// </summary>
        public string Hash = string.Empty;

        /// <summary>
        /// Stored record
        /// </summary>
        internal BacktraceData Record { get; set; }

        /// <summary>
        /// Path to database directory
        /// </summary>
        private string _path = string.Empty;

        /// <summary>
        /// Attachments path
        /// </summary>
        public List<string> Attachments { get; private set; }

        private int _count = 1;

        public int Count
        {
            get
            {
                return _count;
            }
        }


        /// <summary>
        /// Record writer
        /// </summary>
        internal IBacktraceDatabaseRecordWriter RecordWriter;

        public string BacktraceDataJson()
        {
            if (Record != null)
            {
                return Record.ToJson();
            }
            if (string.IsNullOrEmpty(DiagnosticDataPath) || !File.Exists(DiagnosticDataPath))
            {
                return null;
            }

            return File.ReadAllText(DiagnosticDataPath);
        }

        /// <summary>
        /// Get valid BacktraceData from current record
        /// </summary>
        public BacktraceData BacktraceData
        {
            get
            {
                if (Record != null)
                {
                    Record.Deduplication = Count;
                    return Record;
                }
                return null;
            }
        }

        public string ToJson()
        {
            var rawRecord = new BacktraceDatabaseRawRecord()
            {
                Id = Id.ToString(),
                recordName = RecordPath,
                dataPath = DiagnosticDataPath,
                minidumpPath = MiniDumpPath,
                size = Size,
                hash = Hash,
                attachments = Record.Report.AttachmentPaths
            };
            return JsonUtility.ToJson(rawRecord, true);
        }

        public static BacktraceDatabaseRecord Deserialize(string json)
        {
            var rawRecord = JsonUtility.FromJson<BacktraceDatabaseRawRecord>(json);
            return new BacktraceDatabaseRecord(rawRecord);
        }
        /// <summary>
        /// Constructor for serialization purpose
        /// </summary>
        private BacktraceDatabaseRecord(BacktraceDatabaseRawRecord rawRecord)
        {
            Id = Guid.Parse(rawRecord.Id);
            RecordPath = rawRecord.recordName;
            DiagnosticDataPath = rawRecord.dataPath;
            MiniDumpPath = rawRecord.minidumpPath;
            Size = rawRecord.size;
            Hash = rawRecord.hash;
            Attachments = rawRecord.attachments;
        }

        /// <summary>
        /// Create new instance of database record
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        /// <param name="path">database path</param>
        public BacktraceDatabaseRecord(BacktraceData data, string path)
        {
            Id = data.Uuid;
            Record = data;
            _path = path;
            RecordWriter = new BacktraceDatabaseRecordWriter(path);
        }

        /// <summary>
        /// Save data to hard drive
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            try
            {
                var diagnosticDataJson = Record.ToJson();
                DiagnosticDataPath = Save(diagnosticDataJson, string.Format("{0}-attachment", Id));

                // get minidump information
                MiniDumpPath = Record.Report != null
                    ? Record.Report.MinidumpFile ?? string.Empty
                    : string.Empty;
                Size += MiniDumpPath == string.Empty ? 0 : new FileInfo(MiniDumpPath).Length;

                //save record
                RecordPath = Path.Combine(_path, string.Format("{0}-record.json", Id));
                //check current record size
                var json = ToJson();
                byte[] file = Encoding.UTF8.GetBytes(json);
                //add record size
                Size += file.Length;
                RecordWriter.Write(json, string.Format("{0}-record", Id));
                return true;
            }
            catch (IOException io)
            {
                Debug.Log(string.Format("Received {0} while saving data to database.",
                    "IOException"));
                Debug.Log(string.Format("Message {0}", io.Message));
                return false;
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Received {0} while saving data to database.", ex.GetType().Name));
                Debug.Log(string.Format("Message {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Setup RecordWriter and database path after deserialization event
        /// </summary>
        /// <param name="path">Path to database</param>
        internal void DatabasePath(string path)
        {
            _path = path;
            RecordWriter = new BacktraceDatabaseRecordWriter(path);
        }

        /// <summary>
        /// Save single file from database record
        /// </summary>
        /// <param name="json">single file (json/dmp)</param>
        /// <param name="prefix">file prefix</param>
        /// <returns>path to file</returns>
        private string Save(string json, string prefix)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }
            byte[] file = Encoding.UTF8.GetBytes(json);
            Size += file.Length;
            return RecordWriter.Write(file, prefix);
        }

        /// <summary>
        /// Increment number of the same records in database
        /// </summary>
        public virtual void Increment()
        {
            _count++;
        }

        /// <summary>
        /// Check if all necessary files declared on record exists
        /// </summary>
        /// <returns>True if record is valid</returns>
        internal bool Valid()
        {
            return File.Exists(DiagnosticDataPath);
        }

        /// <summary>
        /// Delete all records from hard drive.
        /// </summary>
        internal void Delete()
        {
            Delete(MiniDumpPath);
            Delete(DiagnosticDataPath);
            Delete(RecordPath);
        }

        /// <summary>
        /// Delete single file on database record
        /// </summary>
        /// <param name="path">path to file</param>
        private void Delete(string path)
        {
            try
            {
                if (File.Exists(path))
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
        /// Read single record from file
        /// </summary>
        /// <param name="file">Current file</param>
        /// <returns>Saved database record</returns>
        internal static BacktraceDatabaseRecord ReadFromFile(FileInfo file)
        {
            using (StreamReader streamReader = file.OpenText())
            {
                var json = streamReader.ReadToEnd();
                try
                {
                    return Deserialize(json);
                }
                catch (Exception)
                {
                    //handle invalid json 
                    return null;
                }
            }
        }
        #region dispose
#pragma warning disable CA1063 // Implement IDisposable Correctly
        public virtual void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Locked = false;
                Record = null;
            }
        }
        #endregion

        [Serializable]
        private class BacktraceDatabaseRawRecord
        {
            public string Id;
            public string recordName;
            public string dataPath;
            public string minidumpPath;
            public long size;
            public string hash;
            public List<string> attachments;
        }
    }
}
