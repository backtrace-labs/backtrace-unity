using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Backtrace.Unity.Model.Breadcrumbs.Storage
{
    internal sealed class BacktraceStorageLogManager : IBacktraceLogManager
    {
        /// <summary>
        /// Path to the breadcrumbs file
        /// </summary>
        public string BreadcrumbsFilePath { get; private set; }

        /// <summary>
        /// Minimum size of the breadcrumbs file (10kB)
        /// </summary>
        public const int MinimumBreadcrumbsFileSize = 10 * 1000;

        /// <summary>
        /// Breadcrumbs file size. 
        /// </summary>
        public long BreadcrumbsSize
        {
            get
            {
                return _breadcrumbsSize;
            }
            set
            {
                if (value < MinimumBreadcrumbsFileSize)
                {
                    throw new ArgumentException("Breadcrumbs size must be greater or equal to 10kB");
                }
                _breadcrumbsSize = value;
            }
        }

        /// <summary>
        /// Default breadcrumbs size, by default limited to 64kB.
        /// </summary>
        private long _breadcrumbsSize = 64000;

        /// <summary>
        /// Default log file name
        /// </summary>
        internal const string BreadcrumbLogFileName = "bt-breadcrumbs-0";

        /// <summary>
        /// default breadcrumb row ending
        /// </summary>
        internal static byte[] NewRow = System.Text.Encoding.UTF8.GetBytes(",\n");

        /// <summary>
        /// Default breadcrumb document ending
        /// </summary>
        internal static byte[] EndOfDocument = System.Text.Encoding.UTF8.GetBytes("\n]");

        /// <summary>
        /// Default breadcrumb end of the document
        /// </summary>
        internal static byte[] StartOfDocument = System.Text.Encoding.UTF8.GetBytes("[\n");

        private bool _emptyFile = true;

        /// <summary>
        /// Breadcrumb id
        /// </summary>
        private double _breadcrumbId = DateTimeHelper.TimestampMs();

        /// <summary>
        /// Lock object
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        /// Current breadcurmbs fle size
        /// </summary>
        private long currentSize = 0;

        /// <summary>
        /// Queue that represents number of bytes in each log stored in the breadcrumb file
        /// </summary>
        private readonly Queue<long> _logSize = new Queue<long>();

        internal IBreadcrumbFile BreadcrumbFile { get; set; }

        public BacktraceStorageLogManager(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                throw new ArgumentException("Breadcrumbs storage path is null or empty");
            }
            BreadcrumbsFilePath = Path.Combine(storagePath, BreadcrumbLogFileName);
            BreadcrumbFile = new BreadcrumbFile(BreadcrumbsFilePath);
        }

        /// <summary>
        /// Enables breadcrumbs integration
        /// </summary>
        /// <returns>true if breadcrumbs file was created. Otherwise false.</returns>
        public bool Enable()
        {
            try
            {
                if (BreadcrumbFile.Exists())
                {
                    BreadcrumbFile.Delete();
                }

                using (var _breadcrumbStream = BreadcrumbFile.GetCreateStream())
                {
                    _breadcrumbStream.Write(StartOfDocument, 0, StartOfDocument.Length);
                    _breadcrumbStream.Write(EndOfDocument, 0, EndOfDocument.Length);
                }
                currentSize = StartOfDocument.Length + EndOfDocument.Length;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Cannot initialize breadcrumbs file. Reason: {0}", e.Message));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Adds breadcrumb entry to the breadcrumbs file.
        /// </summary>
        /// <param name="message">Breadcrumb message</param>
        /// <param name="level">Breadcrumb level</param>
        /// <param name="type">Breadcrumb type</param>
        /// <param name="attributes">Breadcrumb attributs</param>
        /// <returns>True if breadcrumb was stored in the breadcrumbs file. Otherwise false.</returns>
        public bool Add(string message, BreadcrumbLevel level, UnityEngineLogLevel type, IDictionary<string, string> attributes)
        {
            byte[] bytes;
            lock (_lockObject)
            {
                double id = _breadcrumbId++;
                var jsonObject = CreateBreadcrumbJson(id, message, level, type, attributes);
                bytes = System.Text.Encoding.UTF8.GetBytes(jsonObject.ToJson());

                if (currentSize + bytes.Length > BreadcrumbsSize)
                {
                    try
                    {
                        ClearOldLogs();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            try
            {
                return AppendBreadcrumb(bytes);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Cannot append data to the breadcrumbs file. Reason: {0}", e.Message));
                return false;
            }
        }

        /// <summary>
        /// Convert diagnostic data to JSON format
        /// </summary>
        /// <param name="id">Breadcrumbs id</param>
        /// <param name="message">breadcrumbs message</param>
        /// <param name="level">Breadcrumb level</param>
        /// <param name="type">Breadcrumb type</param>
        /// <param name="attributes">Breadcrumb attributes</param>
        /// <returns>JSON object</returns>
        private BacktraceJObject CreateBreadcrumbJson(
            double id,
            string message,
            BreadcrumbLevel level,
            UnityEngineLogLevel type,
            IDictionary<string, string> attributes)
        {
            var jsonObject = new BacktraceJObject();
            // breadcrumbs integration accepts timestamp in ms not in sec.
            jsonObject.Add("timestamp", DateTimeHelper.TimestampMs(), "F0");
            jsonObject.Add("id", id, "F0");
            jsonObject.Add("type", Enum.GetName(typeof(BreadcrumbLevel), level).ToLower());
            jsonObject.Add("level", Enum.GetName(typeof(UnityEngineLogLevel), type).ToLower());
            jsonObject.Add("message", message);
            if (attributes != null && attributes.Count > 0)
            {
                jsonObject.Add("attributes", new BacktraceJObject(attributes));
            }
            return jsonObject;
        }
        /// <summary>
        /// Append breadcrumb JSON to the end of the breadcrumbs file.
        /// </summary>
        /// <param name="bytes">Bytes that represents single JSON object with breadcrumb informatiom</param>
        private bool AppendBreadcrumb(byte[] bytes)
        {
            // size of the breadcrumb - it's negative at the beginning because we're removing 2 bytes on start
            long appendingSize = EndOfDocument.Length * -1;
            using (var breadcrumbStream = BreadcrumbFile.GetWriteStream())
            {
                //back to position before end of the document \n}
                breadcrumbStream.Position = breadcrumbStream.Length - EndOfDocument.Length;

                // append ,\n when we're appending new row to existing list of rows. If this is first row
                // ignore it
                if (_emptyFile == false)
                {
                    breadcrumbStream.Write(NewRow, 0, NewRow.Length);
                    appendingSize += NewRow.Length;
                }
                else
                {
                    _emptyFile = false;
                }
                // append breadcrumbs json
                breadcrumbStream.Write(bytes, 0, bytes.Length);
                // and close JSON document
                breadcrumbStream.Write(EndOfDocument, 0, EndOfDocument.Length);
                appendingSize += (bytes.Length + EndOfDocument.Length);
            }
            currentSize += appendingSize;
            _logSize.Enqueue(bytes.Length);
            return true;
        }

        /// <summary>
        /// Remove last n logs from the breadcrumbs file. When breacrumbs file hit 
        /// the file size limit, this method will clear up the oldest logs to decrease
        /// file size.
        /// </summary>
        private void ClearOldLogs()
        {
            var startPosition = GetNextStartPosition();
            using (var breadcrumbsStream = BreadcrumbFile.GetIOStream())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var size = breadcrumbsStream.Length - startPosition;
                    breadcrumbsStream.Seek(size * -1, SeekOrigin.End);

                    breadcrumbsStream.CopyTo(ms);
                    breadcrumbsStream.SetLength(size + StartOfDocument.Length);

                    ms.Position = 0;
                    breadcrumbsStream.Position = 0;

                    breadcrumbsStream.Write(StartOfDocument, 0, StartOfDocument.Length);
                    ms.CopyTo(breadcrumbsStream);
                }
            }
            // decrease the size of the breadcrumb file after removing n breadcrumbs
            currentSize -= startPosition;
            currentSize += StartOfDocument.Length;
        }

        /// <summary>
        /// Calculate start position of the file that will be used
        /// to recreate breadcrumbs file. Position represents place
        /// where starts breadcrumbs that we should keep in the recreated file.
        /// </summary>
        /// <returns>Breadcrumb start index</returns>
        private long GetNextStartPosition()
        {
            double expectedFreedBytes = BreadcrumbsSize - (BreadcrumbsSize * 0.7);
            long numberOfFreeBytes = StartOfDocument.Length;
            int nextLineBytes = NewRow.Length;
            while (numberOfFreeBytes < expectedFreedBytes)
            {
                numberOfFreeBytes += (_logSize.Dequeue() + nextLineBytes);
            }

            return numberOfFreeBytes;
        }

        /// <summary>
        /// Remove breadcrumbs file
        /// </summary>
        public bool Clear()
        {
            try
            {
                currentSize = 0;
                _logSize.Clear();
                BreadcrumbFile.Delete();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int Length()
        {
            return _logSize.Count;
        }

        public double BreadcrumbId()
        {
            return _breadcrumbId;
        }
    }
}
