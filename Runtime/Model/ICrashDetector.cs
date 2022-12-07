using Backtrace.Unity.Model.Attributes;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Backtrace Crash loop detector. This detector allows to detect possible 
    /// crash loops during the startup in the game.
    /// </summary>
    public interface ICrashDetector
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
