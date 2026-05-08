using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Backtrace.Unity.WebGL
{
    internal static class BacktraceWebGLJavaScriptStack
    {
        internal const string AnnotationName = "WebGL JavaScript stack at capture";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string BT_CaptureJavaScriptStack();
#endif

        internal static string Capture()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return BT_CaptureJavaScriptStack() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }

        internal static Dictionary<string, string> CreateAnnotation(string javascriptStack)
        {
            return new Dictionary<string, string>
            {
                { "kind", "javascript_stack_at_backtrace_capture_time" },
                { "stack", javascriptStack ?? string.Empty },
                {
                    "note",
                    "This is a browser JavaScript stack captured when the Backtrace Unity SDK created the report. " +
                    "It is not the original managed C# throw-site stack and is not used as faulting managed frames."
                }
            };
        }
    }
}
