using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    internal static class NativeClientFactory
    {
        internal static INativeClient CreateNativeClient(BacktraceConfiguration configuration, string gameObjectName, IDictionary<string, string> attributes)
        {
#if UNITY_EDITOR
            return null;
#else
#if UNITY_ANDROID
            return new Android.NativeClient(gameObjectName, configuration, attributes);
#elif UNITY_IOS
            return new iOS.NativeClient(configuration, attributes);
#else
            return null;
#endif
#endif
        }
    }
}
