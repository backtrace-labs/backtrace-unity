using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using System;
using System.Collections.Generic;
using System.IO;

namespace Backtrace.Unity.Tests.Runtime
{
    internal class BacktraceDatabaseFileContextMock : IBacktraceDatabaseFileContext
    {
        public Func<BacktraceData, IEnumerable<string>> OnFileAttachments { get; set; }
        public Action<BacktraceDatabaseRecord> OnDelete { get; set; }
        public Func<BacktraceDatabaseRecord, bool> OnValidRecord { get; set; }
        public Func<BacktraceDatabaseRecord, bool> OnSave { get; set; }

        public int ScreenshotQuality { get; set; }

        public int ScreenshotMaxHeight { get; set; }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public void Delete(BacktraceDatabaseRecord record)
        {
            if (OnDelete != null)
            {
                OnDelete.Invoke(record);
            }
            return;
        }

        public IEnumerable<string> GenerateRecordAttachments(BacktraceData data)
        {
            return OnFileAttachments == null
                ? new List<string>()
                : OnFileAttachments.Invoke(data);
        }

        public IEnumerable<FileInfo> GetAll()
        {
            return new List<FileInfo>();
        }

        public IEnumerable<FileInfo> GetRecords()
        {
            return new List<FileInfo>();
        }

        public bool IsValidRecord(BacktraceDatabaseRecord record)
        {
            if (OnValidRecord != null)
            {
                return OnValidRecord.Invoke(record);
            }
            return false;
        }

        public void RemoveOrphaned(IEnumerable<BacktraceDatabaseRecord> existingRecords)
        {
            return;
        }

        public bool Save(BacktraceDatabaseRecord record)
        {
            if (OnSave != null)
            {
                return OnSave.Invoke(record);
            }
            return true;
        }

        public bool ValidFileConsistency()
        {
            return true;
        }
    }
}
