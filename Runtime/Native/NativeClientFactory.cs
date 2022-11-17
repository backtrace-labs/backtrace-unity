using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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

        internal static bool EnableCrashLoopDetection()
        {
#if UNITY_ANDROID
            return Android.NativeClient.EnableCrashLoopDetection();
#else
            return false;
#endif
        }

        internal static bool IsSafeModeRequired(string databasePath)
        {
#if UNITY_ANDROID
            return Android.NativeClient.IsSafeModeRequired(AndroidJNI.NewStringUTF(databasePath));
#else
            return false;
#endif
        }

        public static int ConsecutiveCrashesCount(string databasePath)
        {
#if UNITY_ANDROID
            return Android.NativeClient.ConsecutiveCrashesCount(AndroidJNI.NewStringUTF(databasePath));
#else
            return 0;
#endif
        }

        /*
                    // Performing check if we need to turn safe mode on

            Debug.LogWarning("BTCLD - Enabling");
            EnableCrashLoopDetection();
            Debug.LogWarning("BTCLD - Enabled");

            Debug.LogWarning("BTCLD - Checking if Safe Mode is required");
            if(IsSafeModeRequired("."))
            {
                Debug.LogWarning("BTCLD - Safe Mode IS required");
                return;
            }
            int count = ConsecutiveCrashesCount(".");
            Debug.LogWarning(string.Format("BTCLD - consecutive crashes: {0}", count));
            Debug.LogWarning("BTCLD - Safe Mode IS NOT required. Turning Backtrace ON");

            // TODO: find correct DB path

        */
    }
}
