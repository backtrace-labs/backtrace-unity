﻿using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Android native client 
    /// </summary>
    internal class NativeClient : INativeClient
    {
        [DllImport("backtrace-native")]
        private static extern bool Initialize(IntPtr submissionUrl, IntPtr databasePath, IntPtr handlerPath, IntPtr keys, IntPtr values);

        [DllImport("backtrace-native")]
        private static extern bool AddAttribute(IntPtr key, IntPtr value);

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
            _captureNativeCrashes = _configuration.CaptureNativeCrashes;
            _handlerANR = _configuration.HandleANR;
            HandleAnr(gameObjectName, "OnAnrDetected");
            HandleNativeCrashes();
#endif

        }
        /// <summary>
        /// Start crashpad process to handle native Android crashes
        /// </summary>

        private void HandleNativeCrashes()
        {
            // make sure database is enabled 
            if (!_captureNativeCrashes || !_configuration.Enabled)
            {
                Debug.LogWarning("Backtrace native integration status: Disabled NDK integration");
                return;
            }
            var databasePath = _configuration.CrashpadDatabasePath;
            if (string.IsNullOrEmpty(databasePath) || !Directory.Exists(databasePath))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return;
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
        public Dictionary<string, string> GetAttributes()
        {
            var result = new Dictionary<string, string>();
            if (!_enabled)
            {
                return result;
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var backtraceAttributes = new AndroidJavaObject(_nativeAttributesPath))
            {
                var androidAttributes = backtraceAttributes.Call<AndroidJavaObject>("GetAttributes", context);
                var entrySet = androidAttributes.Call<AndroidJavaObject>("entrySet");
                var iterator = entrySet.Call<AndroidJavaObject>("iterator");
                while (iterator.Call<bool>("hasNext"))
                {
                    var pair = iterator.Call<AndroidJavaObject>("next");

                    var key = pair.Call<string>("getKey");
                    var value = pair.Call<string>("getValue");
                    result[key] = value;
                }

                return result;

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
    }
}