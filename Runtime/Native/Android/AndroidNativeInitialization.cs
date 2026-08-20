#if UNITY_ANDROID || UNITY_EDITOR
using System;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Coordinates native-bridge completion and the managed setup that follows it.
    /// The completion callback is invoked before JNI cleanup so a false result or any later
    /// cleanup or setup failure can roll back possible native side effects.
    /// </summary>
    internal static class AndroidNativeInitialization
    {
        internal static bool Execute(
            Func<Action, bool> initialize,
            Action completeSetup,
            Action rollback,
            Action<Exception> reportRollbackFailure)
        {
            if (initialize == null)
            {
                throw new ArgumentNullException("initialize");
            }
            if (completeSetup == null)
            {
                throw new ArgumentNullException("completeSetup");
            }
            if (rollback == null)
            {
                throw new ArgumentNullException("rollback");
            }

            bool nativeBridgeCompleted = false;
            try
            {
                bool initialized = initialize(() => nativeBridgeCompleted = true);
                if (!initialized)
                {
                    if (nativeBridgeCompleted)
                    {
                        TryRollback(rollback, reportRollbackFailure);
                    }

                    return false;
                }

                // Keep the transaction safe even if an initializer returns true without invoking the native-bridge completion callback.
                nativeBridgeCompleted = true;
                completeSetup();
                return true;
            }
            catch
            {
                if (nativeBridgeCompleted)
                {
                    TryRollback(rollback, reportRollbackFailure);
                }
                throw;
            }
        }

        private static void TryRollback(
            Action rollback,
            Action<Exception> reportRollbackFailure)
        {
            try
            {
                rollback();
            }
            catch (Exception rollbackFailure)
            {
                // Rollback is best-effort. Preserve the initialization outcome while still allowing a contained diagnostic.
                if (reportRollbackFailure == null)
                {
                    return;
                }

                try
                {
                    reportRollbackFailure(rollbackFailure);
                }
                catch (Exception)
                {
                    // Diagnostics must never replace or escape the original initialization outcome.
                }
            }
        }

        /// <summary>
        /// Deletes every non-zero JNI local reference in the supplied order.
        /// A failed deletion does not prevent later references from being released;
        /// the first failure is rethrown after all cleanup attempts so native setup can be rolled back.
        /// </summary>
        internal static void DeleteLocalReferences(
            Action<IntPtr> deleteLocalReference,
            params IntPtr[] references)
        {
            if (deleteLocalReference == null)
            {
                throw new ArgumentNullException("deleteLocalReference");
            }

            Exception firstFailure = null;
            if (references != null)
            {
                foreach (IntPtr reference in references)
                {
                    if (reference == IntPtr.Zero)
                    {
                        continue;
                    }
                    try
                    {
                        deleteLocalReference(reference);
                    }
                    catch (Exception exception)
                    {
                        if (firstFailure == null)
                        {
                            firstFailure = exception;
                        }
                    }
                }
            }

            if (firstFailure != null)
            {
                throw firstFailure;
            }
        }
    }
}
#endif
