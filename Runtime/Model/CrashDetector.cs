using System;
using Backtrace.Unity.Runtime.Native;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Backtrace Crash loop detector. This detector allows to detect possible 
    /// crash loops during the startup in the game.
    /// </summary>
    public class CrashDetector : ICrashDetector
    {
        private readonly INativeCrashDetector _nativeCrashDetector;
        internal CrashDetector(INativeCrashDetector nativeCrashDetector)
        {
            _nativeCrashDetector = nativeCrashDetector;
        }

        public bool EnableCrashLoopDetection()
        {
            return _nativeCrashDetector.EnableCrashLoopDetection();
        }

        public bool IsSafeModeRequired()
        {
            return _nativeCrashDetector.IsSafeModeRequired();
        }

        public int ConsecutiveCrashesCount()
        {
            return _nativeCrashDetector.ConsecutiveCrashesCount();
        }
    }
}