using Backtrace.Unity.Model.Attributes;

namespace Backtrace.Unity.Runtime.Native
{
    /// <summary>
    /// Backtrace native client crash detector interface
    /// </summary>
    internal interface INativeCrashDetector
    {
        /// <summary>
        /// Enable crash loop detection to prevent infinity loop of crashes.
        /// </summary>
        bool EnableCrashLoopDetection();

        /// <summary>
        /// Determines if the safe mode is required.
        /// </summary>
        bool IsSafeModeRequired();

        /// <summary>
        /// Returns information how many time in a row does the application crash.
        /// </summary>
        int ConsecutiveCrashesCount();
    }
}
