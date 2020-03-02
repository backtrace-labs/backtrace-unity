using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Services
{
    /// <summary>
    /// Report watcher class. Watcher controls number of reports sending per one minute. If value reportPerMin is equal to zero, there is no request sending to API. Value has to be greater than or equal to 0
    /// </summary>
    public class ReportLimitWatcher
    {
        /// <summary>
        /// Report timestamp queue. ReportLimitWatcher store events timestamp in _reportQueue
        /// to validate number of reports that Backtarce integration will send per minute.
        /// </summary>
        internal readonly Queue<long> _reportQueue;

        /// <summary>
        /// Time period used to clear values from report queue.
        /// </summary>
        private readonly long _queueReportTime = 60;

        /// <summary>
        /// Determine if watcher is enabled.
        /// </summary>
        private bool _watcherEnable;
        /// <summary>
        /// Determine how many reports class instance can store in report queue.
        /// </summary>
        private int _reportPerMin;


        /// <summary>
        /// Determine if ReportLimitWatcher class should display warning message
        /// </summary>
        private bool _displayMessage = false;

        /// <summary>
        /// Determine if BacktraceClient/BacktraceDatabase hit report limit 
        /// </summary>
        private bool _limitHit = false;

        /// <summary>
        /// Create new instance of background watcher
        /// </summary>
        /// <param name="reportPerMin">How many times per minute watcher can send a report</param>
        internal ReportLimitWatcher(uint reportPerMin)
        {
            if (reportPerMin < 0)
            {
                throw new ArgumentException($"{nameof(reportPerMin)} have to be greater than or equal to zero");
            }
            int reportNumber = checked((int)reportPerMin);
            _reportQueue = new Queue<long>(reportNumber);
            _reportPerMin = reportNumber;
            _watcherEnable = reportPerMin != 0;
        }

        internal void SetClientReportLimit(uint reportPerMin)
        {
            int reportNumber = checked((int)reportPerMin);
            _reportPerMin = reportNumber;
            _watcherEnable = reportPerMin != 0;
        }

     
        /// <summary>
        /// Check if user can send new report to a Backtrace API
        /// </summary>
        /// <param name="report">Current report</param>
        /// <returns>true if user can add a new report</returns>
        public bool WatchReport(long timestamp)
        {
            if (!_watcherEnable)
            {
                return true;
            }
            //clear all reports older than _queReportTime
            Clear();
            if (_reportQueue.Count + 1 > _reportPerMin)
            {
                _limitHit = true;
                return false;
            }
            _limitHit = false;
            _displayMessage = true;
            _reportQueue.Enqueue(timestamp);
            return true;
        }

        /// <summary>
        /// Check if user can send new report to a Backtrace API
        /// </summary>
        /// <param name="report">Current report</param>
        /// <returns>true if user can add a new report</returns>
        public bool WatchReport(BacktraceReport report)
        {
            return WatchReport(report.Timestamp);
        }


        /// <summary>
        /// Display report limit hit 
        /// </summary>
        public void DisplayReportLimitHitMessage()
        {
            if(_limitHit == true && _displayMessage == true)
            {
                _displayMessage = false;
                Debug.LogWarning($"Backtrace report limit hit({_reportPerMin}/min) – Ignoring errors for 1 minute");
            }
        }


        /// <summary>
        /// Remove all records with timestamp older than one minute from now
        /// </summary>
        private void Clear()
        {
            long currentTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            bool clear = false;
            while (!clear && _reportQueue.Count != 0)
            {
                var item = _reportQueue.Peek();
                clear = !(currentTime - item >= _queueReportTime);
                if (!clear)
                {
                    _reportQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// This method only is used in test class project. Use Reset method to reset current counter and available reports
        /// </summary>
        internal void Reset()
        {
            _reportQueue.Clear();
        }

    }
}
