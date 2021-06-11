#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Backtrace.Unity.Model;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.iOS
{
    /// <summary>
    /// iOS native client 
    /// </summary>
    internal class NativeClient : INativeClient
    {
        // Last Backtrace client update time 
        volatile internal float _lastUpdateTime;

        /// <summary>
        /// Determine if the ANR background thread should be disabled or not 
        /// for some period of time.
        /// This option will be used by the native client implementation
        /// once application goes to background/foreground
        /// </summary>
        volatile internal bool _preventAnr = false;

        /// <summary>
        /// Determine if ANR thread should exit
        /// </summary>
        volatile internal bool _stopAnr = false;

        private Thread _anrThread;

        // NSDictinary entry used only for iOS native integration
        internal struct Entry
        {
            public string Key;
            public string Value;
        }

        [DllImport("__Internal", EntryPoint = "StartBacktraceIntegration")]
        private static extern void Start(string plCrashReporterUrl, string[] attributeKeys, string[] attributeValues, int attributesSize, bool enableOomSupport, string[] attachments, int attachmentSize);

        [DllImport("__Internal", EntryPoint = "NativeReport")]
        private static extern void NativeReport(string message, bool setMainThreadAsFaultingThread);

        [DllImport("__Internal", EntryPoint = "Crash")]
        private static extern string Crash();

        [DllImport("__Internal", EntryPoint = "GetAttributes")]
        private static extern void GetNativeAttributes(out IntPtr attributes, out int keysCount);

        [DllImport("__Internal", EntryPoint = "AddAttribute")]
        private static extern void AddAttribute(string key, string value);

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

        public NativeClient(BacktraceConfiguration configuration, IDictionary<string, string> clientAttributes, ICollection<string> attachments)
        {
            if (INITIALIZED || !_enabled)
            {
                return;
            }
            if (configuration.CaptureNativeCrashes)
            {
                HandleNativeCrashes(configuration, clientAttributes, attachments);
                INITIALIZED = true;
            }
            if (configuration.HandleANR)
            {
                // iOS integration doesn't require to pass game object name or callback function
                // it's required by android to know which one object to call when ANR was detected 
                // in Java. In this situation we simply ignore them.
                HandleAnr();
            }
        }


        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes(BacktraceConfiguration configuration, IDictionary<string, string> attributes, IEnumerable<string> attachments)
        {
            var databasePath = configuration.GetFullDatabasePath();
            // make sure database is enabled 
            if (string.IsNullOrEmpty(databasePath) || !Directory.Exists(databasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return;
            }

            var plcrashreporterUrl = new BacktraceCredentials(configuration.GetValidServerUrl()).GetPlCrashReporterSubmissionUrl();

            // add exception.type attribute to PLCrashReporter reports
            // The library will send PLCrashReporter crashes to Backtrace
            // only when Crash occured
            attributes["error.type"] = "Crash";
            var attributeKeys = attributes.Keys.ToArray();
            var attributeValues = attributes.Values.ToArray();

            Start(plcrashreporterUrl.ToString(), attributeKeys, attributeValues, attributeValues.Length, configuration.OomReports, attachments.ToArray(), attachments.Count());
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
            GetNativeAttributes(out IntPtr pUnmanagedArray, out int keysCount);

            // calculate struct size for current OS.
            // We multiply by 2 because Entry struct has two pointers
            var structSize = IntPtr.Size * 2;
            for (int i = 0; i < keysCount; i++)
            {
                var address = pUnmanagedArray + i * structSize;
                Entry entry = Marshal.PtrToStructure<Entry>(address);
                result[entry.Key] = entry.Value;
            }

            Marshal.FreeHGlobal(pUnmanagedArray);
        }

        /// <summary>
        /// Setup iOS ANR support and set callback function when ANR happened.
        /// </summary>
        public void HandleAnr(string gameObjectName = "", string callbackName = "")
        {
            // if INITIALIZED is equal to false, plcrashreporter instance is disabled
            // so we can't generate native report
            if (!_enabled || INITIALIZED == false)
            {
                return;
            }

            bool reported = false;
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _anrThread = new Thread(() =>
            {
                float lastUpdatedCache = 0;
                while (_anrThread.IsAlive && _stopAnr == false)
                {
                    if (!_preventAnr)
                    {
                        if (lastUpdatedCache == 0)
                        {
                            lastUpdatedCache = _lastUpdateTime;
                        }
                        else if (lastUpdatedCache == _lastUpdateTime)
                        {
                            if (!reported)
                            {
                                // set temporary attribute to "Hang"
                                SetAttribute("error.type", "Hang");
                                NativeReport("ANRException: Blocked thread detected.", true);
                                // update error.type attribute in case when crash happen 
                                SetAttribute("error.type", "Crash");
                                reported = true;
                            }
                        }
                        else
                        {
                            reported = false;
                        }


                        lastUpdatedCache = _lastUpdateTime;
                    }
                    else if (lastUpdatedCache != 0)
                    {
                        // make sure when ANR happened just after going to foreground
                        // we won't false positive ANR report
                        lastUpdatedCache = 0;
                    }
                    Thread.Sleep(5000);

                }
            });
            _anrThread.IsBackground = true;
            _anrThread.Start();
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
        /// Update native client internal timer.
        /// </summary>
        /// <param name="time">Current time</param>
        public void UpdateClientTime(float time)
        {
            _lastUpdateTime = time;
        }

        /// <summary>
        /// Disable native client integration
        /// </summary>
        public void Disable()
        {
            if (_anrThread != null)
            {
                _stopAnr = true;
            }
        }

        /// <summary>
        /// Pause ANR detection
        /// </summary>
        /// <param name="stopAnr">True - if native client should pause ANR detection"</param>
        public void PauseAnrThread(bool stopAnr)
        {
            _preventAnr = stopAnr;
        }
    }
}
#endif
