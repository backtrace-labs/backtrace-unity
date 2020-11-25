namespace Backtrace.Unity.Types
{
    /// <summary>
    /// Existing send method result statuses
    /// </summary>
    public enum BacktraceResultStatus
    {
        /// <summary>
        /// Set when client/server limit is reached
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
        Empty,
        /// <summary>
        /// Status generated on networking error
        /// </summary>
        NetworkError
    }
}
