using Backtrace.Unity.Model;
using System;
using System.Reflection;

namespace Backtrace.Unity.Common
{
    /// <summary>
    /// Extensions method available for every excepton object
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Convert current exception to BacktraceReport instance
        /// </summary>
        /// <param name="source">Current exception</param>
        /// <returns>Backtrace Report</returns>
        public static BacktraceReport ToBacktraceReport(this Exception source)
        {
            return new BacktraceReport(source);
        }

        public static Assembly GetExceptionSourceAssembly(this Exception source)
        {
            return source?.TargetSite?.DeclaringType?.Assembly;
        }
    }
}