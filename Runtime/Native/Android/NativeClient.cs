using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Android native client 
    /// </summary>
    internal class NativeClient : INativeClient
    {
        // Android native interface paths
        private const string _namespace = "backtrace.io.backtrace_unity_android_plugin";
        private readonly string _nativeAttributesPath = string.Format("{0}.{1}", _namespace, "BacktraceAttributes");
        private readonly string _anrPath = string.Format("{0}.{1}", _namespace, "BacktraceANRWatchdog");

        /// <summary>
        /// Determine if android integration should be enabled
        /// </summary>
        private readonly bool _enabled = Application.platform == RuntimePlatform.Android;

        public NativeClient(string gameObjectName, bool detectAnrs)
        {
            if (detectAnrs && _enabled)
            {
                HandleAnr(gameObjectName, "OnAnrDetected");
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
            new AndroidJavaClass(_anrPath).CallStatic("watch", gameObjectName, callbackName);
        }
    }
}