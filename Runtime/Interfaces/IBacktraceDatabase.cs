using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    /// <summary>
    /// Backtrace Database Interface
    /// Before start: Be sure that used directory is empty!
    /// </summary>
    public interface IBacktraceDatabase
    {
        /// <summary>
        /// Screenshot quality
        /// </summary>
        int ScreenshotQuality { get; set; }

        /// <summary>
        /// Screenshot max height - based on screenshot max height, algorithm calculates
        /// ratio, that allows to calculate screenshot max width
        /// </summary>
        int ScreenshotMaxHeight { get; set; }

        /// <summary>
        /// Backtrace Breadcrumbs
        /// </summary>
        IBacktraceBreadcrumbs Breadcrumbs { get; }

        /// <summary>
        /// Send all reports stored in BacktraceDatabase and clean database
        /// </summary>
        void Flush();

        /// <summary>
        /// Set Backtrace API instance
        /// </summary>
        /// <param name="backtraceApi">Backtrace API object instance</param>
        void SetApi(IBacktraceApi backtraceApi);


        /// <summary>
        /// Remove all existing reports in BacktraceDatabase
        /// </summary>
        void Clear();

        /// <summary>
        /// Check all database consistency requirements
        /// </summary>
        /// <returns>True - if database has valid consistency requirements</returns>
        bool ValidConsistency();

        /// <summary>
        /// Add new report to Database
        /// </summary>
        [Obsolete("Please use Add method with Backtrace data parameter instead")]
        BacktraceDatabaseRecord Add(BacktraceReport backtraceReport, Dictionary<string, string> attributes, MiniDumpType miniDumpType = MiniDumpType.Normal);

        /// <summary>
        /// Get all records stored in Database
        /// </summary>
        IEnumerable<BacktraceDatabaseRecord> Get();

        /// <summary>
        /// Delete database record by using BacktraceDatabaseRecord
        /// </summary>
        /// <param name="record">Database record</param>
        void Delete(BacktraceDatabaseRecord record);

        /// <summary>
        /// Get database settings
        /// </summary>
        /// <returns></returns>
        BacktraceDatabaseSettings GetSettings();

        /// <summary>
        /// Get database size
        /// </summary>
        long GetDatabaseSize();

        /// <summary>
        /// Set report limit watcher - object responsible to validate number of events per time unit
        /// </summary>
        /// <param name="reportLimitWatcher">Report limit watcher instance</param>
        void SetReportWatcher(ReportLimitWatcher reportLimitWatcher);

        /// <summary>
        /// Reload Backtrace database configuration. Reloading configuration is required, when you change 
        /// BacktraceDatabase configuration options.
        /// </summary>
        void Reload();

        /// <summary>
        /// Add Backtrace data to database
        /// </summary>
        /// <param name="data">Backtrace data</param>
        /// <param name="lock">Lock report - default true</param>
        /// <returns>Backtrace record</returns>
        BacktraceDatabaseRecord Add(BacktraceData data, bool @lock = true);

        /// <summary>
        /// Validate if BacktraceDatabase is enabled
        /// </summary>
        /// <returns>true if BacktraceDatabase is enabled. Otherwise false.</returns>
        bool Enabled();

        /// <summary>
        /// Enables Breadcrumbs support
        /// </summary>
        /// <returns>True if the breadcrumbs file was initialized correctly. Otherwise false.</returns>
        bool EnableBreadcrumbsSupport();
    }
}
