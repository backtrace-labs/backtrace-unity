using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Collect and send native Crashes from unity crashes directory to Backtrace
    /// </summary>
    internal class NativeCrashUploader
    {
        /// <summary>
        /// Application version storage key
        /// </summary>
        internal const string VersionKey = "backtrace-app-version";

        /// <summary>
        /// Application UUID storage key
        /// </summary>
        internal const string MachineUuidKey = "backtrace-uuid";

        /// <summary>
        /// Application session id storage key
        /// </summary>
        internal const string SessionKey = "backtrace-session-id";

        /// <summary>
        /// Path to the native crash directory
        /// </summary>
        internal readonly string NativeCrashesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Temp",
                    Application.companyName,
                    Application.productName,
                    "crashes");

        /// <summary>
        /// Backtrace API
        /// </summary>
        private readonly IBacktraceApi _backtraceApi;

        /// <summary>
        /// Application version
        /// </summary>
        internal readonly string ApplicationVersion;

        /// <summary>
        /// Machine UUID
        /// </summary>
        internal readonly string MachineUuid;

        /// <summary>
        /// Session ID
        /// </summary>
        internal readonly string SessionId;

        public NativeCrashUploader(AttributeProvider attributeProvider, IBacktraceApi backtraceApi)
        {
            _backtraceApi = backtraceApi;
            // retrieve values from previous session (if defined)
            // otherwise get values from current session
            ApplicationVersion = PlayerPrefs.GetString(VersionKey, attributeProvider.ApplicationVersion);
            MachineUuid = PlayerPrefs.GetString(MachineUuidKey, attributeProvider.ApplicationGuid);
            SessionId = PlayerPrefs.GetString(SessionKey, null);
            // update temporary attributes
            UpdatePrefs(attributeProvider.ApplicationGuid, attributeProvider.ApplicationSessionKey, attributeProvider.ApplicationVersion);
        }

        private void UpdatePrefs(string machineId, string sessionId, string applicationVersion)
        {
            PlayerPrefs.SetString(VersionKey, applicationVersion);
            PlayerPrefs.SetString(MachineUuidKey, machineId);
            PlayerPrefs.SetString(SessionKey, sessionId);
        }

        /// <summary>
        /// Read directory structure in the native crash directory and send new crashes to Backtrace
        /// </summary>
        public IEnumerator SendUnhandledGameCrashesOnGameStartup()
        {
            if (string.IsNullOrEmpty(NativeCrashesDir) || !Directory.Exists(NativeCrashesDir))
            {
                yield break;
            }
            else
            {
                var crashDirs = Directory.GetDirectories(NativeCrashesDir);
                foreach (var crashDir in crashDirs)
                {

                    var crashDirFullPath = Path.Combine(NativeCrashesDir, crashDir);
                    var crashFiles = Directory.GetFiles(crashDirFullPath);

                    var alreadyUploaded = crashFiles.Any(n => n.EndsWith("backtrace.json"));
                    if (alreadyUploaded)
                    {
                        continue;
                    }
                    var minidumpPath = crashFiles.FirstOrDefault(n => n.EndsWith("crash.dmp"));
                    if (string.IsNullOrEmpty(minidumpPath))
                    {
                        continue;
                    }
                    var attachments = crashFiles.Where(n => n != minidumpPath);
                    var attributes = new Dictionary<string, string>()
                    {
                        {"guid", MachineUuid },
                        {"application.version", ApplicationVersion },
                        {"error.type", "Crash" },
                        { BacktraceMetrics.ApplicationSessionKey, string.IsNullOrEmpty(SessionId) ? "null" : SessionId}
                    };
                    yield return _backtraceApi.SendMinidump(minidumpPath, attachments, attributes, (BacktraceResult result) =>
                     {
                         if (result != null && result.Status == BacktraceResultStatus.Ok)
                         {
                             File.Create(Path.Combine(crashDirFullPath, "backtrace.json"));
                         }
                     });

                }
            }
        }
    }
}