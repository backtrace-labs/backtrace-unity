namespace Backtrace.Unity.Types
{
    /// <summary>
    /// Controls optional WebGL JavaScript stack-at-capture enrichment.
    /// This stack is supplemental context only and is not used as a managed
    /// C# faulting stack.
    /// </summary>
    public enum BacktraceWebGLJavaScriptStackFallbackMode
    {
        /// <summary>
        /// Do not capture a browser JavaScript stack.
        /// </summary>
        Disabled = 0,
        /// <summary>
        /// Capture a browser JavaScript stack only when Unity sends an Error or
        /// Exception log callback with an empty managed stackTrace string.
        /// </summary>
        StacklessUnityLogsOnly = 1
    }
}
