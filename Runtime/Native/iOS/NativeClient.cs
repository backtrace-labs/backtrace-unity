#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.iOS
{
    /// <summary>
    /// iOS native client 
    /// </summary>
    public class NativeClient : INativeClient
    {
        // NSDictinary entry used only for iOS native integration
        private struct Entry
        {
            public string Key;
            public string Value;
        }

        [DllImport("__Internal", EntryPoint = "StartBacktraceIntegration")]
        private static extern void Start(string plCrashReporterUrl);


        [DllImport("__Internal", EntryPoint = "Crash")]
        public static extern string Crash();

        [DllImport("__Internal", EntryPoint = "GetAttibutes")]
        public static extern string GetNativeAttibutes(out IntPtr stingArray, out int keysCount);

        private static bool INITIALIZED = false;

        /// <summary>
        /// Determine if ios integration should be enabled
        /// </summary>
        private readonly bool _enabled =
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public NativeClient(string gameObjectName, BacktraceConfiguration configuration)
        {
            if (INITIALIZED || !_enabled)
            {
                return;
            }
            if(configuration.CaptureNativeCrashes)
            {
                HandleNativeCrashes(configuration);
            }
            INITIALIZED = true;
        }

        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes(BacktraceConfiguration configuration)
        {
            var plcrashreporterUrl = new BacktraceCredentials(configuration.GetValidServerUrl()).GetPlCrashReporterSubmissionUrl();
            var backtraceAttributes = new BacktraceAttributes(null, null, true);
            var submissionUrl = BacktraceApi.GetParametrizedQuery(plcrashreporterUrl.ToString(), backtraceAttributes.Attributes);
            Start(submissionUrl);
        }


        /// <summary>
        /// Retrieve Backtrace Attributes from the Android native code.
        /// </summary>
        /// <returns>Backtrace Attributes from the Android build</returns>
        public Dictionary<string, string> GetAttributes()
        {
            var dic = new Dictionary<string, string>();

            GetNativeAttibutes(out IntPtr pUnmanagedArray, out int keysCount);

            IntPtr[] pIntPtrArray = new IntPtr[keysCount];

            // This was the original problem.
            // Now it copies the native array pointers to individual IntPtr. Which now they point to individual structs.
            Marshal.Copy(pUnmanagedArray, pIntPtrArray, 0, keysCount);

            for (int i = 0; i < keysCount; i++)
            {
                Entry entry = Marshal.PtrToStructure<Entry>(pIntPtrArray[i]); // Magic!
                dic.Add(entry.Key, entry.Value);

                Marshal.FreeHGlobal(pIntPtrArray[i]); // Free the individual struct malloc
            }

            Marshal.FreeHGlobal(pUnmanagedArray); // Free native array of pointers malloc.

            return dic;
        }

        /// <summary>
        /// Setup Android ANR support and set callback function when ANR happened.
        /// </summary>
        /// <param name="gameObjectName">Backtrace game object name</param>
        /// <param name="callbackName">Callback function name</param>
        public void HandleAnr(string gameObjectName, string callbackName)
        {
            Debug.Log("ANR support on iOS is still unsupported.");
        }

        public void SetAttribute(string key, string value)
        {
            Debug.Log("Custom report attribuets on iOS are unsupported.");
        }
    }
}
#endif