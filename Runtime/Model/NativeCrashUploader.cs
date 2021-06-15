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
        internal const string VersionKey = "backtrace-app-version";
        internal const string MachineUuidKey = "backtrace-uuid";
        internal const string SessionKey = "backtrace-session-id";

        internal readonly string NativeCrashesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Temp",
                    Application.companyName,
                    Application.productName,
                    "crashes");

        private readonly IBacktraceApi _backtraceApi;
        private readonly string _applicationVersion;
        private readonly string _machineUuid;
        private readonly string _sessionId;

        public NativeCrashUploader(AttributeProvider attributeProvider, IBacktraceApi backtraceApi)
        {
            _applicationVersion = PlayerPrefs.GetString(VersionKey, attributeProvider.ApplicationVersion);
            _machineUuid = PlayerPrefs.GetString(MachineUuidKey, attributeProvider.ApplicationGuid);
            _sessionId = PlayerPrefs.GetString(SessionKey, attributeProvider.ApplicationSessionKey);
            UpdatePrefs(attributeProvider.ApplicationGuid, attributeProvider.ApplicationSessionKey, attributeProvider.ApplicationVersion);
            _backtraceApi = backtraceApi;
        }

        private void UpdatePrefs(string machineId, string sessionId, string applicationVersion)
        {
            PlayerPrefs.SetString(VersionKey, applicationVersion);
            PlayerPrefs.SetString(MachineUuidKey, machineId);
            PlayerPrefs.SetString(SessionKey, sessionId);
        }

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
                        {"guid", _machineUuid },
                        {"application.version", _applicationVersion },
                        {"error.type", "Crash" },
                        { BacktraceMetrics.ApplicationSessionKey, string.IsNullOrEmpty(_sessionId) ? "null" : _sessionId}
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