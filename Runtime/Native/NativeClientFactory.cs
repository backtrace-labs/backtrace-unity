using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backtrace.Unity.Runtime.Native
{
    internal class NativeClientFactory
    {
        internal static INativeClient GetNativeClient(BacktraceConfiguration configuration, string gameObjectName)
        {
#if UNITY_ANDROID 
            return new Android.NativeClient(gameObjectName, configuration.HandleANR);
#else
            return null;
#endif
        }
    }
}
