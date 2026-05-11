namespace Backtrace.Unity.Types
{
    /// <summary>
    /// Controls whether Backtrace wraps Debug.unityLogger.logHandler to capture
    /// original Exception objects passed through Debug.LogException before Unity
    /// reduces them to log-callback messages.
    /// </summary>
    public enum BacktraceUnityLogHandlerExceptionCaptureMode
    {
        /// <summary>
        /// Enable on WebGL builds and disable elsewhere.
        /// </summary>
        Automatic = 0,
        /// <summary>
        /// Do not install the Backtrace Unity log-handler wrapper.
        /// </summary>
        Disabled = 1,
        /// <summary>
        /// Always install the Backtrace Unity log-handler wrapper.
        /// </summary>
        Enabled = 2
    }
}
