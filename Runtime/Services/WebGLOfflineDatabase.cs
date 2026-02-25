#if UNITY_WEBGL
using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Backtrace.Unity.Services
{
    /// <summary>
    /// Lightweight PlayerPrefs offline queue for WebGL.
    ///
    /// This is a fallback persistence mechanism used when Backtrace offline database is enabled in
    /// <see cref="BacktraceConfiguration"/> but the on-disk <see cref="BacktraceDatabase"/> is not available
    /// (for example: invalid path, directory creation disabled or filesystem sync limitations in WebGL).
    ///
    /// The queue is bounded by Backtrace database configuration values (MaxRecordCount / MaxDatabaseSize),
    /// but also enforces additional hard caps suitable for WebGL to avoid excessive PlayerPrefs usage.
    /// </summary>
    internal sealed class WebGLOfflineDatabase
    {
        /// <summary>
        /// Storage key prefix. A stable hash is appended.
        /// </summary>
        private const string StorageKeyPrefix = "backtrace-webgl-offline-queue-";

        /// <summary>
        /// WebGL hard cap: maximum number of records stored in PlayerPrefs.
        /// </summary>
        private const int HardMaxRecords = 32;

        /// <summary>
        /// WebGL hard cap: maximum total payload size in bytes stored in PlayerPrefs.
        /// </summary>
        private const int HardMaxTotalBytes = 1000 * 1000; // ~1 MB

        /// <summary>
        /// WebGL hard cap: maximum size in bytes for a single report JSON payload.
        /// </summary>
        private const int HardMaxRecordBytes = 256 * 1024; // 256 KB

        private readonly BacktraceConfiguration _configuration;
        private readonly string _storageKey;

        private WebGLOfflineQueue _cache;

        [Serializable]
        internal sealed class WebGLOfflineRecord
        {
            /// <summary>
            /// Backtrace report UUID used for lookup.
            /// </summary>
            public string uuid;

            /// <summary>
            /// Serialized BacktraceData JSON to replay later.
            /// </summary>
            public string json;

            /// <summary>
            /// Optional attachment paths.
            /// </summary>
            public string[] attachments;

            /// <summary>
            /// Deduplication count.
            /// </summary>
            public int deduplication;

            /// <summary>
            /// Unix timestamp (seconds).
            /// </summary>
            public long timestamp;

            /// <summary>
            /// Number of failed send attempts for this record.
            /// </summary>
            public int attempts;
        }

        [Serializable]
        private sealed class WebGLOfflineQueue
        {
            public List<WebGLOfflineRecord> items = new List<WebGLOfflineRecord>();
        }

        public WebGLOfflineDatabase(BacktraceConfiguration configuration)
        {
            _configuration = configuration;

            // Use a stable hash so different environments don't share a queue.
            var url = configuration != null ? configuration.GetValidServerUrl() : string.Empty;
            var hash = ComputeFnv1aHash(url);
            _storageKey = string.Format("{0}{1}", StorageKeyPrefix, hash);

            _cache = Load();
        }

        public bool IsEmpty
        {
            get { return _cache == null || _cache.items == null || _cache.items.Count == 0; }
        }

        public int Count
        {
            get { return _cache == null || _cache.items == null ? 0 : _cache.items.Count; }
        }

        /// <summary>
        /// Remove corrupted or empty entries.
        /// </summary>
        public void Compact()
        {
            EnsureCache();

            for (int i = _cache.items.Count - 1; i >= 0; i--)
            {
                var item = _cache.items[i];
                if (item == null || string.IsNullOrEmpty(item.uuid) || string.IsNullOrEmpty(item.json))
                {
                    _cache.items.RemoveAt(i);
                }
            }

            TrimToFit();
            Save();
        }

        /// <summary>
        /// Queue a report for retry on a future session.
        /// </summary>
        public void Enqueue(Guid uuid, string json, IEnumerable<string> attachments, int deduplication)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var jsonBytes = GetUtf8ByteCount(json);
            if (jsonBytes <= 0)
            {
                return;
            }

            // If the record is too large for safe PlayerPrefs storage, skip it.
            if (jsonBytes > HardMaxRecordBytes || jsonBytes > HardMaxTotalBytes)
            {
                return;
            }

            EnsureCache();

            _cache.items.Add(new WebGLOfflineRecord
            {
                uuid = uuid.ToString(),
                json = json,
                attachments = attachments != null ? new List<string>(attachments).ToArray() : new string[0],
                deduplication = deduplication,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                attempts = 0
            });

            TrimToFit();
            Save();
        }

        /// <summary>
        /// Try to peek a record without removing it.
        /// </summary>
        public bool TryPeek(RetryOrder retryOrder, out WebGLOfflineRecord record)
        {
            record = null;

            if (_cache == null || _cache.items == null || _cache.items.Count == 0)
            {
                return false;
            }

            record = retryOrder == RetryOrder.Stack
                ? _cache.items[_cache.items.Count - 1]
                : _cache.items[0];

            return record != null;
        }

        /// <summary>
        /// Check if the queue already contains a record with the given UUID.
        /// </summary>
        public bool Contains(string uuid)
        {
            if (string.IsNullOrEmpty(uuid) || _cache == null || _cache.items == null)
            {
                return false;
            }

            for (int i = 0; i < _cache.items.Count; i++)
            {
                if (_cache.items[i] != null && _cache.items[i].uuid == uuid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove a record by UUID.
        /// </summary>
        public void Remove(string uuid)
        {
            if (string.IsNullOrEmpty(uuid) || _cache == null || _cache.items == null)
            {
                return;
            }

            for (int i = 0; i < _cache.items.Count; i++)
            {
                if (_cache.items[i] != null && _cache.items[i].uuid == uuid)
                {
                    _cache.items.RemoveAt(i);
                    Save();
                    return;
                }
            }
        }

        /// <summary>
        /// Increment send attempt counter for a record.
        /// </summary>
        public void IncrementAttempts(string uuid)
        {
            if (string.IsNullOrEmpty(uuid) || _cache == null || _cache.items == null)
            {
                return;
            }

            for (int i = 0; i < _cache.items.Count; i++)
            {
                var item = _cache.items[i];
                if (item != null && item.uuid == uuid)
                {
                    item.attempts++;
                    Save();
                    return;
                }
            }
        }

        private void EnsureCache()
        {
            if (_cache == null)
            {
                _cache = new WebGLOfflineQueue();
            }
            if (_cache.items == null)
            {
                _cache.items = new List<WebGLOfflineRecord>();
            }
        }

        private WebGLOfflineQueue Load()
        {
            string raw;
            try
            {
                raw = PlayerPrefs.GetString(_storageKey, string.Empty);
            }
            catch
            {
                // Some browsers like Safari on iOS can throw on storage access.
                // We treat this as empty storage to avoid crashing the game.
                raw = string.Empty;
            }
            if (string.IsNullOrEmpty(raw))
            {
                return new WebGLOfflineQueue();
            }

            try
            {
                var queue = JsonUtility.FromJson<WebGLOfflineQueue>(raw);
                return queue ?? new WebGLOfflineQueue();
            }
            catch
            {
                // reset corrupted storage.
                return new WebGLOfflineQueue();
            }
        }

        private void Save()
        {
            try
            {
                EnsureCache();
                var json = JsonUtility.ToJson(_cache);
                PlayerPrefs.SetString(_storageKey, json);
                PlayerPrefs.Save();
            }
            catch
            {
                // We never throw into the game, worst case we just don't persist this batch.
            }
        }

        private void TrimToFit()
        {
            EnsureCache();

            var maxRecords = GetEffectiveMaxRecords();
            var maxBytes = GetEffectiveMaxBytes();

            // Remove oldest entries to satisfy max record count.
            while (_cache.items.Count > maxRecords)
            {
                _cache.items.RemoveAt(0);
            }

            // Remove oldest entries to satisfy size limit.
            var totalBytes = GetApproximateQueueBytes(_cache.items);
            while (totalBytes > maxBytes && _cache.items.Count > 0)
            {
                totalBytes -= GetApproximateRecordBytes(_cache.items[0]);
                _cache.items.RemoveAt(0);
            }
        }

        private int GetEffectiveMaxRecords()
        {
            int configured = BacktraceConfiguration.DefaultMaxRecordCount;
            if (_configuration != null)
            {
                configured = _configuration.MaxRecordCount;
            }

            if (configured < 0)
            {
                configured = BacktraceConfiguration.DefaultMaxRecordCount;
            }

            if (configured == 0)
            {
                configured = HardMaxRecords;
            }

            if (configured > HardMaxRecords)
            {
                configured = HardMaxRecords;
            }

            return Mathf.Max(1, configured);
        }

        private long GetEffectiveMaxBytes()
        {
            long configuredBytes = 0;
            if (_configuration != null)
            {
                // Convert MB to bytes to match BacktraceDatabaseSettings.
                configuredBytes = _configuration.MaxDatabaseSize * 1000L * 1000L;
            }

            if (configuredBytes <= 0)
            {
                configuredBytes = HardMaxTotalBytes;
            }

            if (configuredBytes > HardMaxTotalBytes)
            {
                configuredBytes = HardMaxTotalBytes;
            }

            return configuredBytes;
        }

        private static long GetApproximateQueueBytes(List<WebGLOfflineRecord> records)
        {
            if (records == null)
            {
                return 0;
            }

            long total = 0;
            for (int i = 0; i < records.Count; i++)
            {
                total += GetApproximateRecordBytes(records[i]);
            }
            return total;
        }

        private static long GetApproximateRecordBytes(WebGLOfflineRecord record)
        {
            if (record == null)
            {
                return 0;
            }

            long bytes = 0;
            bytes += GetUtf8ByteCount(record.uuid);
            bytes += GetUtf8ByteCount(record.json);

            if (record.attachments != null)
            {
                for (int i = 0; i < record.attachments.Length; i++)
                {
                    bytes += GetUtf8ByteCount(record.attachments[i]);
                }
            }

            // Rough overhead for JSON structure.
            bytes += 64;
            return bytes;
        }

        private static int GetUtf8ByteCount(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            try
            {
                return Encoding.UTF8.GetByteCount(value);
            }
            catch
            {
                // approximate fallback.
                return value.Length;
            }
        }

        private static string ComputeFnv1aHash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "00000000";
            }

            unchecked
            {
                const uint offsetBasis = 2166136261;
                const uint prime = 16777619;
                uint hash = offsetBasis;

                for (int i = 0; i < input.Length; i++)
                {
                    hash ^= input[i];
                    hash *= prime;
                }

                return hash.ToString("x8");
            }
        }
    }
}
#endif
