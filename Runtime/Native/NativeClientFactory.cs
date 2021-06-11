using Backtrace.Unity.Model;
using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    internal static class NativeClientFactory
    {
        internal static INativeClient CreateNativeClient(BacktraceConfiguration configuration, string gameObjectName, IDictionary<string, string> attributes, ICollection<string> attachments)
        {
#if UNITY_EDITOR
            return null;
#else
#if UNITY_ANDROID
            return new Android.NativeClient(gameObjectName, configuration, attributes, attachments);
#elif UNITY_IOS
            return new iOS.NativeClient(configuration, attributes, attachments);
#else
            return null;
#endif
#endif
        }
    }
}
