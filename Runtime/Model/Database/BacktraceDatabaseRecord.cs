﻿using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using Backtrace.Unity.Interfaces.Database;
using System;
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
        [JsonProperty]
        public Guid Id = Guid.NewGuid();

        /// <summary>
        /// Check if current record is in use
        /// </summary>
        [JsonIgnore]
        internal bool Locked = false;

        /// <summary>
        /// Path to json stored all information about current record
        /// </summary>
        [JsonProperty(PropertyName = "recordName")]
        internal string RecordPath { get; set; }

        /// <summary>
        /// Path to a diagnostic data json
        /// </summary>
        [JsonProperty(PropertyName = "dataPath")]
        internal string DiagnosticDataPath { get; set; }

        /// <summary>
        /// Path to minidump file
        /// </summary>
        [JsonProperty(PropertyName = "minidumpPath")]
        internal string MiniDumpPath { get; set; }

        /// <summary>
        /// Path to Backtrace Report json
        /// </summary>
        [JsonProperty(PropertyName = "reportPath")]
        internal string ReportPath { get; set; }

        /// <summary>
        /// Total size of record
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        internal long Size { get; set; }

        /// <summary>
        /// Record hash
        /// </summary>
        [JsonProperty(PropertyName = "hash")]
        public string Hash = string.Empty;

        /// <summary>
        /// Stored record
        /// </summary>
        [JsonIgnore]
        internal BacktraceData Record { get; set; }

        /// <summary>
        /// Path to database directory
        /// </summary>
        [JsonIgnore]
        private string _path = string.Empty;

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
        [JsonIgnore]
        internal IBacktraceDatabaseRecordWriter RecordWriter;

        /// <summary>
        /// Get valid BacktraceData from current record
        /// </summary>
        [JsonIgnore]
        public BacktraceData BacktraceData
        {
            get
            {
                if (Record != null)
                {
                    Record.Deduplication = Count;
                    return Record;
                }
                if (!Valid())
                {
                    return null;
                }
                //read json files stored in BacktraceDatabase
                using (var dataReader = new StreamReader(DiagnosticDataPath))
                using (var reportReader = new StreamReader(ReportPath))
                {
                    //read diagnostic data
                    var diagnosticDataJson = dataReader.ReadToEnd();
                    //read report
                    var reportJson = reportReader.ReadToEnd();

                    //deserialize data - if deserialize fails, we receive invalid entry
                    try
                    {
                        var diagnosticData = BacktraceData.Deserialize(diagnosticDataJson);
                        var report = BacktraceReport.Deserialize(reportJson);
                        //add report to diagnostic data
                        //we don't store report with diagnostic data in the same json
                        //because we have easier way to serialize and deserialize data
                        //and no problem/condition with serialization when BacktraceApi want to send diagnostic data to API
                        diagnosticData.Report = report;
                        diagnosticData.Attachments = report.AttachmentPaths;
                        diagnosticData.Deduplication = Count;
                        return diagnosticData;
                    }
                    catch (SerializationException)
                    {
                        //catch all exception caused by invalid serialization
                        return null;
                    }
                }
            }
        }

        public string ToJson()
        {
            var record = new BacktraceJObject
            {
                {"Id", Id},
                {"recordName", RecordPath},
                {"dataPath", DiagnosticDataPath},
                {"minidumpPath", MiniDumpPath},
                {"reportPath", ReportPath},
                {"size", Size},
                {"hash", Hash }
            };
            return record.ToString();
        }

        public static BacktraceDatabaseRecord Deserialize(string json)
        {
            var @object = BacktraceJObject.Parse(json);
            return new BacktraceDatabaseRecord()
            {
                Id = new Guid(@object.Value<string>("Id")),
                RecordPath = @object.Value<string>("recordName"),
                DiagnosticDataPath = @object.Value<string>("dataPath"),
                MiniDumpPath = @object.Value<string>("minidumpPath"),
                ReportPath = @object.Value<string>("reportPath"),
                Size = @object.Value<long>("size"),
                Hash = @object.Value<string>("hash"),
            };
        }
        /// <summary>
        /// Constructor for serialization purpose
        /// </summary>
        [JsonConstructor]
        internal BacktraceDatabaseRecord()
        {
            RecordPath = string.Format("{0}-record.json", Id);
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
                var reportJson = Record.Report.ToJson();
                ReportPath = Save(reportJson, string.Format("{0}-report", Id));

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
                //save it again with actual record size
                string recordJson = ToJson();
                RecordWriter.Write(recordJson, string.Format("{0}-record", Id));
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
            return File.Exists(DiagnosticDataPath) && File.Exists(ReportPath);
        }

        /// <summary>
        /// Delete all records from hard drive.
        /// </summary>
        internal void Delete()
        {
            Delete(MiniDumpPath);
            Delete(ReportPath);
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                Locked = false;
                Record = null;
            }
        }
        #endregion
    }
}
