using System;
using System.Diagnostics;

namespace Backtrace.Unity.Common
{
    internal static class MetricsHelper
    {
        public static string GetPerformanceInfo(Stopwatch stopwatch)
        {
            return Math.Max(1, stopwatch.ElapsedMilliseconds).ToString();
        }
    }
}
