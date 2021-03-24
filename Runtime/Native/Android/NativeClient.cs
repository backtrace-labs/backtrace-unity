﻿using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Android native client 
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

        [DllImport("backtrace-native")]
        private static extern bool Initialize(IntPtr submissionUrl, IntPtr databasePath, IntPtr handlerPath, IntPtr keys, IntPtr values, IntPtr attachments);

        [DllImport("backtrace-native")]
        private static extern bool AddAttribute(IntPtr key, IntPtr value);

        [DllImport("backtrace-native", EntryPoint = "DumpWithoutCrash")]
        private static extern bool NativeReport(IntPtr message, bool setMainThreadAsFaultingThread);

        /// <summary>
        /// Native client built-in specific attributes
        /// </summary>
        private readonly Dictionary<string, string> _builtInAttributes = new Dictionary<string, string>();

        /// <summary>
        /// Attribute maps - list of attribute maps that allows Backtrace-Unity to rename attributes 
        /// grabbed from android specific directories
        /// </summary>
        private readonly Dictionary<string, string> _attributeMapping = new Dictionary<string, string>();

        private void SetDefaultAttributeMaps()
        {
            _attributeMapping.Add("FDSize", "descriptor.count");
            _attributeMapping.Add("VmPeak", "vm.vma.peak");
            _attributeMapping.Add("VmSize", "vm.vma.size");
            _attributeMapping.Add("VmLck", "vm.locked.size");
            _attributeMapping.Add("VmHWM", "vm.rss.peak");
            _attributeMapping.Add("VmRSS", "vm.rss.size");
            _attributeMapping.Add("VmStk", "vm.stack.size");
            _attributeMapping.Add("VmData", "vm.data");
            _attributeMapping.Add("VmExe", "vm.exe");
            _attributeMapping.Add("VmLib", "vm.shared.size");
            _attributeMapping.Add("VmPTE", "vm.pte.size");
            _attributeMapping.Add("VmSwap", "vm.swap.size");
            _attributeMapping.Add("State", "state");
            _attributeMapping.Add("voluntary_ctxt_switches", "sched.cs.voluntary");
            _attributeMapping.Add("nonvoluntary_ctxt_switches", "sched.cs.involuntary");
            _attributeMapping.Add("SigPnd", "vm.sigpnd");
            _attributeMapping.Add("ShdPnd", "vm.shdpnd");
            _attributeMapping.Add("Threads", "vm.threads");
            _attributeMapping.Add("MemTotal", "system.memory.total");
            _attributeMapping.Add("MemFree", "system.memory.free");
            _attributeMapping.Add("Buffers", "system.memory.buffers");
            _attributeMapping.Add("Cached", "system.memory.cached");
            _attributeMapping.Add("SwapCached", "system.memory.swap.cached");
            _attributeMapping.Add("Active", "system.memory.active");
            _attributeMapping.Add("Inactive", "system.memory.inactive");
            _attributeMapping.Add("SwapTotal", "system.memory.swap.total");
            _attributeMapping.Add("SwapFree", "system.memory.swap.free");
            _attributeMapping.Add("Dirty", "system.memory.dirty");
            _attributeMapping.Add("Writeback", "system.memory.writeback");
            _attributeMapping.Add("Slab", "system.memory.slab");
            _attributeMapping.Add("VmallocTotal", "system.memory.vmalloc.total");
            _attributeMapping.Add("VmallocUsed", "system.memory.vmalloc.used");
            _attributeMapping.Add("VmallocChunk", "system.memory.vmalloc.chunk");
        }

        private readonly BacktraceConfiguration _configuration;
        // Android native interface paths
        private const string _namespace = "backtrace.io.backtrace_unity_android_plugin";
        private readonly string _anrPath = string.Format("{0}.{1}", _namespace, "BacktraceANRWatchdog");

        /// <summary>
        /// Determine if android integration should be enabled
        /// </summary>
        private bool _enabled =
#if UNITY_ANDROID && !UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <summary>
        /// Anr watcher object
        /// </summary>
        private AndroidJavaObject _anrWatcher;

        private bool _captureNativeCrashes = false;
        private readonly bool _handlerANR = false;
        public NativeClient(string gameObjectName, BacktraceConfiguration configuration)
        {
            _configuration = configuration;
            SetDefaultAttributeMaps();
            if (!_enabled)
            {
                return;
            }

#if UNITY_ANDROID
            _handlerANR = _configuration.HandleANR;
            HandleNativeCrashes();
            HandleAnr(gameObjectName, "OnAnrDetected");

            // read device manufacturer
            using (var build = new AndroidJavaClass("android.os.Build"))
            {
                _builtInAttributes["device.manufacturer"] = build.GetStatic<string>("MANUFACTURER").ToString();
            }
#endif
        }

        /// <summary>
        /// Get path to the native libraries directory
        /// </summary>
        /// <returns>Path to the native libraries directory</returns>
        private string GetNativeDirectoryPath()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var applicationInfo = context.Call<AndroidJavaObject>("getApplicationInfo"))
            {
                return applicationInfo.Get<string>("nativeLibraryDir");
            }
        }

        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes()
        {
            // make sure database is enabled 
            var integrationDisabled =
#if UNITY_ANDROID
                !_configuration.CaptureNativeCrashes || !_configuration.Enabled;
#else
                true;
#endif
            if (integrationDisabled)
            {
                Debug.LogWarning("Backtrace native integration status: Disabled NDK integration");
                return;
            }
            var databasePath = _configuration.CrashpadDatabasePath;
            if (string.IsNullOrEmpty(databasePath) || !Directory.Exists(_configuration.GetFullDatabasePath()))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return;
            }
            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }

            // crashpad is available only for API level 21+ 
            // make sure we don't want ot start crashpad handler 
            // on the unsupported API
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int apiLevel = version.GetStatic<int>("SDK_INT");
                _builtInAttributes["device.sdk"] = apiLevel.ToString();
                if (apiLevel < 21)
                {
                    Debug.LogWarning("Backtrace native integration status: Unsupported Android API level");
                    return;
                }
            }

            var libDirectory = GetNativeDirectoryPath();
            if (!Directory.Exists(libDirectory))
            {
                return;
            }
            const string crashpadHandlerName = "libcrashpad_handler.so";
            var crashpadHandlerPath = Path.Combine(libDirectory, crashpadHandlerName);

            if (string.IsNullOrEmpty(crashpadHandlerPath))
            {
                Debug.LogWarning("Backtrace native integration status: Cannot find crashpad library");
                return;
            }
            // get default built-in Backtrace-Unity attributes
            var backtraceAttributes = new BacktraceAttributes(null, null, true).Attributes;

            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();
            var attachments = _configuration.GetAttachmentPaths().ToArray();

            // reassign to captureNativeCrashes
            // to avoid doing anything on crashpad binary, when crashpad
            // isn't available
            _captureNativeCrashes = Initialize(
                AndroidJNI.NewStringUTF(minidumpUrl),
                AndroidJNI.NewStringUTF(databasePath),
                AndroidJNI.NewStringUTF(crashpadHandlerPath),
                AndroidJNIHelper.ConvertToJNIArray(backtraceAttributes.Keys.ToArray()),
                AndroidJNIHelper.ConvertToJNIArray(backtraceAttributes.Values.ToArray()),
                AndroidJNIHelper.ConvertToJNIArray(attachments));
            if (!_captureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Crashpad client");
            }

            // add exception type to crashes handled by crashpad - all exception handled by crashpad 
            // by default we setting this option here, to set error.type when unexpected crash happen (so attribute will present)
            // otherwise in other methods - ANR detection, OOM handler, we're overriding it and setting it back to "crash"

            // warning 
            // don't add attributes that can change over the time to initialization method attributes. Crashpad will prevent from 
            // overriding them on game runtime. ANRs/OOMs methods can override error.type attribute, so we shouldn't pass error.type 
            // attribute via attributes parameters.
            AddAttribute(
                        AndroidJNI.NewStringUTF("error.type"),
                        AndroidJNI.NewStringUTF("Crash"));
        }

        /// <summary>
        /// Retrieve Backtrace Attributes from the Android native code.
        /// </summary>
        /// <returns>Backtrace Attributes from the Android build</returns>
        public void GetAttributes(Dictionary<string, string> result)
        {
            if (!_enabled)
            {
                return;
            }
            // rewrite built in attributes to report attributes
            foreach (var builtInAttribute in _builtInAttributes)
            {
                result.Add(builtInAttribute.Key, builtInAttribute.Value);
            }

            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var filesToRead = new string[2] { $"/proc/{processId}/status", "/proc/meminfo" };
            foreach (var diagnosticFilePath in filesToRead)
            {
                if (!File.Exists(diagnosticFilePath))
                {
                    continue;
                }
                foreach (var line in File.ReadLines(diagnosticFilePath))
                {
                    string[] entries = line.Split(':');
                    if (entries.Length != 2)
                    {
                        continue;
                    }
                    var key = entries[0].Trim();
                    if (!_attributeMapping.ContainsKey(key))
                    {
                        continue;
                    }
                    key = _attributeMapping[key];
                    var value = entries[1];
                    if (value.EndsWith("kB"))
                    {
                        value = value.Substring(0, value.LastIndexOf("k")).Trim();
                    }
                    result.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Setup Android ANR support and set callback function when ANR happened.
        /// </summary>
        /// <param name="gameObjectName">Backtrace game object name</param>
        /// <param name="callbackName">Callback function name</param>
        public void HandleAnr(string gameObjectName, string callbackName)
        {
            if (!_handlerANR)
            {
                return;
            }
            try
            {
                _anrWatcher = new AndroidJavaObject(_anrPath, gameObjectName, callbackName);
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("Cannot initialize ANR watchdog - reason: {0}", e.Message));
                _enabled = false;
            }

            if (!_captureNativeCrashes)
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

                                reported = true;
                                if (AndroidJNI.AttachCurrentThread() == 0)
                                {
                                    // set temporary attribute to "Hang"
                                    AddAttribute(
                                        AndroidJNI.NewStringUTF("error.type"),
                                        AndroidJNI.NewStringUTF("Hang"));

                                    NativeReport(AndroidJNI.NewStringUTF("ANRException: Blocked thread detected."), true);
                                    // update error.type attribute in case when crash happen 
                                    SetAttribute("error.type", "Crash");
                                }
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
        /// Set Backtrace-Android crashpad crash attributes
        /// </summary>
        /// <param name="key">Attribute key</param>
        /// <param name="value">Attribute value</param>
        public void SetAttribute(string key, string value)
        {
            if (!_captureNativeCrashes || string.IsNullOrEmpty(key))
            {
                return;
            }
            // avoid null reference in crashpad source code
            if (value == null)
            {
                value = string.Empty;
            }

            AddAttribute(
                AndroidJNI.NewStringUTF(key),
                AndroidJNI.NewStringUTF(value));
        }

        /// <summary>
        /// Report OOM via Backtrace native android library.
        /// </summary>
        /// <returns>true - if native crash reprorter is enabled. Otherwise false.</returns>
        public bool OnOOM()
        {
            SetAttribute("memory.warning", "true");
            SetAttribute("memory.warning.date", DateTime.Now.ToString(CultureInfo.InvariantCulture));
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