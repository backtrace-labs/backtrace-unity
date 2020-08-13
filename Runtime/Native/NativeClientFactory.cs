using Backtrace.Unity.Model;

namespace Backtrace.Unity.Runtime.Native
{
    internal static class NativeClientFactory
    {
        internal static INativeClient GetNativeClient(BacktraceConfiguration configuration, string gameObjectName)
        {
#if UNITY_ANDROID 
            return new Android.NativeClient(gameObjectName, configuration);
#else
            return null;
#endif
        }
    }
}
