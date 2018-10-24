using System;
using Backtrace.Unity.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Send method result
    /// </summary>
    public class BacktraceResult
    {
        /// <summary>
        /// Current report
        /// </summary>
        public BacktraceReport BacktraceReport;

        /// <summary>
        /// Inner exception Backtrace status
        /// </summary>
        public BacktraceResult InnerExceptionResult;

        private string _message;
        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }

        /// <summary>
        /// Result
        /// </summary>
        public BacktraceResultStatus Status { get; set; } = BacktraceResultStatus.Ok;

        private string _object;
        /// <summary>
        /// Created object id
        /// </summary>
        public string Object
        {
            get
            {
                return _object;
            }
            set
            {
                _object = value;
                Status = BacktraceResultStatus.Ok;
            }
        }
        /// <summary>
        /// Backtrace APi can return _rxid instead of ObjectId. 
        /// Use this setter to set _object field correctly for both answers
        /// </summary>
        public string RxId
        {
            set
            {
                _object = value;
                Status = BacktraceResultStatus.Ok;
            }
        }


        /// <summary>
        /// Set result when client rate limit reached
        /// </summary>
        /// <param name="report">Executed report</param>
        /// <returns>BacktraceResult with limit reached information</returns>
        internal static BacktraceResult OnLimitReached(BacktraceReport report)
        {
            return new BacktraceResult()
            {
                BacktraceReport = report,
                Status = BacktraceResultStatus.LimitReached,
                Message = "Client report limit reached"
            };
        }

        /// <summary>
        /// Set result when error occurs while sending data to API
        /// </summary>
        /// <param name="report">Executed report</param>
        /// <param name="exception">Exception</param>
        /// <returns>BacktraceResult with exception information</returns>
        internal static BacktraceResult OnError(BacktraceReport report, Exception exception)
        {
            return new BacktraceResult()
            {
                BacktraceReport = report,
                Message = exception.Message,
                Status = BacktraceResultStatus.ServerError
            };
        }

        internal void AddInnerResult(BacktraceResult innerResult)
        {
            if (InnerExceptionResult == null)
            {
                InnerExceptionResult = innerResult;
                return;
            }
            InnerExceptionResult.AddInnerResult(innerResult);
        }
    }
}
