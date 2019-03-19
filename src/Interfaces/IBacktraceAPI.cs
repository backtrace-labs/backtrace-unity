using Backtrace.Unity.Model;
using System;
using System.Collections;

namespace Backtrace.Unity.Interfaces
{
    /// <summary>
    /// Backtrace API sender interface
    /// </summary>
    public interface IBacktraceApi
    {
        /// <summary>
        /// Send a Backtrace report to Backtrace API
        /// </summary>
        /// <param name="data">Library diagnostic data</param>
        IEnumerator Send(BacktraceData data, Action<BacktraceResult> callback = null);

        /// <summary>
        /// Set an event executed when received bad request, unauthorize request or other information from server
        /// </summary>
        Action<Exception> OnServerError { get; set; }

        /// <summary>
        /// Set an event executed when server return information after sending data to API
        /// </summary>
        Action<BacktraceResult> OnServerResponse { get; set; }

        void SetClientRateLimitEvent(Action<BacktraceReport> onClientReportLimitReached);

        void SetClientRateLimit(uint rateLimit);

        /// <summary>
        /// Setup custom request method
        /// </summary>
        Func<string, BacktraceData, BacktraceResult> RequestHandler { get; set; }
    }
}
