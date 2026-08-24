using System;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.OSX
{
    /// <summary>
    /// Guards Backtrace's pending macOS crash reports against Unity's built-in crash reporter.
    /// </summary>
    internal static class PendingCrashReportQuarantine
    {
        /// <summary>
        /// PLCrashReporter's fixed pending report file name.
        /// </summary>
        internal const string LiveReportFileName = "live_report.plcrash";

        /// <summary>
        /// Name the pending report is parked under while hidden from Unity.
        /// </summary>
        internal const string QuarantinedReportFileName = LiveReportFileName + ".backtrace-quarantine";

        /// <summary>
        /// PlayerPrefs flag persisted while the Backtrace native crash handler is active, so the next launch knows a pending report in the shared directory belongs to this SDK.
        /// </summary>
        internal const string CaptureActivePlayerPrefsKey = "backtrace-osx-native-capture";

        /// <summary>
        /// PLCrashReporter's shared cache directory name.
        /// </summary>
        private const string PlCrashReporterCacheDirectory = "com.plausiblelabs.crashreporter.data";

        /// <summary>
        /// PLCrashReporter's per-application report directory under the given caches root.
        /// </summary>
        internal static string GetDefaultReportDirectory(string cachesRoot, string bundleIdentifier)
        {
            return Path.Combine(Path.Combine(cachesRoot, PlCrashReporterCacheDirectory), bundleIdentifier);
        }

        /// <summary>
        /// Default PLCrashReporter report directory of the running application, or null when it cannot be determined. Never throws.
        /// </summary>
        internal static string GetApplicationReportDirectory()
        {
            try
            {
                var bundleIdentifier = Application.identifier;
                if (string.IsNullOrEmpty(bundleIdentifier))
                {
                    return null;
                }
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (string.IsNullOrEmpty(home))
                {
                    return null;
                }
                // PLCrashReporter's default basePath is NSCachesDirectory
                var cachesRoot = Path.Combine(Path.Combine(home, "Library"), "Caches");
                return GetDefaultReportDirectory(cachesRoot, bundleIdentifier);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Moves a pending report aside so Unity's crash reporter cannot find it. Never throws.
        /// </summary>
        /// <returns>true when a report was quarantined</returns>
        internal static bool QuarantineLiveReport(string reportDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(reportDirectory))
                {
                    return false;
                }
                var liveReport = Path.Combine(reportDirectory, LiveReportFileName);
                if (!File.Exists(liveReport))
                {
                    return false;
                }
                var quarantinedReport = Path.Combine(reportDirectory, QuarantinedReportFileName);
                if (File.Exists(quarantinedReport))
                {
                    // a previously quarantined report was never handed back, keep the newer crash, matching PLCrashReporter's single-pending-report semantics
                    File.Delete(quarantinedReport);
                }
                File.Move(liveReport, quarantinedReport);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Puts a quarantined report back so PLCrashReporter can submit and purge it. Call immediately before the native crash handler starts. Never throws.
        /// </summary>
        /// <returns>true when a report was restored</returns>
        internal static bool RestoreLiveReport(string reportDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(reportDirectory))
                {
                    return false;
                }
                var quarantinedReport = Path.Combine(reportDirectory, QuarantinedReportFileName);
                if (!File.Exists(quarantinedReport))
                {
                    return false;
                }
                var liveReport = Path.Combine(reportDirectory, LiveReportFileName);
                if (File.Exists(liveReport))
                {
                    // a fresh report already occupies the live slot, keep the quarantined one parked for the next launch instead of overwriting either of them
                    return false;
                }
                File.Move(quarantinedReport, liveReport);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Persists whether Backtrace native crash capture is active, so the next launch only quarantines reports this SDK is responsible for.
        /// Saved immediately because a crashed session never reaches Unity's regular PlayerPrefs flush. Never throws.
        /// </summary>
        internal static void SetCaptureActive(bool active)
        {
            try
            {
                var value = active ? 1 : 0;
                if (PlayerPrefs.GetInt(CaptureActivePlayerPrefsKey, -1) == value)
                {
                    return;
                }
                PlayerPrefs.SetInt(CaptureActivePlayerPrefsKey, value);
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// True when the previous session had Backtrace native crash capture active.
        /// </summary>
        internal static bool IsCaptureActive()
        {
            try
            {
                return PlayerPrefs.GetInt(CaptureActivePlayerPrefsKey, 0) == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
#if UNITY_2019_2_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void QuarantineOnStartup()
        {
            if (!IsCaptureActive())
            {
                return;
            }
            QuarantineLiveReport(GetApplicationReportDirectory());
        }
#endif
    }
}
