using System;

namespace Backtrace.Unity.Runtime.Native
{
    /// <summary>
    /// Contains optional native attribute writes so platform or diagnostic failures cannot escape into the host game.
    /// Delegates keep this policy testable in the Editor.
    /// </summary>
    internal static class NativeAttributeLifecycle
    {
        internal const string NativeAttributeFailureCode = "BT_UNITY_NATIVE_ATTRIBUTE_FAILURE";

        internal static bool TrySetAttribute(
            Action setAttribute,
            string failureCode,
            Action<string> logWarning)
        {
            if (setAttribute == null)
            {
                throw new ArgumentNullException("setAttribute");
            }
            if (string.IsNullOrEmpty(failureCode))
            {
                throw new ArgumentException("A native attribute failure code is required", "failureCode");
            }

            try
            {
                setAttribute();
                return true;
            }
            catch (Exception exception)
            {
                LogFailure(failureCode, exception, logWarning);
                return false;
            }
        }

        private static void LogFailure(
            string failureCode,
            Exception exception,
            Action<string> logWarning)
        {
            var failureType = exception == null ? "unknown" : exception.GetType().FullName;
            try
            {
                if (logWarning != null)
                {
                    logWarning(failureCode + ": Failure type: " + failureType);
                }
            }
            catch (Exception)
            {
                // Diagnostics are optional and must not let a native attribute failure escape.
            }
        }
    }
}
