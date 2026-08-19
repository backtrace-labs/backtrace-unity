#if UNITY_ANDROID || UNITY_EDITOR
using System;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Runs every Android native cleanup stage independently,
    /// optional JNI failure cannot skip the remaining shutdown work or escape to the host game.
    /// </summary>
    internal static class AndroidNativeDisableLifecycle
    {
        internal const string AnrStopFailureCode = "BT_UNITY_ANDROID_ANR_STOP_FAILURE";
        internal const string NativeDisableFailureCode = "BT_UNITY_ANDROID_NATIVE_DISABLE_FAILURE";
        internal const string AnrWatcherStopFailureCode = "BT_UNITY_ANDROID_ANR_WATCHER_STOP_FAILURE";
        internal const string AnrWatcherDisposeFailureCode = "BT_UNITY_ANDROID_ANR_WATCHER_DISPOSE_FAILURE";
        internal const string UnhandledWatcherStopFailureCode = "BT_UNITY_ANDROID_UNHANDLED_WATCHER_STOP_FAILURE";
        internal const string UnhandledWatcherDisposeFailureCode = "BT_UNITY_ANDROID_UNHANDLED_WATCHER_DISPOSE_FAILURE";

        internal static void Run(
            Action stopAnrThread,
            Action disableNativeIntegration,
            Action stopAnrWatcher,
            Action disposeAnrWatcher,
            Action stopUnhandledWatcher,
            Action disposeUnhandledWatcher,
            Action<string> logWarning)
        {
            RunStep(stopAnrThread, AnrStopFailureCode, logWarning);
            RunStep(disableNativeIntegration, NativeDisableFailureCode, logWarning);
            RunStep(stopAnrWatcher, AnrWatcherStopFailureCode, logWarning);
            RunStep(disposeAnrWatcher, AnrWatcherDisposeFailureCode, logWarning);
            RunStep(stopUnhandledWatcher, UnhandledWatcherStopFailureCode, logWarning);
            RunStep(disposeUnhandledWatcher, UnhandledWatcherDisposeFailureCode, logWarning);
        }

        private static void RunStep(Action cleanup, string failureCode, Action<string> logWarning)
        {
            if (cleanup == null)
            {
                return;
            }

            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                LogFailure(failureCode, exception, logWarning);
            }
        }

        private static void LogFailure(string code, Exception exception, Action<string> logWarning)
        {
            var failureType = exception == null ? "unknown" : exception.GetType().FullName;
            try
            {
                if (logWarning != null)
                {
                    logWarning(code + ": Failure type: " + failureType);
                }
            }
            catch (Exception)
            {
                // Cleanup diagnostics are optional and must not interrupt shutdown.
            }
        }
    }
}
#endif
