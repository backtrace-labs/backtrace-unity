#if UNITY_STANDALONE_OSX || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native.Apple;

namespace Backtrace.Unity.Runtime.Native.OSX
{
    // Keep the factory's platform type; lifecycle and ABI ownership are shared.
    internal sealed class NativeClient : AppleNativeClient
    {
        public NativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs,
            IDictionary<string, string> attributes, ICollection<string> attachments)
            : base(configuration, breadcrumbs, attributes, attachments) { }

        internal NativeClient(BacktraceConfiguration configuration, AppleNativeSession session,
            AppleNativeRequest request, Func<IDisposable> captureLease)
            : base(configuration, session, request, captureLease) { }
    }
}
#endif
