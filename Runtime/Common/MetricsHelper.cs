using System;
using System.Diagnostics;
using System.Globalization;

namespace Backtrace.Unity.Common
{
    internal static class MetricsHelper
    {
        /// <summary>
        /// Get performance info from stopwatch in micros
        /// </summary>
        /// <param name="stopwatch">Stop watch</param>
        /// <returns>Elapsed time in μs</returns>
        public static string GetMicroseconds(this Stopwatch stopwatch)
        {
            return Math.Max(1, ((stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
