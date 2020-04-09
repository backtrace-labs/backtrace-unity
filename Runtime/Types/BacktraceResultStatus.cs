using System;
using System.Collections.Generic;
using System.Text;

namespace Backtrace.Unity.Types
{
    /// <summary>
    /// Existing send method result statuses
    /// </summary>
    public enum BacktraceResultStatus
    {
        /// <summary>
        /// Set when client limit is reached
        /// </summary>
        LimitReached,
        /// <summary>
        /// Set when error occurs while sending diagnostic data
        /// </summary>
        ServerError,
        /// <summary>
        /// Set when data were send to API
        /// </summary>
        Ok,
        /// <summary>
        /// Status generated Backtrace client receive empty report (Aggregate Exception purpose)
        /// </summary>
        Empty
    }
}
