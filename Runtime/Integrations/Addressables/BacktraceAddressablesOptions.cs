#if BACKTRACE_UNITY_ADDRESSABLES
namespace Backtrace.Unity.Integrations
{
    public sealed class BacktraceAddressablesOptions
    {
        /// <summary>
        /// Preserve existing Addressables logging behavior by forwarding to the previous
        /// ResourceManager.ExceptionHandler or Addressables.LogException after Backtrace capture.
        /// </summary>
        public bool ForwardToUnityLogging = true;

        /// <summary>
        /// Prevent the forwarded Debug.LogException path from creating a duplicate
        /// Backtrace Unity log-callback report.
        /// </summary>
        public bool SuppressForwardedUnityLogReport = true;

        /// <summary>
        /// Maximum string size stored in the Addressables annotation.
        /// </summary>
        public int MaxAnnotationValueLength = 8 * 1024;
    }
}
#endif
