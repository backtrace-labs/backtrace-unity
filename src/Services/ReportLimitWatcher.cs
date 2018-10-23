using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Backtrace.Tests")]
namespace Backtrace.Unity.Services
{
    /// <summary>
    /// Report watcher class. Watcher controls number of reports sending per one minute. If value reportPerMin is equal to zero, there is no request sending to API. Value has to be greater than or equal to 0
    /// </summary>
    internal class ReportLimitWatcher
    {
        /// <summary>
        /// Set event executed when client site report limit reached
        /// </summary>
        internal Action<BacktraceReport> OnClientReportLimitReached = null;

        internal readonly Queue<long> _reportQue;

        private readonly long _queReportTime = 60;
        private bool _watcherEnable;
        private int _reportPerSec;


        /// <summary>
        /// Create new instance of background watcher
        /// </summary>
        /// <param name="reportPerMin">How many times per minute watcher can send a report</param>
        public ReportLimitWatcher(uint reportPerMin)
        {
            if (reportPerMin < 0)
            {
                throw new ArgumentException($"{nameof(reportPerMin)} have to be greater than or equal to zero");
            }
            int reportNumber = checked((int)reportPerMin);
            _reportQue = new Queue<long>(reportNumber);
            _reportPerSec = reportNumber;
            _watcherEnable = reportPerMin != 0;
        }

        internal void SetClientReportLimit(uint reportPerMin)
        {
            int reportNumber = checked((int)reportPerMin);
            _reportPerSec = reportNumber;
            _watcherEnable = reportPerMin != 0;
        }

        /// <summary>
        /// Check if user can send new report to a Backtrace API
        /// </summary>
        /// <param name="report">Current report</param>
        /// <returns>true if user can add a new report</returns>
        public bool WatchReport(BacktraceReport report)
        {
            if (!_watcherEnable)
            {
                return true;
            }
            //clear all reports older than _queReportTime
            Clear();
            if (_reportQue.Count + 1 > _reportPerSec)
            {
                OnClientReportLimitReached?.Invoke(report);
                return false;
            }
            _reportQue.Enqueue(report.Timestamp);
            return true;
        }

        /// <summary>
        /// Remove all records with timestamp older than one minute from now
        /// </summary>
        private void Clear()
        {
            long currentTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            bool clear = false;
            while (!clear && _reportQue.Count != 0)
            {
                var item = _reportQue.Peek();
                clear = !(currentTime - item >= _queReportTime);
                if (!clear)
                {
                    _reportQue.Dequeue();
                }
            }
        }

        /// <summary>
        /// This method only is used in test class project. Use Reset method to reset current counter and available reports
        /// </summary>
        internal void Reset()
        {
            _reportQue.Clear();
        }

    }
}
