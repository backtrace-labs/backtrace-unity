using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    /// <summary>
    /// Backtrace client interface. Use this interface with dependency injection features
    /// </summary>
    public interface IBacktraceClient
    {
        /// <summary>
        /// Backtrace Breadcrumbs
        /// </summary>
        IBacktraceBreadcrumbs Breadcrumbs { get; }

        /// <summary>
        /// Send a new report to a Backtrace API
        /// </summary>
        /// <param name="report">New backtrace report</param>
        void Send(BacktraceReport report, Action<BacktraceResult> sendCallback);

        /// <summary>
        /// Send a message report to Backtrace API
        /// </summary>
        /// <param name="message">Report message</param>
        /// <param name="attachmentPaths">List of attachments</param>
        /// <param name="attributes">List of report attributes</param>
        void Send(string message, List<string> attachmentPaths = null, Dictionary<string, string> attributes = null);

        /// <summary>
        /// Send an exception to Backtrace API
        /// </summary>
        /// <param name="exception">Report exception</param>
        /// <param name="attachmentPaths">List of attachments</param>
        /// <param name="attributes">List of report attributes</param
        void Send(Exception exception, List<string> attachmentPaths = null, Dictionary<string, string> attributes = null);


        /// <summary>
        /// Set client report limit in Backtrace API
        /// </summary>
        /// <param name="reportPerMin"></param>
        void SetClientReportLimit(uint reportPerMin);

        /// <summary>
        /// Refresh client configuration
        /// </summary>
        void Refresh();

        /// <summary>
        /// Enabled Backtrace database breadcrumbs integration
        /// </summary>
        /// <returns>True, if breadcrumbs file was initialized correctly. Otherwise false.</returns>
        bool EnableBreadcrumbsSupport();

#if !UNITY_WEBGL
        /// <summary>
        /// Backtrace Metrics instance
        /// </summary>
        IBacktraceMetrics Metrics { get; }

        /// <summary>
        /// Enable event aggregation support.
        /// </summary>
        void EnableMetrics();

        /// <summary>
        /// Enable event aggregation support.
        /// </summary>
        void EnableMetrics(string uniqueEventsSubmissionUrl, string summedEventsSubmissionUrl, uint timeIntervalInSec, string uniqueEventName);
#endif
    }
}