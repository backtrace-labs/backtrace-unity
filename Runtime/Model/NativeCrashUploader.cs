using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Types;
using System;
using System.Collections;
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
        private IBacktraceApi _backtraceApi;

        internal string nativeCrashesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Temp",
                    Application.companyName,
                    Application.productName,
                    "crashes");

        public void SetBacktraceApi(IBacktraceApi backtraceApi)
        {
            _backtraceApi = backtraceApi;
        }

        public IEnumerator SendUnhandledGameCrashesOnGameStartup()
        {
            if (string.IsNullOrEmpty(nativeCrashesDir) || !Directory.Exists(nativeCrashesDir))
            {
                yield break;
            }
            else
            {
                var crashDirs = Directory.GetDirectories(nativeCrashesDir);
                foreach (var crashDir in crashDirs)
                {

                    var crashDirFullPath = Path.Combine(nativeCrashesDir, crashDir);
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
                    yield return _backtraceApi.SendMinidump(minidumpPath, attachments, (BacktraceResult result) =>
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