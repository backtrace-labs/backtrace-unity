using Backtrace.Unity.Types;
using System;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Send method result
    /// </summary>
    public class BacktraceResult
    {
        /// <summary>
        /// Inner exception Backtrace status
        /// </summary>
        public BacktraceResult InnerExceptionResult;

        public string message;
        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        public string response;

        /// <summary>
        /// Result
        /// </summary>
        public BacktraceResultStatus Status = BacktraceResultStatus.Ok;

        private string @object;
        /// <summary>
        /// Created object id
        /// </summary>
        public string Object
        {
            get
            {
                return @object;
            }
            set
            {
                @object = value;
                Status = BacktraceResultStatus.Ok;
            }
        }

        public string _rxId;
        /// <summary>
        /// Backtrace APi can return _rxid instead of ObjectId. 
        /// Use this setter to set _object field correctly for both answers
        /// </summary>
        public string RxId
        {
            get
            {
                return _rxId;
            }
            set
            {
                _rxId = value;
                Status = BacktraceResultStatus.Ok;
            }
        }


        /// <summary>
        /// Set result when client rate limit reached
        /// </summary>
        /// <param name="report">Executed report</param>
        /// <returns>BacktraceResult with limit reached information</returns>
        internal static BacktraceResult OnLimitReached()
        {
            return new BacktraceResult()
            {
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
        internal static BacktraceResult OnNetworkError(Exception exception)
        {
            return new BacktraceResult()
            {
                Message = exception.Message,
                Status = BacktraceResultStatus.NetworkError
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

        public static BacktraceResult FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new BacktraceResult()
                {
                    Status = BacktraceResultStatus.Empty
                };
            }
            var rawResult = JsonUtility.FromJson<BacktraceRawResult>(json);
            var result = new BacktraceResult()
            {
                response = rawResult.response,
                _rxId = rawResult._rxid,
                Status = rawResult.response == "ok" ? BacktraceResultStatus.Ok : BacktraceResultStatus.ServerError
            };
            return result;
        }


        [Serializable]
        private class BacktraceRawResult
        {
#pragma warning disable CA2235 // Mark all non-serializable fields
#pragma warning disable CS0649
            public string response;
            public string _rxid;
#pragma warning restore CS0649
#pragma warning restore CA2235 // Mark all non-serializable fields


        }
    }
}
