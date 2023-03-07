#if UNITY_ANDROID
using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native.Base;
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
    internal sealed class NativeClient : NativeClientBase, INativeClient
    {
        private const string CallbackMethodName = "OnAnrDetected";
        [DllImport("backtrace-native")]
        private static extern bool Initialize(IntPtr submissionUrl, IntPtr databasePath, IntPtr handlerPath, IntPtr keys, IntPtr values, IntPtr attachments, bool enableClientSideUnwinding, int unwindingMode);

        [DllImport("backtrace-native")]
        private static extern bool AddAttribute(IntPtr key, IntPtr value);

        [DllImport("backtrace-native", EntryPoint = "DumpWithoutCrash")]
        private static extern bool NativeReport(IntPtr message, bool setMainThreadAsFaultingThread);

        [DllImport("backtrace-native", EntryPoint = "Disable")]
        private static extern bool DisableNativeIntegration();

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
        // Android native interface paths
        private const string _namespace = "backtrace.io.backtrace_unity_android_plugin";

        /// <summary>
        /// unwinding mode
        /// </summary>
        private UnwindingMode UnwindingMode = UnwindingMode.LOCAL_DUMPWITHOUTCRASH;

        /// <summary>
        /// Path to class responsible for detecting ANRs occurred by Java code.
        /// </summary>
        private readonly string _anrPath = string.Format("{0}.{1}", _namespace, "BacktraceANRWatchdog");

        /// <summary>
        /// Path to class responsible for capturing unhandled java exceptions.
        /// </summary>
        private readonly string _unhandledExceptionPath = string.Format("{0}.{1}", _namespace, "BacktraceAndroidBackgroundUnhandledExceptionHandler");

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

        /// <summary>
        /// Unhandled exception watcher object reference
        /// </summary>
        private AndroidJavaObject _unhandledExceptionWatcher;

        private readonly bool _enableClientSideUnwinding = false;
        public string GameObjectName { get; internal set; }
        public NativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs, IDictionary<string, string> clientAttributes, IEnumerable<string> attachments, string gameObjectName) : base(configuration, breadcrumbs)
        {
            GameObjectName = gameObjectName;
            SetDefaultAttributeMaps();
            if (!_enabled)
            {
                return;
            }
#if UNITY_2019_2_OR_NEWER
            _enableClientSideUnwinding = _configuration.ClientSideUnwinding;
#endif
            HandlerANR = _configuration.HandleANR;
            // read device manufacturer
            using (var build = new AndroidJavaClass("android.os.Build"))
            {
                const string deviceManufacturerKey = "device.manufacturer";
                _builtInAttributes[deviceManufacturerKey] = build.GetStatic<string>("MANUFACTURER").ToString();
            }
            HandleNativeCrashes(clientAttributes, attachments);
            if (!configuration.ReportFilterType.HasFlag(Types.ReportFilterType.Hang))
            {
                HandleAnr();
            }
            if (configuration.HandleUnhandledExceptions && !configuration.ReportFilterType.HasFlag(Types.ReportFilterType.UnhandledException))
            {
                HandleUnhandledExceptions();
            }
        }

        /// <summary>
        /// Setup communication between Untiy and Android to receive information about unhandled thread exceptions
        /// </summary>
        /// <param name="callbackName">Game object callback method name</param>
        private void HandleUnhandledExceptions()
        {
            const string callbackName = "HandleUnhandledExceptionsFromAndroidBackgroundThread";
            try
            {
                _unhandledExceptionWatcher = new AndroidJavaObject(_unhandledExceptionPath, GameObjectName, callbackName);
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("Cannot initialize unhandled exception watcher - reason: {0}", e.Message));
            }
        }

        /// <summary>
        /// Get path to the native libraries directory
        /// </summary>
        /// <returns>Path to the native libraries directory</returns>
        private string GetNativeDirectoryPath()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                // handle specific case when unity player is not available or available under different name
                // this case might happen for example in flutter.
                if (unityPlayer == null)
                {
                    return string.Empty;
                }
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // handle specific case when current activity is not available
                    // this case might happen for example in flutter.
                    if (activity == null)
                    {
                        return string.Empty;
                    }
                    using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                    using (var applicationInfo = context.Call<AndroidJavaObject>("getApplicationInfo"))
                    {
                        return applicationInfo.Get<string>("nativeLibraryDir");
                    }
                }
            }
        }

        /// <summary>
        /// Guess native directory path based on the data path directory. 
        /// GetNativeDirectoryPath method might return empty value when activity is not available
        /// this might happen in flutter apps.
        /// </summary>
        /// <returns>Guessed path to lib directory</returns>
        private string GuessNativeDirectoryPath()
        {
            var sourceDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "lib");
            if (!Directory.Exists(sourceDirectory))
            {
                return string.Empty;
            }
            var libDirectory = Directory.GetDirectories(sourceDirectory);
            if (libDirectory.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return libDirectory[0];
            }
        }

        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes(IDictionary<string, string> backtraceAttributes, IEnumerable<string> attachments)
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
            if (string.IsNullOrEmpty(libDirectory) || !Directory.Exists(libDirectory))
            {
                libDirectory = GuessNativeDirectoryPath();
            }
            if (!Directory.Exists(libDirectory))
            {
                return;
            }
            const string crashpadHandlerName = "libcrashpad_handler.so";
            var crashpadHandlerPath = Path.Combine(libDirectory, crashpadHandlerName);

            if (string.IsNullOrEmpty(crashpadHandlerPath) || !File.Exists(crashpadHandlerPath))
            {
                Debug.LogWarning("Backtrace native integration status: Cannot find crashpad library");
                return;
            }

            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();

            // reassign to captureNativeCrashes
            // to avoid doing anything on crashpad binary, when crashpad isn't available
            CaptureNativeCrashes = Initialize(
                AndroidJNI.NewStringUTF(minidumpUrl),
                AndroidJNI.NewStringUTF(databasePath),
                AndroidJNI.NewStringUTF(crashpadHandlerPath),
                AndroidJNIHelper.ConvertToJNIArray(new string[0]),
                AndroidJNIHelper.ConvertToJNIArray(new string[0]),
                AndroidJNIHelper.ConvertToJNIArray(attachments.ToArray()),
                _enableClientSideUnwinding,
                (int)UnwindingMode);
            if (!CaptureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Crashpad client");
                return;
            }
            foreach (var attribute in backtraceAttributes)
            {
                AddAttribute(AndroidJNI.NewStringUTF(attribute.Key), AndroidJNI.NewStringUTF(attribute.Value));
            }

            // add native client built-in attributes
            foreach (var attribute in _builtInAttributes)
            {
                AddAttribute(AndroidJNI.NewStringUTF(attribute.Key), AndroidJNI.NewStringUTF(attribute.Value));
            }

            // add exception type to crashes handled by crashpad - all exception handled by crashpad 
            // by default we setting this option here, to set error.type when unexpected crash happen (so attribute will present)
            // otherwise in other methods - ANR detection, OOM handler, we're overriding it and setting it back to "crash"

            // warning 
            // don't add attributes that can change over the time to initialization method attributes. Crashpad will prevent from 
            // overriding them on game runtime. ANRs/OOMs methods can override error.type attribute, so we shouldn't pass error.type 
            // attribute via attributes parameters.
            AddAttribute(
                        AndroidJNI.NewStringUTF(ErrorTypeAttribute),
                        AndroidJNI.NewStringUTF(CrashType));
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
            // rewrite built in attributes to report attributes
            foreach (var builtInAttribute in _builtInAttributes)
            {
                result[builtInAttribute.Key] = builtInAttribute.Value;
            }

            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var filesToRead = new string[2] { string.Format("/proc/{0}/status", processId), "/proc/meminfo" };
            foreach (var diagnosticFilePath in filesToRead)
            {
                if (!File.Exists(diagnosticFilePath))
                {
                    continue;
                }
                foreach (var line in File.ReadAllLines(diagnosticFilePath))
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
                    result[key] = value;
                }
            }
        }

        public void FinishUnhandledBackgroundException()
        {
            if (_unhandledExceptionWatcher == null)
            {
                return;
            }
            _unhandledExceptionWatcher.Call("finish");
        }

        /// <summary>
        /// Setup Android ANR support and set callback function when ANR happened.
        /// </summary>
        public void HandleAnr()
        {
            if (!HandlerANR)
            {
                return;
            }
            try
            {
                _anrWatcher = new AndroidJavaObject(_anrPath, GameObjectName, CallbackMethodName, AnrWatchdogTimeout);
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("Cannot initialize ANR watchdog - reason: {0}", e.Message));
            }

            if (!CaptureNativeCrashes)
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
                                reported = true;
                                if (AndroidJNI.AttachCurrentThread() == 0)
                                {
                                    // set temporary attribute to "Hang"
                                    AddAttribute(
                                        AndroidJNI.NewStringUTF(ErrorTypeAttribute),
                                        AndroidJNI.NewStringUTF(HangType));

                                    NativeReport(AndroidJNI.NewStringUTF(AnrMessage), true);
                                    // update error.type attribute in case when crash happen 
                                    AddAttribute(
                                        AndroidJNI.NewStringUTF(ErrorTypeAttribute),
                                        AndroidJNI.NewStringUTF(CrashType));
                                }
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
        /// Set Backtrace-Android crashpad crash attributes
        /// </summary>
        /// <param name="key">Attribute key</param>
        /// <param name="value">Attribute value</param>
        public void SetAttribute(string key, string value)
        {
            if (!CaptureNativeCrashes || string.IsNullOrEmpty(key))
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
            SetAttribute("memory.warning.date", DateTimeHelper.Timestamp().ToString(CultureInfo.InvariantCulture));
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
            if (_anrWatcher != null)
            {
                _anrWatcher.Call("stopMonitoring");
                _anrWatcher.Dispose();
                _anrWatcher = null;
            }
            if (_unhandledExceptionWatcher != null)
            {
                _unhandledExceptionWatcher.Call("stopMonitoring");
                _unhandledExceptionWatcher.Dispose();
                _unhandledExceptionWatcher = null;
            }
            base.Disable();
        }
    }
}
#endif
