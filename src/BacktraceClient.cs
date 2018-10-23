using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity
{
    /// <summary>
    /// Base Backtrace .NET Client 
    /// </summary>
    public class BacktraceClient
    {

        /// <summary>
        /// Custom request handler for HTTP call to server
        /// </summary>
        public Func<string, string, BacktraceData, BacktraceResult> RequestHandler
        {
            get
            {
                return BacktraceApi.RequestHandler;
            }

            set
            {
                BacktraceApi.RequestHandler = value;
            }
        }
        /// <summary>
        /// Set an event executed when received bad request, unauthorize request or other information from server
        /// </summary>
        public Action<Exception> OnServerError
        {
            get
            {
                return BacktraceApi.OnServerError;
}

            set
            {
                BacktraceApi.OnServerError = value;
            }
        }

        /// <summary>
        /// Set an event executed when Backtrace API return information about send report
        /// </summary>
        public Action<BacktraceResult> OnServerResponse
        {
            get
            {
                return BacktraceApi.OnServerResponse;
            }

            set
            {
                BacktraceApi.OnServerResponse = value;
            }
        }

        /// <summary>
        /// Get or set minidump type
        /// </summary>
        public MiniDumpType MiniDumpType { get; set; } = MiniDumpType.Normal;

        /// <summary>
        /// Set event executed when client site report limit reached
        /// </summary>
        public Action<BacktraceReport> OnClientReportLimitReached
        {
            set
            {
                BacktraceApi.SetClientRateLimitEvent(value);
            }
        }

        /// <summary>
        /// Set event executed before sending data to Backtrace API
        /// </summary>
        public Func<BacktraceData, BacktraceData> BeforeSend = null;

        /// <summary>
        /// Set event executed when unhandled application exception event catch exception
        /// </summary>
        public Action<Exception> OnUnhandledApplicationException = null;

        /// <summary>
        /// Get custom client attributes. Every argument stored in dictionary will be send to Backtrace API
        /// </summary>
        public readonly Dictionary<string, object> Attributes;

        /// <summary>
        /// Backtrace database instance that allows to manage minidump files 
        /// </summary>
        public IBacktraceDatabase Database;

        private IBacktraceApi _backtraceApi;
        /// <summary>
        /// Instance of BacktraceApi that allows to send data to Backtrace API
        /// </summary>
        internal IBacktraceApi BacktraceApi
        {
            get
            {
                return _backtraceApi;
            }

            set
            {
                _backtraceApi = value;
                Database?.SetApi(_backtraceApi);
            }
        }

        /// <summary>
        /// Initialize new client instance with BacktraceCredentials
        /// </summary>
        /// <param name="backtraceCredentials">Backtrace credentials to access Backtrace API</param>
        /// <param name="attributes">Additional information about current application</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Number of reports sending per one minute. If value is equal to zero, there is no request sending to API. Value have to be greater than or equal to 0</param>
        public BacktraceClient(
            BacktraceCredentials backtraceCredentials,
            Dictionary<string, object> attributes = null,
            BacktraceDatabaseSettings databaseSettings = null,
            uint reportPerMin = 3)
            : this(backtraceCredentials, attributes, new BacktraceDatabase(databaseSettings),
                  reportPerMin)
        { }

        /// <summary>
        /// Initialize new client instance with BacktraceCredentials
        /// </summary>
        /// <param name="backtraceCredentials">Backtrace credentials to access Backtrace API</param>
        /// <param name="attributes">Additional information about current application</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Number of reports sending per one minute. If value is equal to zero, there is no request sending to API. Value have to be greater than or equal to 0</param>
        public BacktraceClient(
            BacktraceCredentials backtraceCredentials,
            Dictionary<string, object> attributes = null,
            IBacktraceDatabase database = null,
            uint reportPerMin = 3)
        {
            Attributes = attributes ?? new Dictionary<string, object>();
            BacktraceApi = new BacktraceApi(backtraceCredentials, reportPerMin);
            Database = database ?? new BacktraceDatabase();
            Database.SetApi(BacktraceApi);
            Database.Start();
        }

        /// <summary>
        /// Change maximum number of reportrs sending per one minute
        /// </summary>
        /// <param name="reportPerMin">Number of reports sending per one minute. If value is equal to zero, there is no request sending to API. Value have to be greater than or equal to 0</param>
        public void SetClientReportLimit(uint reportPerMin)
        {
            BacktraceApi.SetClientRateLimit(reportPerMin);
        }

        /// <summary>
        /// Send a report to Backtrace API
        /// </summary>
        /// <param name="report">Report to send</param>
        public virtual BacktraceResult Send(BacktraceReport report)
        {
            var record = Database.Add(report, Attributes, MiniDumpType);
            //create a JSON payload instance
            var data = record?.BacktraceData ?? report.ToBacktraceData(Attributes);
            //valid user custom events
            data = BeforeSend?.Invoke(data) ?? data;
            var result = BacktraceApi.Send(data);
            record?.Dispose();
            if (result?.Status == BacktraceResultStatus.Ok)
            {
                Database.Delete(record);
            }
            //check if there is more errors to send
            //handle inner exception
            result.InnerExceptionResult = HandleInnerException(report);
            return result;
        }

        /// <summary>
        /// Handle inner exception in current backtrace report
        /// if inner exception exists, client should send report twice - one with current exception, one with inner exception
        /// </summary>
        /// <param name="report">current report</param>
        private BacktraceResult HandleInnerException(BacktraceReport report)
        {
            //we have to create a copy of an inner exception report
            //to have the same calling assembly property
            var innerExceptionReport = report.CreateInnerReport();
            if (innerExceptionReport == null)
            {
                return null;
            }
            return Send(innerExceptionReport);
        }
    }
}
