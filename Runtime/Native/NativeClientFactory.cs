using Backtrace.Unity.Model;
using System.Collections.Generic;

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
#elif UNITY_IOS
            return new iOS.NativeClient(configuration);
#else
            return null;
#endif
#endif
        }
    }
}
