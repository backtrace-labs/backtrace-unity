using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using System;
using System.Collections.Generic;
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
        internal float _lastUpdateTime;

        private Thread _anrThread;

        [DllImport("backtrace-native")]
        private static extern bool Initialize(IntPtr submissionUrl, IntPtr databasePath, IntPtr handlerPath, IntPtr keys, IntPtr values);

        [DllImport("backtrace-native")]
        private static extern bool AddAttribute(IntPtr key, IntPtr value);

        [DllImport("backtrace-native", EntryPoint = "DumpWithoutCrash")]
        private static extern bool NativeReport(IntPtr message);



        private readonly BacktraceConfiguration _configuration;
        // Android native interface paths
        private const string _namespace = "backtrace.io.backtrace_unity_android_plugin";
        private readonly string _nativeAttributesPath = string.Format("{0}.{1}", _namespace, "BacktraceAttributes");
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
            if (!_enabled)
            {
                return;
            }
#if UNITY_ANDROID
            _handlerANR = _configuration.HandleANR;
            HandleNativeCrashes();
            HandleAnr(gameObjectName, "OnAnrDetected");
#endif

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
                if (apiLevel < 21)
                {
                    Debug.LogWarning("Backtrace native integration status: Unsupported Android API level");
                    return;
                }
            }
            var libDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "lib");
            if (!Directory.Exists(libDirectory))
            {
                return;
            }
            var crashpadHandlerPath = Directory.GetFiles(libDirectory, "libcrashpad_handler.so", SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(crashpadHandlerPath))
            {
                Debug.LogWarning("Backtrace native integration status: Cannot find crashpad library");
                return;
            }
            // get default built-in Backtrace-Unity attributes
            var backtraceAttributes = new BacktraceAttributes(null, null, true);
            // add exception type to crashes handled by crashpad - all exception handled by crashpad 
            // will be game crashes
            backtraceAttributes.Attributes["error.type"] = "Crash";
            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();

            // reassign to captureNativeCrashes
            // to avoid doing anything on crashpad binary, when crashpad
            // isn't available
            _captureNativeCrashes = Initialize(
                AndroidJNI.NewStringUTF(minidumpUrl),
                AndroidJNI.NewStringUTF(databasePath),
                AndroidJNI.NewStringUTF(crashpadHandlerPath),
                AndroidJNIHelper.ConvertToJNIArray(backtraceAttributes.Attributes.Keys.ToArray()),
                AndroidJNIHelper.ConvertToJNIArray(backtraceAttributes.Attributes.Values.ToArray()));
            if (!_captureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Crashpad client");
            }
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

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var backtraceAttributes = new AndroidJavaObject(_nativeAttributesPath))
            {
                var androidAttributes = backtraceAttributes.Call<AndroidJavaObject>("GetAttributes", new object[] { context });
                var entrySet = androidAttributes.Call<AndroidJavaObject>("entrySet");
                var iterator = entrySet.Call<AndroidJavaObject>("iterator");
                while (iterator.Call<bool>("hasNext"))
                {
                    var pair = iterator.Call<AndroidJavaObject>("next");

                    var key = pair.Call<string>("getKey");
                    var value = pair.Call<string>("getValue");
                    result[key] = value;
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
                while (true)
                {
                    if (lastUpdatedCache == 0)
                    {
                        lastUpdatedCache = _lastUpdateTime;
                    }
                    else if (lastUpdatedCache == _lastUpdateTime)
                    {
                        if (!reported)
                        {
                            if (AndroidJNI.AttachCurrentThread() == 0)
                            {
                                NativeReport(AndroidJNI.NewStringUTF("ANRException: Blocked thread detected."));
                            }
                            reported = true;
                        }
                    }
                    else
                    {
                        reported = false;
                    }

                    lastUpdatedCache = _lastUpdateTime;
                    Thread.Sleep(5000);

                }
            });

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
            if (!_enabled || _captureNativeCrashes)
            {
                return false;
            }

            NativeReport(AndroidJNI.NewStringUTF("OOMException: Out of memory detected."));
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
                _anrThread.Abort();
            }
        }
    }
}