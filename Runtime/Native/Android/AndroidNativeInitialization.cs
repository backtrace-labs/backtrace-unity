#if UNITY_ANDROID || UNITY_EDITOR
using System;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Coordinates native-backend activation and the managed setup that follows it.
    /// The activation callback is invoked before JNI cleanup,
    /// a cleanup failure after a successful native initialization still rolls the backend back.
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

            bool backendActive = false;
            try
            {
                bool initialized = initialize(() => backendActive = true);
                if (!initialized)
                {
                    return false;
                }

                // Keep the transaction safe even if an initializer forgets to invoke the early activation callback after returning true.
                backendActive = true;
                completeSetup();
                return true;
            }
            catch
            {
                if (backendActive)
                {
                    try
                    {
                        rollback();
                    }
                    catch (Exception rollbackFailure)
                    {
                        // Rollback is best-effort. Preserve the setup exception, which is the actionable failure, while still allowing a contained diagnostic.
                        if (reportRollbackFailure != null)
                        {
                            try
                            {
                                reportRollbackFailure(rollbackFailure);
                            }
                            catch (Exception)
                            {
                                // Diagnostics must never replace the setup failure.
                            }
                        }
                    }
                }
                throw;
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
