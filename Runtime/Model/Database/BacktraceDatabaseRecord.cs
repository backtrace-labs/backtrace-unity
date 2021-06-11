using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Model.Database
{
    /// <summary>
    /// Single record in BacktraceDatabase
    /// </summary>
    public class BacktraceDatabaseRecord
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
        /// Attachments path
        /// </summary>
        public ICollection<string> Attachments { get; private set; }

        internal string DiagnosticDataJson { get; set; }

        /// <summary>
        /// Determine if current record is duplicated
        /// </summary>
        public bool Duplicated
        {
            get
            {
                return _count != 1;
            }
        }

        private int _count = 1;

        /// <summary>
        /// Number of instances of the record
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }
        /// <summary>
        /// Return JSON diagnostic data
        /// </summary>
        /// <returns></returns>
        public string BacktraceDataJson()
        {
            if (!string.IsNullOrEmpty(DiagnosticDataJson))
            {
                return DiagnosticDataJson;
            }

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

        /// <summary>
        /// Convert current record to JSON
        /// </summary>
        /// <returns>Record JSON representation</returns>
        public string ToJson()
        {
            var rawRecord = new BacktraceDatabaseRawRecord()
            {
                Id = Id.ToString(),
                recordName = RecordPath,
                dataPath = DiagnosticDataPath,
                size = Size,
                hash = Hash,
                attachments = new List<string>(Attachments)
            };
            return JsonUtility.ToJson(rawRecord, false);
        }

        /// <summary>
        /// Convert JSON record to Record
        /// </summary>
        /// <param name="json">JSON record</param>
        /// <returns>Backtrace Database record</returns>
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
            Size = rawRecord.size;
            Hash = rawRecord.hash;
            Attachments = rawRecord.attachments;
        }

        /// <summary>
        /// Create new instance of database record
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        public BacktraceDatabaseRecord(BacktraceData data)
        {
            Id = data.Uuid;
            Record = data;
            Attachments = data.Attachments;
        }

        /// <summary>
        /// Increment number of the same records in database
        /// </summary>
        public virtual void Increment()
        {
            _count++;
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

        public virtual void Unlock()
        {
            Locked = false;
            Record = null;
        }

        [Serializable]
        private struct BacktraceDatabaseRawRecord
        {
            public string Id;
            public string recordName;
            public string dataPath;
            public long size;
            public string hash;
            public List<string> attachments;
        }
    }
}
