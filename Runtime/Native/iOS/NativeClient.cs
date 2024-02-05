#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native.Base;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.iOS
{
    /// <summary>
    /// iOS native client 
    /// </summary>
    internal class NativeClient : NativeClientBase, INativeClient
    {
        // NSDictinary entry used only for iOS native integration
        internal struct Entry
        {
            public string Key;
            public string Value;
        }

        [DllImport("__Internal", EntryPoint = "StartBacktraceIntegration")]
        private static extern void Start(string plCrashReporterUrl, string[] attributeKeys, string[] attributeValues, int attributesSize, bool enableOomSupport, string[] attachments, int attachmentSize, bool enableClientSideUnwinding);

        [DllImport("__Internal", EntryPoint = "NativeReport")]
        private static extern void NativeReport(string message, bool setMainThreadAsFaultingThread, bool ignoreIfDebugger);

        [DllImport("__Internal", EntryPoint = "Crash")]
        private static extern string Crash();

        [DllImport("__Internal", EntryPoint = "GetAttributes")]
        private static extern void GetNativeAttributes(out IntPtr attributes, out int keysCount);

        [DllImport("__Internal", EntryPoint = "AddAttribute")]
        private static extern void AddAttribute(string key, string value);

        [DllImport("__Internal", EntryPoint = "Disable")]
        private static extern void DisableNativeIntegration();

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

        public NativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs, IDictionary<string, string> clientAttributes, ICollection<string> attachments) : base(configuration, breadcrumbs)
        {
            if (INITIALIZED || !_enabled)
            {
                return;
            }
            if (_configuration.CaptureNativeCrashes)
            {
                HandleNativeCrashes(clientAttributes, attachments);
                INITIALIZED = true;
            }
            if (_configuration.HandleANR)
            {
                HandleAnr();
            }
        }


        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes(IDictionary<string, string> attributes, IEnumerable<string> attachments)
        {
            var databasePath = _configuration.GetFullDatabasePath();
            // make sure database is enabled 
            if (string.IsNullOrEmpty(databasePath) || !Directory.Exists(databasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return;
            }

            var plcrashreporterUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetPlCrashReporterSubmissionUrl();

            // add exception.type attribute to PLCrashReporter reports
            // The library will send PLCrashReporter crashes to Backtrace
            // only when Crash occured
            attributes[ErrorTypeAttribute] = CrashType;
            var attributeKeys = attributes.Keys.ToArray();
            var attributeValues = attributes.Values.ToArray();

            Start(plcrashreporterUrl.ToString(), attributeKeys, attributeValues, attributeValues.Length, _configuration.OomReports, attachments.ToArray(), attachments.Count(), _configuration.ClientSideUnwinding);
            CaptureNativeCrashes = true;
        }

        /// <summary>
        /// Retrieve Backtrace Attributes from the Android native code.
        /// </summary>
        /// <returns>Backtrace Attributes from the Android build</returns>
        public void GetAttributes(IDictionary<string, string> result)
        {
            if (!_enabled)
            {
                return;
            }
            IntPtr pUnmanagedArray;
            int keysCount;
            GetNativeAttributes(out pUnmanagedArray, out keysCount);

            // calculate struct size for current OS.
            // We multiply by 2 because Entry struct has two pointers
            var structSize = IntPtr.Size * 2;
            const int x86StructSize = 4;
            for (int i = 0; i < keysCount; i++)
            {
                IntPtr address;
                if (structSize == x86StructSize)
                {
                    address = new IntPtr(pUnmanagedArray.ToInt32() + i * structSize);
                }
                else
                {
                    address = new IntPtr(pUnmanagedArray.ToInt64() + i * structSize);
                }
                Entry entry = (Entry)Marshal.PtrToStructure(address, typeof(Entry));
                result[entry.Key] = entry.Value;
            }

            Marshal.FreeHGlobal(pUnmanagedArray);
        }

        /// <summary>
        /// Setup iOS ANR support and set callback function when ANR happened.
        /// </summary>
        public void HandleAnr()
        {
            // if INITIALIZED is equal to false, plcrashreporter instance is disabled
            // so we can't generate native report
            if (!_enabled || INITIALIZED == false)
            {
                return;
            }

            bool reported = false;
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            AnrThread = new Thread(() =>
            {
                float lastUpdatedCache = 0;
                while (AnrThread.IsAlive && StopAnr == false)
                {
                    if (!PreventAnr)
                    {
                        if (lastUpdatedCache == 0)
                        {
                            lastUpdatedCache = LastUpdateTime;
                        }
                        else if (lastUpdatedCache == LastUpdateTime)
                        {
                            if (!reported)
                            {
                                OnAnrDetection();
                                // set temporary attribute to "Hang"
                                SetAttribute(ErrorTypeAttribute, HangType);
                                NativeReport(AnrMessage, true, true);
                                // update error.type attribute in case when crash happen 
                                SetAttribute(ErrorTypeAttribute, CrashType);
                                reported = true;
                            }
                        }
                        else
                        {
                            reported = false;
                        }


                        lastUpdatedCache = LastUpdateTime;
                    }
                    else if (lastUpdatedCache != 0)
                    {
                        // make sure when ANR happened just after going to foreground
                        // we won't false positive ANR report
                        lastUpdatedCache = 0;
                    }
                    Thread.Sleep(AnrWatchdogTimeout);

                }
            });
            AnrThread.IsBackground = true;
            AnrThread.Start();
        }



        /// <summary>
        /// Add attribute to native crash
        /// </summary>
        /// <param name="key">attribute name</param>
        /// <param name="value">attribute value</param>
        public void SetAttribute(string key, string value)
        {
            // if INITIALIZED is equal to false, we don't need to set
            // attributes or store them. AddAttibutes call to objective-c code
            // is usefull ONLY when we initialized PLCrashReporter integration
            if (!_enabled || INITIALIZED == false)
            {
                return;
            }
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return;
            }
            AddAttribute(key, value);
        }
        /// <summary>
        /// Report OOM via PlCrashReporter report.
        /// </summary>
        /// <returns>true - if native crash reprorter is enabled. Otherwise false.</returns>
        public bool OnOOM()
        {
            // if INITIALIZED is equal to false, plcrashreporter instance is disabled
            // so we can't generate native report
            if (!_enabled || INITIALIZED == false)
            {
                return false;
            }
            // oom support will be handled by native plugin - this will prevent
            // false positive reports
            // to avoid reporting low memory warning when application didn't crash 
            // native plugin will analyse previous application session             
            return true;
        }
        /// <summary>
        /// Disable native client integration
        /// </summary>
        public override void Disable()
        {
            if (CaptureNativeCrashes)
            {
                CaptureNativeCrashes = false;
                DisableNativeIntegration();
            }
            base.Disable();
        }
    }
}
#endif
