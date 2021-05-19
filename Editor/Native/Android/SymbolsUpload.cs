#if UNITY_2019_2_OR_NEWER && UNITY_ANDROID
using Backtrace.Unity.Model;
using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;

namespace Backtrace.Unity.Editor.Build
{
    public class SymbolsUpload : IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }


        public void OnPostprocessBuild(BuildReport report)
        {
            var disabledSymbols =
#if UNITY_2021_1_OR_NEWER
                EditorUserBuildSettings.androidCreateSymbols == AndroidCreateSymbols.Disabled;
#else
                EditorUserBuildSettings.androidCreateSymbolsZip == false;
#endif
            // symbols upload is availble only on the il2cpp Android builds 
            if (report.summary.platform != BuildTarget.Android || disabledSymbols)
            {
                return;
            }
            // validate if symbols.zip archive exists 
            var symbolsArchive = GetPathToSymbolsArchive(report);
            var backtraceAssets = AssetDatabase.FindAssets("Backtrace t: ScriptableObject", null);
            foreach (string backtraceAsset in backtraceAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(backtraceAsset);

                // make sure that we won't try to load test or debug Backtrace configurations
                var normalizedPath = path.ToLower();
                if (normalizedPath.Contains("test") || normalizedPath.Contains("debug"))
                {
                    continue;
                }

                var configuration = GetBacktraceConfiguration(path);
                if (configuration == null || string.IsNullOrEmpty(configuration.SymbolsUploadToken))
                {
                    continue;
                }

                Debug.Log("Backtrace symbols upload. Detected Backtrace configuration with enabled symbols upload option.");
                Debug.Log(string.Format("Configuration path {0}", path));
                if (!EditorUtility.DisplayDialog("Backtrace symbols upload",
                    "Would you like to upload generated symbols files for better debugging experience?",
                    "Yes", "Skip"))
                {
                    Debug.Log("Canceled symbols upload.");
                    return;
                }


                Debug.Log("Trying to upload symbols to Backtrace.");
                if (!File.Exists(symbolsArchive))
                {
                    Debug.LogWarning("Cannot upload symbols to Backtrace: Symbols archive doesn't exist.");
                    Debug.LogWarning(string.Format("Symbols archive path {0}", symbolsArchive));
                    return;
                }
                var backtraceSymbols = ConvertSymbols(symbolsArchive);
                if (string.IsNullOrEmpty(backtraceSymbols))
                {
                    return;
                }
                UploadSymbols(configuration.ServerUrl, configuration.SymbolsUploadToken, backtraceSymbols);
                File.Delete(backtraceSymbols);
                return;
            }
        }

        private string ConvertSymbols(string symbolsArchive)
        {
            var symbolsTmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(symbolsTmpDir);

            try
            {
                var unpackProcess = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        FileName = "tar",
                        Arguments = string.Format("-C {0} -xf {1}", symbolsTmpDir, symbolsArchive)
                    }
                };
                unpackProcess.Start();
                unpackProcess.WaitForExit();
                var files = Directory.GetFiles(symbolsTmpDir, "*.sym.so", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var newName = file.Replace(".sym.so", ".so");
                    File.Move(file, newName);
                }
                var backtraceSymbols = Path.Combine(Path.GetTempPath(), string.Format("backtrace-{0}-symbols.zip", Guid.NewGuid().ToString()));

                var zipProcess = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        FileName = "tar",
                        Arguments = string.Format("-czvf {0} {1}", backtraceSymbols, symbolsTmpDir)
                    }
                };
                zipProcess.Start();
                zipProcess.WaitForExit();
                return backtraceSymbols;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Cannot generate Backtrace symbols archive. Reason: " + e.Message);
                return string.Empty;
            }
            finally
            {
                if (Directory.Exists(symbolsTmpDir))
                {
                    Directory.Delete(symbolsTmpDir, true);
                }
            }
        }

        private string GetPathToSymbolsArchive(BuildReport report)
        {
            var archiveName = string.Format("{0}-{1}-v{2}.symbols.zip", Path.GetFileNameWithoutExtension(report.summary.outputPath), PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode.ToString());
            return Path.Combine(Directory.GetParent(report.summary.outputPath).FullName, archiveName);

        }
        private BacktraceConfiguration GetBacktraceConfiguration(string assetPath)
        {
            try
            {
                var configuration = AssetDatabase.LoadAssetAtPath<BacktraceConfiguration>(assetPath);
                if (string.IsNullOrEmpty(configuration.ServerUrl))
                {
                    return null;
                }
                return configuration;
            }
            catch (Exception)
            {
                // Invalid Backtrace asset
                // Ignoring asset.
                return null;
            }
        }

        private void UploadSymbols(string serverUrl, string symbolsToken, string symbolsPath)
        {
            Debug.Log($"Uploading symbols archive : {symbolsPath} to server {serverUrl}. Please wait...");
            var backtraceCredentials = new BacktraceCredentials(serverUrl);


            var boundaryId = string.Format("----------{0:N}", Guid.NewGuid());
            var boundaryIdBytes = Encoding.ASCII.GetBytes(boundaryId);
            try
            {
                var bytes = File.ReadAllBytes(symbolsPath);
                var formData = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection("upload_file", bytes)
                };
                using (var request = UnityWebRequest.Post(
                    uri: backtraceCredentials.GetSymbolsSubmissionUrl(symbolsToken).ToString(),
                    multipartFormSections: formData,
                    boundary: boundaryIdBytes))
                {
                    request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundaryId);
                    request.timeout = 15000;
                    request.SendWebRequest();
                    while (!request.isDone)
                    {
                        EditorUtility.DisplayProgressBar("Backtrace symbols upload", "Symbols upload progress:", request.uploadProgress);
                    }
                    if (request.ReceivedNetworkError())
                    {
                        Debug.LogWarning(string.Format("Cannot upload symbols to Backtrace. Reason: {0}", request.downloadHandler.text));
                        return;
                    }
                    Debug.Log("Symbols are available in your Backtrace instance");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("Cannot upload symbols to Backtrace. Reason: {0}", e.Message));
            }
        }
    }
}
#endif
