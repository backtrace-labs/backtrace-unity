using Backtrace.Newtonsoft;
using Backtrace.Unity.Interfaces.Database;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
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
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Check if current record is in use
        /// </summary>
        [JsonIgnore]
        internal bool Locked { get; set; } = false;

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
        /// Stored record
        /// </summary>
        [JsonIgnore]
        internal virtual BacktraceData Record { get; set; }

        /// <summary>
        /// Path to database directory
        /// </summary>
        [JsonIgnore]
        private readonly string _path = string.Empty;

        /// <summary>
        /// Record writer
        /// </summary>
        [JsonIgnore]
        internal IBacktraceDatabaseRecordWriter RecordWriter;

        /// <summary>
        /// Get valid BacktraceData from current record
        /// </summary>
        [JsonIgnore]
        public virtual BacktraceData BacktraceData
        {
            get
            {
                if (Record != null)
                {
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
                        var diagnosticData = BacktraceDataConverter.DeserializeObject<BacktraceData>(diagnosticDataJson);
                        var report = BacktraceDataConverter.DeserializeObject<BacktraceReport>(reportJson);
                        //add report to diagnostic data
                        //we don't store report with diagnostic data in the same json
                        //because we have easier way to serialize and deserialize data
                        //and no problem/condition with serialization when BacktraceApi want to send diagnostic data to API
                        diagnosticData.Report = report;
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
        /// <summary>
        /// Constructor for serialization purpose
        /// </summary>
        [JsonConstructor]
        internal BacktraceDatabaseRecord()
        {
            RecordPath = $"{Id}-record.json";
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
                DiagnosticDataPath = Save(Record, $"{Id}-attachment");
                ReportPath = Save(Record.Report, $"{Id}-report");

                // get minidump information
                MiniDumpPath = Record.Report?.MinidumpFile ?? string.Empty;
                Size += MiniDumpPath == string.Empty ? 0 : new FileInfo(MiniDumpPath).Length;

                //save record
                RecordPath = Path.Combine(_path, $"{Id}-record.json");
                //check current record size
                var json = BacktraceDataConverter.SerializeObject(this);
                byte[] file = Encoding.UTF8.GetBytes(json);
                //add record size
                Size += file.Length;
                //save it again with actual record size
                RecordWriter.Write(this, $"{Id}-record");
                return true;
            }
            catch (IOException io)
            {
                Debug.Log($"Received {nameof(IOException)} while saving data to database.");
                Debug.Log($"Message {io.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.Log($"Received {nameof(Exception)} while saving data to database.");
                Debug.Log($"Message {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save single file from database record
        /// </summary>
        /// <param name="data">single file (json/dmp)</param>
        /// <param name="prefix">file prefix</param>
        /// <returns>path to file</returns>
        private string Save(object data, string prefix)
        {
            if (data == null)
            {
                return string.Empty;
            }
            var json = BacktraceDataConverter.SerializeObject(data);
            byte[] file = Encoding.UTF8.GetBytes(json);
            Size += file.Length;
            return RecordWriter.Write(file, prefix);
        }

        /// <summary>
        /// Check if all necessary files declared on record exists
        /// </summary>
        /// <returns>True if record is valid</returns>
        internal virtual bool Valid()
        {
            return File.Exists(DiagnosticDataPath) && File.Exists(ReportPath);
        }

        /// <summary>
        /// Delete all record files
        /// </summary>
        internal virtual void Delete()
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
                Debug.Log($"File {path} is in use. Message: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.Log($"Cannot delete file: {path}. Message: {e.Message}");
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
                    return BacktraceDataConverter.DeserializeObject<BacktraceDatabaseRecord>(json);
                }
                catch (SerializationException)
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

        protected virtual void Dispose(bool disposing)
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
