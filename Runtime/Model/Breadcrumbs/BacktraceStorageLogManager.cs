﻿using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Model.Breadcrumbs
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
        /// Default breacrumbs size. By default breadcrumbs file size is limitted to 64kB.
        /// </summary>
        private long _breadcrumbsSize = 64000;

        /// <summary>
        /// Default log file name
        /// </summary>
        internal const string BreadcrumbLogFileName = "bt-breadcrumbs-0";

        /// <summary>
        /// default breadcrumb row ending
        /// </summary
        private byte[] _newRow = System.Text.Encoding.UTF8.GetBytes(",\n");

        /// <summary>
        /// Default breadcrumb document ending
        /// </summary>
        private byte[] _endOfDocument = System.Text.Encoding.UTF8.GetBytes("\n]");

        /// <summary>
        /// Default breadcrumb end of the document
        /// </summary>
        private byte[] _startOfDocument = System.Text.Encoding.UTF8.GetBytes("[\n");

        /// <summary>
        /// Breadcrumb id
        /// </summary>
        private long _breadcrumbId = 0;

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

        public BacktraceStorageLogManager(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                throw new ArgumentException("Breadcrumbs storage path is null or empty");
            }
            BreadcrumbsFilePath = Path.Combine(storagePath, BreadcrumbLogFileName);
        }

        /// <summary>
        /// Enables breadcrumbs integration
        /// </summary>
        /// <returns>true if breadcrumbs file was created. Otherwise false.</returns>
        public bool Enable()
        {
            try
            {
                if (File.Exists(BreadcrumbsFilePath))
                {
                    File.Delete(BreadcrumbsFilePath);
                }

                using (var _breadcrumbStream = new FileStream(BreadcrumbsFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    _breadcrumbStream.Write(_startOfDocument, 0, _startOfDocument.Length);
                    _breadcrumbStream.Write(_endOfDocument, 0, _endOfDocument.Length);
                }
                currentSize = _startOfDocument.Length + _endOfDocument.Length;
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
        public bool Add(string message, BreadcrumbLevel level, LogType type, IDictionary<string, string> attributes)
        {
            byte[] bytes;
            lock (_lockObject)
            {
                long id = _breadcrumbId++;
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
            catch (Exception)
            {
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
            long id,
            string message,
            BreadcrumbLevel level,
            LogType type,
            IDictionary<string, string> attributes)
        {
            var jsonObject = new BacktraceJObject();
            jsonObject.Add("timestamp", DateTimeHelper.Timestamp());
            jsonObject.Add("id", id);
            jsonObject.Add("level", Enum.GetName(typeof(BreadcrumbLevel), level));
            jsonObject.Add("type", Enum.GetName(typeof(LogType), type));
            jsonObject.Add("message", message);
            jsonObject.Add("attributes", new BacktraceJObject(attributes));
            return jsonObject;
        }
        /// <summary>
        /// Append breadcrumb JSON to the end of the breadcrumbs file.
        /// </summary>
        /// <param name="bytes">Bytes that represents single JSON object with breadcrumb informatiom</param>
        private bool AppendBreadcrumb(byte[] bytes)
        {
            // size of the breadcrumb - it's negative at the beginning because we're removing 2 bytes on start
            long appendingSize = _endOfDocument.Length * -1;
            using (var breadcrumbStream = new FileStream(BreadcrumbsFilePath, FileMode.Open, FileAccess.Write))
            {
                //back to position before end of the document \n}
                breadcrumbStream.Position = breadcrumbStream.Length - _endOfDocument.Length;

                // append ,\n when we're appending new row to existing list of rows. If this is first row
                // ignore it
                if (_breadcrumbId != 1)
                {
                    breadcrumbStream.Write(_newRow, 0, _newRow.Length);
                    appendingSize += _newRow.Length;
                }
                // append breadcrumbs json
                breadcrumbStream.Write(bytes, 0, bytes.Length);
                // and close JSON document
                breadcrumbStream.Write(_endOfDocument, 0, _endOfDocument.Length);
                appendingSize += (bytes.Length + _endOfDocument.Length);
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
            using (FileStream breadcrumbsStream = new FileStream(BreadcrumbsFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var size = breadcrumbsStream.Length - startPosition;
                    breadcrumbsStream.Seek(size * -1, SeekOrigin.End);

                    breadcrumbsStream.CopyTo(ms);
                    breadcrumbsStream.SetLength(size + _startOfDocument.Length);

                    ms.Position = 0;
                    breadcrumbsStream.Position = 0;

                    breadcrumbsStream.Write(_startOfDocument, 0, _startOfDocument.Length);
                    ms.CopyTo(breadcrumbsStream);
                }
            }
            // decrease a size of the breadcrumb file after removing n breadcrumbs
            currentSize -= startPosition;
            currentSize += _startOfDocument.Length;
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
            long numberOfFreeBytes = _startOfDocument.Length;
            int nextLineBytes = _newRow.Length;
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
                File.Delete(BreadcrumbsFilePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
