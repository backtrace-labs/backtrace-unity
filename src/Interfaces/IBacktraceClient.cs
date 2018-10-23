using Backtrace.Unity.Model;

namespace Backtrace.Unity.Interfaces
{
    /// <summary>
    /// Backtrace client interface. Use this interface with dependency injection features
    /// </summary>
    public interface IBacktraceClient
    {
        /// <summary>
        /// Send a new report to a Backtrace API
        /// </summary>
        /// <param name="report">New backtrace report</param>
        BacktraceResult Send(BacktraceReport report);
    }
}