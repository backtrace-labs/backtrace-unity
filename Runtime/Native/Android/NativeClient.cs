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

        [DllImport("backtrace-native", EntryPoint = "InitializeJavaCrashHandler", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool InitializeJavaCrashHandlerNative(
            IntPtr submissionUrl,
            IntPtr databasePath,
            IntPtr classPath,
            IntPtr keys,
            IntPtr values,
            IntPtr attachments,
            IntPtr environmentVariables);

        [DllImport("backtrace-native", EntryPoint = "AddAttribute", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AddAttributeNative(IntPtr key, IntPtr value);

        [DllImport("backtrace-native", EntryPoint = "DumpWithoutCrash", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DumpWithoutCrashNative(
            IntPtr message,
            [MarshalAs(UnmanagedType.I1)] bool setMainThreadAsFaultingThread);

        [DllImport("backtrace-native", EntryPoint = "Disable", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DisableNativeIntegrationNative();

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
        /// Guess native directory path based on the data path directory.
        /// The application-info snapshot can miss nativeLibraryDir when the activity is not available.
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
        /// Resolve the path to the base APK that contains the Backtrace Java classes.
        /// </summary>
        private static string GetApkPathForCrashHandler()
        {
            String applicationDataPath = Application.dataPath;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    if (unityPlayer == null)
                    {
                        return applicationDataPath;
                    }

                    using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        if (activity == null)
                        {
                            return applicationDataPath;
                        }

                        using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                        {
                            if (context != null)
                            {
                                using (AndroidJavaObject applicationInfo = context.Call<AndroidJavaObject>("getApplicationInfo"))
                                {
                                    if (applicationInfo != null)
                                    {
                                        String sourceDir = applicationInfo.Get<String>("sourceDir");
                                        if (!String.IsNullOrEmpty(sourceDir))
                                        {
                                            return sourceDir;
                                        }
                                    }
                                }

                                using (AndroidJavaObject packageManager = context.Call<AndroidJavaObject>("getPackageManager"))
                                {
                                    if (packageManager != null)
                                    {
                                        String packageName = context.Call<String>("getPackageName");
                                        if (!String.IsNullOrEmpty(packageName))
                                        {
                                            using (AndroidJavaObject pmApplicationInfo = packageManager.Call<AndroidJavaObject>("getApplicationInfo", packageName, 0))
                                            {
                                                if (pmApplicationInfo != null)
                                                {
                                                    String pmSourceDir = pmApplicationInfo.Get<String>("sourceDir");
                                                    if (!String.IsNullOrEmpty(pmSourceDir))
                                                    {
                                                        return pmSourceDir;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Failure class only: messages can carry package and filesystem details.
                Debug.LogWarning(String.Format(
                    "Backtrace native crash handler: failed to resolve the APK path, falling back to"
                        + " Application.dataPath. Failure type: {0}",
                    FailureType(e)));
            }

            return applicationDataPath;
#else
            // Editor/non‑Android
            return applicationDataPath;
#endif
        }

        /// <summary>
        /// Diagnostics carry the failure class only, never exception messages.
        /// messages can contain submission URLs, tokens, attachment paths, or resolved library paths.
        /// </summary>
        private static string FailureType(Exception exception)
        {
            return exception == null ? "unknown" : exception.GetType().FullName;
        }

        private static void DeleteLocalReference(IntPtr reference)
        {
            if (reference != IntPtr.Zero)
            {
                AndroidJNI.DeleteLocalRef(reference);
            }
        }

        /// <summary>
        /// Sets one native crash attribute with deterministic JNI local-reference cleanup.
        /// native attribute write goes through this helper:
        /// initial population, SetAttribute, ANR error.type changes, and OOM attributes.
        /// </summary>
        private static void SetNativeAttribute(string key, string value)
        {
            IntPtr keyReference = IntPtr.Zero;
            IntPtr valueReference = IntPtr.Zero;
            try
            {
                keyReference = AndroidJNI.NewStringUTF(key);
                valueReference = AndroidJNI.NewStringUTF(value ?? string.Empty);
                AddAttributeNative(keyReference, valueReference);
            }
            finally
            {
                DeleteLocalReference(valueReference);
                DeleteLocalReference(keyReference);
            }
        }

        /// <summary>
        /// Requests a native dump with deterministic JNI local-reference cleanup.
        /// The native export is a safe no-op when the backend is unavailable or disabled.
        /// </summary>
        private static void SendNativeDump(string message, bool setMainThreadAsFaultingThread)
        {
            IntPtr messageReference = IntPtr.Zero;
            try
            {
                messageReference = AndroidJNI.NewStringUTF(message ?? string.Empty);
                DumpWithoutCrashNative(messageReference, setMainThreadAsFaultingThread);
            }
            finally
            {
                DeleteLocalReference(messageReference);
            }
        }

        private static bool InvokeInitialize(
            string minidumpUrl,
            string databasePath,
            string classPath,
            string[] attributeKeys,
            string[] attributeValues,
            string[] attachments,
            string[] environmentVariables)
        {
            IntPtr urlRef = IntPtr.Zero;
            IntPtr databaseRef = IntPtr.Zero;
            IntPtr classRef = IntPtr.Zero;
            IntPtr keysRef = IntPtr.Zero;
            IntPtr valuesRef = IntPtr.Zero;
            IntPtr attachmentsRef = IntPtr.Zero;
            IntPtr environmentRef = IntPtr.Zero;
            try
            {
                urlRef = AndroidJNI.NewStringUTF(minidumpUrl);
                databaseRef = AndroidJNI.NewStringUTF(databasePath);
                classRef = AndroidJNI.NewStringUTF(classPath);
                keysRef = AndroidJNIHelper.ConvertToJNIArray(attributeKeys ?? new string[0]);
                valuesRef = AndroidJNIHelper.ConvertToJNIArray(attributeValues ?? new string[0]);
                attachmentsRef = AndroidJNIHelper.ConvertToJNIArray(attachments ?? new string[0]);
                environmentRef = AndroidJNIHelper.ConvertToJNIArray(environmentVariables ?? new string[0]);
                return InitializeJavaCrashHandlerNative(
                    urlRef,
                    databaseRef,
                    classRef,
                    keysRef,
                    valuesRef,
                    attachmentsRef,
                    environmentRef);
            }
            finally
            {
                DeleteLocalReference(environmentRef);
                DeleteLocalReference(attachmentsRef);
                DeleteLocalReference(valuesRef);
                DeleteLocalReference(keysRef);
                DeleteLocalReference(classRef);
                DeleteLocalReference(databaseRef);
                DeleteLocalReference(urlRef);
            }
        }

        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes(IDictionary<string, string> backtraceAttributes, IEnumerable<string> attachments)
        {
            // Native integration is optional: any failure disables native crash capture only and managed Unity reporting continues.
            // No exception escapes the component constructor.
            try
            {
                CaptureNativeCrashes = TryInitializeNativeCrashes(backtraceAttributes, attachments);
            }
            catch (Exception exception)
            {
                CaptureNativeCrashes = false;
                Debug.LogWarning(
                    "BT_UNITY_ANDROID_NATIVE_PREPARE_FAILURE: Native crash capture is unavailable. "
                        + "Failure type: " + FailureType(exception));
            }
        }

        private bool TryInitializeNativeCrashes(IDictionary<string, string> backtraceAttributes, IEnumerable<string> attachments)
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
                return false;
            }

            var databasePath = _configuration.CrashpadDatabasePath;
            var fullDatabasePath = _configuration.GetFullDatabasePath();

            if (string.IsNullOrEmpty(databasePath) || string.IsNullOrEmpty(fullDatabasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path undefined");
                return false;
            }
            if (!Directory.Exists(fullDatabasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return false;
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
                return false;
            }

            // crashpad is available only for API level 21+
            // make sure we don't want ot start crashpad handler
            // on the unsupported API
            if (apiLevel < 21)
            {
                Debug.LogWarning("Backtrace native integration status: Unsupported Android API level");
                return false;
            }

            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();

            // The ABI of THIS PROCESS (not the device-preferred ABI: a 32-bit process on a 64-bit device differs).
            // It is required only for the x86 policy and the split/base-APK path fallback,
            // an undetermined ABI must not disable capture when the linker path or an extracted library resolves, so the resolver receives null and decides.
            string processAbi = TryGetProcessAbi();
            if (!AndroidProcessAbi.SupportsNativeCrashCapture(processAbi) && processAbi != null)
            {
                Debug.LogWarning(
                    "Backtrace native integration status: native crash capture is unsupported on "
                        + processAbi + "; managed reporting stays enabled");
                return false;
            }

            var applicationInfo = AndroidApplicationInfoSnapshot.Capture();
            if (string.IsNullOrEmpty(applicationInfo.NativeLibraryDir))
            {
                // Legacy discovery for hosts without a Unity activity.
                // An empty native-library directory no longer disables capture:
                // the linker-reported path or split metadata can still resolve the handler.
                applicationInfo.NativeLibraryDir = GuessNativeDirectoryPath();
            }

            // Resolution order: linker-reported path, extracted library, installed ABI split, base APK without opening an APK archive.
            var loadedLibraryPath = AndroidLoadedLibraryPath.TryGet();
            var handlerPath = AndroidNativeLibraryPathResolver.Resolve(
                applicationInfo,
                loadedLibraryPath,
                processAbi,
                Application.dataPath);

            // CLASSPATH stays the base APK containing the Java classes; the handler path may point at an ABI split.
            var apkPath = GetApkPathForCrashHandler();
            var environmentVariables = AndroidCrashHandlerEnvironment.BuildEnvironment(
                Environment.GetEnvironmentVariables(),
                apkPath,
                handlerPath,
                BuildLibrarySearchPaths(applicationInfo.NativeLibraryDir));

            var initialized = InvokeInitialize(
                minidumpUrl,
                databasePath,
                _crashHandlerPath,
                new string[0],
                new string[0],
                attachments == null ? new string[0] : attachments.ToArray(),
                environmentVariables);
            if (!initialized)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Java crash handler");
                return false;
            }

            foreach (var attribute in backtraceAttributes)
            {
                SetNativeAttribute(attribute.Key, attribute.Value);
            }

            // add exception type to crashes handled by crashpad - all exception handled by crashpad
            // by default we setting this option here, to set error.type when unexpected crash happen (so attribute will present)
            // otherwise in other methods - ANR detection, OOM handler, we're overriding it and setting it back to "crash"

            // warning
            // don't add attributes that can change over the time to initialization method attributes. Crashpad will prevent from overriding them on game runtime.
            // ANRs/OOMs methods can override error.type attribute, so we shouldn't pass error.type attribute via attributes parameters.
            SetNativeAttribute(ErrorTypeAttribute, CrashType);
            return true;
        }

        private static string TryGetProcessAbi()
        {
            try
            {
                return AndroidProcessAbi.Capture();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private List<string> BuildLibrarySearchPaths(string nativeLibraryDir)
        {
            var searchPaths = new List<string>();
            if (!string.IsNullOrEmpty(nativeLibraryDir))
            {
                searchPaths.Add(nativeLibraryDir);
                var parent = Directory.GetParent(nativeLibraryDir);
                if (parent != null)
                {
                    searchPaths.Add(parent.ToString());
                }
            }
            searchPaths.Add(GetLibrarySystemPath());
            searchPaths.Add("/data/local");
            return searchPaths;
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
            AnrThread = new Thread(() =>
            {
                // Attach the Unity-created watchdog thread to the JVM exactly once and detach it when the loop ends;
                // attaching per report leaked the attachment for the thread's lifetime.
                // The Unity main thread is never detached here, this is the watchdog thread only.
                bool attached = AndroidJNI.AttachCurrentThread() == 0;
                try
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
                                    if (!attached)
                                    {
                                        // A transient early attach failure (JVM resource pressure) must not permanently disable hang dumps.
                                        attached = AndroidJNI.AttachCurrentThread() == 0;
                                    }
                                    if (attached)
                                    {
                                        // set temporary attribute to "Hang"
                                        SetNativeAttribute(ErrorTypeAttribute, HangType);
                                        SendNativeDump(AnrMessage, true);
                                        // update error.type attribute in case when crash happen
                                        SetNativeAttribute(ErrorTypeAttribute, CrashType);
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
                            // make sure when ANR happened just after going to foreground we won't false positive ANR report
                            lastUpdatedCache = 0;
                        }
                        Thread.Sleep(AnrWatchdogTimeout);
                    }
                }
                finally
                {
                    if (attached)
                    {
                        AndroidJNI.DetachCurrentThread();
                    }
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
            SetNativeAttribute(key, value ?? string.Empty);
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
            // Disabling is contained like setup: a native failure must not leave the component half-disabled or crash the caller, and it logs the failure class only.
            try
            {
                if (CaptureNativeCrashes)
                {
                    DisableNativeIntegrationNative();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "BT_UNITY_ANDROID_NATIVE_DISABLE_FAILURE: Failure type: " + FailureType(exception));
            }
            finally
            {
                CaptureNativeCrashes = false;
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
