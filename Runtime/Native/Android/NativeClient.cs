#if UNITY_ANDROID
using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native.Base;
using System;
using System.Collections;
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
        private static extern bool InitializeJavaCrashHandler(IntPtr submissionUrl, IntPtr databasePath, IntPtr classPath, IntPtr keys, IntPtr values, IntPtr attachments, IntPtr environmentVariables);

        [DllImport("backtrace-native")]
        private static extern bool AddAttribute(IntPtr key, IntPtr value);

        [DllImport("backtrace-native", EntryPoint = "DumpWithoutCrash")]
        private static extern bool NativeReport(IntPtr message, bool setMainThreadAsFaultingThread);

        [DllImport("backtrace-native", EntryPoint = "Disable")]
        private static extern bool DisableNativeIntegration();

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

        // Android base native interface path
        private const string _baseNamespace = "backtraceio";

        // Unity-Android native interface path
        private const string _namespace = "backtraceio.unity";

        /// <summary>
        /// Path to class responsible for detecting ANRs occurred by Java code.
        /// </summary>
        private readonly string _anrPath = string.Format("{0}.{1}", _namespace, "BacktraceANRWatchdog");

        /// <summary>
        /// Path to class responsible for capturing unhandled java exceptions.
        /// </summary>
        private readonly string _unhandledExceptionPath = string.Format("{0}.{1}", _namespace, "BacktraceAndroidBackgroundUnhandledExceptionHandler");

        /// <summary>
        /// Path to class responsible for generating and sending native dump on crash
        /// </summary>
        private readonly string _crashHandlerPath = string.Format("{0}.library.nativeCalls.BacktraceCrashHandler", _baseNamespace);

        /// <summary>
        /// Backtrace-Android native library name
        /// </summary>
        private readonly string _nativeLibraryName = "libbacktrace-native.so";

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

        public string GameObjectName { get; internal set; }
        public NativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs, IDictionary<string, string> clientAttributes, IEnumerable<string> attachments, string gameObjectName) : base(configuration, breadcrumbs)
        {
            GameObjectName = gameObjectName;
            SetDefaultAttributeMaps();
            if (!_enabled)
            {
                return;
            }

            HandlerANR = _configuration.HandleANR;
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
        /// Setup communication between Unity and Android to receive information about unhandled thread exceptions
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
            var fullDatabasePath = _configuration.GetFullDatabasePath();

            if (string.IsNullOrEmpty(databasePath) || string.IsNullOrEmpty(fullDatabasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path undefined");
                return;
            }
            if (!Directory.Exists(fullDatabasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return;
            }
            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }

            var apiLevelString = backtraceAttributes["device.sdk"];
            int apiLevel;
            if (apiLevelString == null || !int.TryParse(apiLevelString, out apiLevel))
            {
                Debug.LogWarning("Backtrace native integration status: Cannot determine Android API level");
                return;
            }

            // crashpad is available only for API level 21+ 
            // make sure we don't want ot start crashpad handler 
            // on the unsupported API
            if (apiLevel < 21)
            {
                Debug.LogWarning("Backtrace native integration status: Unsupported Android API level");
                return;
            }

            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();
            
            // Resolve native library directory
            var libDirectory = GetNativeDirectoryPath();
            if (string.IsNullOrEmpty(libDirectory) || !Directory.Exists(libDirectory))
            {
                libDirectory = GuessNativeDirectoryPath();
            }

            if (string.IsNullOrEmpty(libDirectory) || !Directory.Exists(libDirectory))
            {
                Debug.LogWarning("Backtrace native integration status: Cannot resolve native library directory");
                return;
            }

            CaptureNativeCrashes = InitializeJavaCrashHandler(minidumpUrl, databasePath, backtraceAttributes["device.abi"], libDirectory, attachments);
            
            if (!CaptureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Java crash handler");
                return;
            }

            foreach (var attribute in backtraceAttributes)
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
            AddAttribute(AndroidJNI.NewStringUTF(ErrorTypeAttribute), AndroidJNI.NewStringUTF(CrashType));
        }

        private bool InitializeJavaCrashHandler(String minidumpUrl, String databasePath, String deviceAbi, String nativeDirectory, IEnumerable<String> attachments) {
            if (String.IsNullOrEmpty(deviceAbi)) {
                Debug.LogWarning("Cannot determine device ABI");
                return false;
            }

            var envVariableDictionary =  Environment.GetEnvironmentVariables();
            if (envVariableDictionary == null) {
                Debug.LogWarning("Environment variables are not defined.");
                return false;
            }

            // verify if the library is already extracted
            var backtraceNativeLibraryPath = Path.Combine(nativeDirectory, _nativeLibraryName);
            if (!File.Exists(backtraceNativeLibraryPath)) {
                backtraceNativeLibraryPath = string.Format("{0}!/lib/{1}/{2}", Application.dataPath, deviceAbi, _nativeLibraryName);
            }

            // prepare native crash handler environment variables
            List<String> environmentVariables = new List<string> () {
                string.Format("CLASSPATH={0}", Application.dataPath),
                string.Format("BACKTRACE_UNITY_CRASH_HANDLER={0}", backtraceNativeLibraryPath),
                string.Format("LD_LIBRARY_PATH={0}", string.Join(":", nativeDirectory, Directory.GetParent(nativeDirectory), GetLibrarySystemPath(), "/data/local")),
                "ANDROID_DATA=/data"
            };

            foreach (DictionaryEntry kvp in envVariableDictionary) {
                environmentVariables.Add(string.Format("{0}={1}", kvp.Key, kvp.Value == null ? "NULL" : kvp.Value));
            }
            

            return InitializeJavaCrashHandler(
                AndroidJNI.NewStringUTF(minidumpUrl),
                AndroidJNI.NewStringUTF(databasePath),
                AndroidJNI.NewStringUTF(_crashHandlerPath),
                AndroidJNIHelper.ConvertToJNIArray(Array.Empty<string>()),
                AndroidJNIHelper.ConvertToJNIArray(Array.Empty<string>()),
                AndroidJNIHelper.ConvertToJNIArray(attachments?.ToArray() ?? Array.Empty<string>()),
                AndroidJNIHelper.ConvertToJNIArray(environmentVariables.ToArray())
            );
        }

        private string GetLibrarySystemPath() {
            using (var systemClass = new AndroidJavaClass("java.lang.System"))
            {
                return systemClass.CallStatic<string>("getProperty", "java.library.path");
            }
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
