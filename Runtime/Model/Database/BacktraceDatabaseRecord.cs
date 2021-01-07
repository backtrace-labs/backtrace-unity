﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        /// Path to database directory
        /// </summary>
        private string _path = string.Empty;

        /// <summary>
        /// Attachments path
        /// </summary>
        public List<string> Attachments { get; private set; }

        private string _diagnosticDataJson;

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
            if (!string.IsNullOrEmpty(_diagnosticDataJson))
            {
                return _diagnosticDataJson;
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
                attachments = Attachments
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
        /// <param name="path">database path</param>
        public BacktraceDatabaseRecord(BacktraceData data, string path)
        {
            Id = data.Uuid;
            Record = data;
            _path = path;
            Attachments = data.Attachments;
        }

        /// <summary>
        /// Save data to hard drive
        /// </summary>
        /// <returns>True if record was successfully saved on hard drive</returns>
        public bool Save()
        {
            try
            {
                var jsonPrefix = Record.UuidString;
                _diagnosticDataJson = Record.ToJson();
                DiagnosticDataPath = Path.Combine(_path, string.Format("{0}-attachment.json", jsonPrefix));
                Save(_diagnosticDataJson, DiagnosticDataPath);

                if (Attachments != null && Attachments.Count != 0)
                {
                    foreach (var attachment in Attachments)
                    {
                        if (IsInsideDatabaseDirectory(attachment))
                        {
                            Size += new FileInfo(attachment).Length;
                        }
                    }
                }
                //save record
                RecordPath = Path.Combine(_path, string.Format("{0}-record.json", jsonPrefix));
                Save(ToJson(), RecordPath);
                return true;
            }
            catch (IOException io)
            {
                Debug.Log("Received IOException while saving data to database.");
                Debug.Log(io.Message);
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
        }

        /// <summary>
        /// Save single file from database record
        /// </summary>
        /// <param name="json">single file (json/dmp)</param>
        /// <param name="destPath">file path</param>
        private void Save(string json, string destPath)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            byte[] file = Encoding.UTF8.GetBytes(json);
            Size += file.Length;

            using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(file, 0, file.Length);
            }
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
            Delete(DiagnosticDataPath);
            Delete(RecordPath);

            //remove database attachments
            if (Attachments != null && Attachments.Count != 0)
            {
                foreach (var attachment in Attachments)
                {
                    if (IsInsideDatabaseDirectory(attachment))
                    {
                        Delete(attachment);
                    }
                }
            }
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

        /// <summary>
        /// Validate if attachment is placed in Backtrace database.
        /// </summary>
        /// <param name="path">Path to attachment</param>
        /// <returns>True if attachment is in backtrace-database directory. Otherwise false.</returns>
        private bool IsInsideDatabaseDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }
            return Path.GetDirectoryName(path) == _path;
        }
        public virtual void Unlock()
        {
            Locked = false;
            Record = null;
            _diagnosticDataJson = string.Empty;
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
