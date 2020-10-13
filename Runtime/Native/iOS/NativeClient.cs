#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Backtrace.Unity.Model;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.iOS
{
    /// <summary>
    /// iOS native client 
    /// </summary>
    public class NativeClient : INativeClient
    {
        // NSDictinary entry used only for iOS native integration
        internal struct Entry
        {
            public string Key;
            public string Value;
        }

        [DllImport("__Internal", EntryPoint = "StartBacktraceIntegration")]
        private static extern void Start(string plCrashReporterUrl, string[] attributeKeys, string[] attributeValues, int size);

        [DllImport("__Internal", EntryPoint = "Crash")]
        public static extern string Crash();

        [DllImport("__Internal", EntryPoint = "GetAttibutes")]
        public static extern void GetNativeAttibutes(out IntPtr attributes, out int keysCount);

        [DllImport("__Internal", EntryPoint = "AddAttribute")]
        public static extern void AddAttribute(string key, string value);

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
            var backtraceAttributes = new Model.JsonData.BacktraceAttributes(null, null, true);
            //var submissionUrl = BacktraceApi.GetParametrizedQuery(plcrashreporterUrl.ToString(), backtraceAttributes.Attributes);
            var attributeKeys = backtraceAttributes.Attributes.Keys.ToArray();
            var attributeValues = backtraceAttributes.Attributes.Values.ToArray();

            Start(plcrashreporterUrl.ToString(),attributeKeys, attributeValues, attributeValues.Length);
        }


        /// <summary>
        /// Retrieve Backtrace Attributes from the Android native code.
        /// </summary>
        /// <returns>Backtrace Attributes from the Android build</returns>
        public Dictionary<string, string> GetAttributes()
        {
            var result = new Dictionary<string, string>();
            if (_enabled == false)
            {
                return result;
            }
            GetNativeAttibutes(out IntPtr pUnmanagedArray, out int keysCount);

            for (int i = 0; i < keysCount; i++)
            {
                var address = pUnmanagedArray + i * 16;
                Entry entry = Marshal.PtrToStructure<Entry>(address);
                result.Add(entry.Key, entry.Value);
            }

            Marshal.FreeHGlobal(pUnmanagedArray);
            return result;
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
            if (_enabled == false)
            {
                return;
            }
            if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return;
            }
            AddAttribute(key, value);
        }
    }
}
#endif