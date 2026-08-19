#if UNITY_ANDROID || UNITY_EDITOR
using System;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Contains the JNI attach/detach lifecycle for the Unity-created ANR worker.
    /// The delegates keep the policy testable in the Editor without invoking Android JNI.
    /// </summary>
    internal static class AndroidAnrThreadLifecycle
    {
        internal const string ThreadFailureCode = "BT_UNITY_ANDROID_ANR_THREAD_FAILURE";
        internal const string DetachFailureCode = "BT_UNITY_ANDROID_JNI_DETACH_FAILURE";
        internal const string NativeDumpFailureCode = "BT_UNITY_ANDROID_NATIVE_DUMP_FAILURE";
        internal const string NativeAttributeFailureCode = "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE";

        internal static void Run(
            Func<int> attachCurrentThread,
            Action<Func<bool>> runWatchdog,
            Action detachCurrentThread,
            Action<string> logWarning)
        {
            bool attached = false;
            Func<bool> tryAttach = () =>
            {
                if (!attached)
                {
                    attached = attachCurrentThread() == 0;
                }
                return attached;
            };

            try
            {
                tryAttach();
                runWatchdog(tryAttach);
            }
            catch (Exception exception)
            {
                LogFailure(ThreadFailureCode, exception, logWarning);
            }
            finally
            {
                if (attached)
                {
                    try
                    {
                        detachCurrentThread();
                    }
                    catch (Exception exception)
                    {
                        LogFailure(DetachFailureCode, exception, logWarning);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the temporary Hang classification, sends the native dump, and always attempts to restore Crash.
        /// The Hang write can succeed natively and then throw during JNI local-reference cleanup, restoration cannot depend on the call returning normally.
        /// </summary>
        internal static void CaptureNativeDump(
            Action setHangType,
            Action sendDump,
            Action restoreCrashType,
            Action<string> logWarning)
        {
            try
            {
                bool hangTypeApplied = false;
                try
                {
                    setHangType();
                    hangTypeApplied = true;
                }
                catch (Exception exception)
                {
                    LogFailure(NativeAttributeFailureCode, exception, logWarning);
                }

                if (hangTypeApplied)
                {
                    try
                    {
                        sendDump();
                    }
                    catch (Exception exception)
                    {
                        LogFailure(NativeDumpFailureCode, exception, logWarning);
                    }
                }
            }
            finally
            {
                try
                {
                    restoreCrashType();
                }
                catch (Exception exception)
                {
                    LogFailure(NativeAttributeFailureCode, exception, logWarning);
                }
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
                // Diagnostics must never allow an optional ANR worker failure to escape.
            }
        }
    }
}
#endif
