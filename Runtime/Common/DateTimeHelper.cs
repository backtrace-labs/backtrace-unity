using System;

namespace Backtrace.Unity.Common
{
    internal static class DateTimeHelper
    {
        /// <summary>
        /// Current time in Timespan.
        /// Warning: We keep this code, because modern api that calculates
        /// timestamp is not available in the .NET 3.5/.NET 2.0.
        /// </summary>
        /// <returns>Timespan that represents DateTime.Now</returns>
        private static TimeSpan Now()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)));
        }
        /// <summary>
        /// Generates timestamp in sec
        /// </summary>
        /// <returns>Timestamp in sec</returns>
        public static int Timestamp()
        {
            return (int)(Now()).TotalSeconds;
        }
        /// <summary>
        /// Generates timestamp in ms
        /// </summary>
        /// <returns>Timestamp in ms</returns>
        public static double TimestampMs()
        {
            return Now().TotalMilliseconds;
        }
    }
}
