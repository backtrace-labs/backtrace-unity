using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    internal static class NativeClientFactory
    {
        internal static INativeClient CreateNativeClient(BacktraceConfiguration configuration, string gameObjectName, BacktraceBreadcrumbs breadcrumbs, IDictionary<string, string> attributes, ICollection<string> attachments)
        {
#if UNITY_EDITOR
            return null;
#elif UNITY_STANDALONE_WIN
            return new Windows.NativeClient(configuration, breadcrumbs, attributes, attachments);
#elif UNITY_ANDROID
            return new Android.NativeClient(configuration, breadcrumbs, attributes, attachments, gameObjectName);
#elif UNITY_IOS
            return new iOS.NativeClient(configuration, breadcrumbs, attributes, attachments);
#else
            return null;
#endif
        }
    }
}
