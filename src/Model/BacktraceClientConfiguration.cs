using System.Collections.Generic;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// All properties required by BacktraceClient in one place
    /// </summary>
    public class BacktraceClientConfiguration
    {
        /// <summary>
        /// Client credentials
        /// </summary>
        public readonly BacktraceCredentials Credentials;

        /// <summary>
        /// Client's attributes
        /// </summary>
        public Dictionary<string, object> ClientAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Numbers of records sending per one minute
        /// </summary>
        public uint ReportPerMin { get; set; } = 3;

        /// <summary>
        /// Create new client settings with disabled database
        /// </summary>
        /// <param name="credentials">Backtrace server API credentials</param>
        public BacktraceClientConfiguration(BacktraceCredentials credentials)
        {
            Credentials = credentials;
        }
    }
}
