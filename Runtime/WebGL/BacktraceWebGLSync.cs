using System.Runtime.InteropServices;
using UnityEngine;

namespace Backtrace.Unity.WebGL
{
    /// <summary>
    /// WebGL helpers for flushing Emscripten IDBFS writes to IndexedDB.
    ///
    /// When the Backtrace database is enabled on WebGL, it stores records inside Unity's persistent data directory, which is backed by Emscripten's virtual filesystem.
    ///
    /// Unity's virtual filesystem does not always flush buffered writes to IndexedDB before the tab is closed or backgrounded.
    /// To reduce report loss, the SDK installs browser lifecycle hooks (via a WebGL .jslib) and exposes explicit flush calls.
    /// </summary>
    internal static class BacktraceWebGLSync
    {
        // Debounce to avoid spamming FS.syncfs on frequent events / frequent report writes.
        private const float MinSyncIntervalSeconds = 2f;

        private static bool _hooksAttempted;
        private static bool _hooksInstalled;
        private static float _lastSyncTime;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int BT_InstallPageLifecycleHooks();

        [DllImport("__Internal")]
        private static extern void BT_SyncFS();
#endif

        /// <summary>
        /// Install JS page lifecycle hooks that flush FS to IndexedDB.
        /// Safe to call multiple times.
        /// </summary>
        public static void TryInstallPageLifecycleHooks()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_hooksInstalled || _hooksAttempted)
            {
                return;
            }

            _hooksAttempted = true;
            try
            {
                _hooksInstalled = BT_InstallPageLifecycleHooks() != 0;
            }
            catch
            {
                _hooksInstalled = false;
            }
#endif
        }

        /// <summary>
        /// Flush FS to IndexedDB.
        /// </summary>
        /// <param name="force">If true, bypasses debounce interval.</param>
        public static void TrySyncFileSystem(bool force = false)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!force)
            {
                var now = Time.realtimeSinceStartup;
                if (now - _lastSyncTime < MinSyncIntervalSeconds)
                {
                    return;
                }
                _lastSyncTime = now;
            }

            try
            {
                BT_SyncFS();
            }
            catch
            {
                // Intentionally ignored. Missing plugin or unsupported runtime.
            }
#endif
        }
    }
}
