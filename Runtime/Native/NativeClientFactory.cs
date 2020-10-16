using Backtrace.Unity.Model;

namespace Backtrace.Unity.Runtime.Native
{
    internal static class NativeClientFactory
    {
        internal static INativeClient GetNativeClient(BacktraceConfiguration configuration, string gameObjectName)
        {
#if UNITY_EDITOR
            return null;
#else
#if UNITY_ANDROID
            return new Android.NativeClient(gameObjectName, configuration);
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            return new iOS.NativeClient(gameObjectName, configuration);
#else
            return null;
#endif
#endif
        }
    }
}
