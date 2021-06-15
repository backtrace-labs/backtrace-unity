using Backtrace.Unity.Model;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    /// <summary>
    /// Backtrace API sender interface
    /// </summary>
    public interface IBacktraceApi
    {
        /// <summary>
        /// Server url
        /// </summary>
        string ServerUrl { get; }
        /// <summary>
        /// Send a Backtrace report to Backtrace API
        /// </summary>
        /// <param name="data">Library diagnostic data</param>
        IEnumerator Send(BacktraceData data, Action<BacktraceResult> callback = null);

        /// <summary>
        /// Send diagnostic report to Backtrace API
        /// </summary>
        /// <param name="json">Library diagnostic data in JSON format</param>
        /// <param name="attachments">List of report attachments</param>
        /// <param name="deduplication">Deduplication count</param>
        /// <param name="callback">Coroutine callback</param>
        /// <returns></returns>
        IEnumerator Send(string json, IEnumerable<string> attachments, int deduplication, Action<BacktraceResult> callback);


        /// <summary>
        /// Send diagnostic report to Backtrace API
        /// </summary>
        /// <param name="json">Library diagnostic data in JSON format</param>
        /// <param name="attachments">List of report attachments</param>
        /// <param name="queryStringAttributes">Query string</param>
        /// <param name="callback">Coroutine callback</param>
        /// <returns></returns>
        IEnumerator Send(string json, IEnumerable<string> attachments, Dictionary<string, string> queryAttributes, Action<BacktraceResult> callback);

        /// <summary>
        /// Set an event executed when received bad request, unauthorize request or other information from server
        /// </summary>
        Action<Exception> OnServerError { get; set; }

        /// <summary>
        /// Set an event executed when server return information after sending data to API
        /// </summary>
        Action<BacktraceResult> OnServerResponse { get; set; }

        /// <summary>
        /// Setup custom request method
        /// </summary>
        Func<string, BacktraceData, BacktraceResult> RequestHandler { get; set; }

        /// <summary>
        /// Upload minidump to server
        /// </summary>
        /// <param name="minidumpPath">Minidump path</param>
        /// <param name="attachments">attachment path</param>
        /// param name="queryAttributes"> query attributes </param>
        /// <param name="callback">Result callback</param>
        /// <returns>Server response</returns>
        IEnumerator SendMinidump(string minidumpPath, IEnumerable<string> attachments, IDictionary<string, string> queryAttributes, Action<BacktraceResult> callback = null);

        /// <summary>
        /// Enable performance statistics in Backtrace API
        /// </summary>
        bool EnablePerformanceStatistics { get; set; }
    }
}
