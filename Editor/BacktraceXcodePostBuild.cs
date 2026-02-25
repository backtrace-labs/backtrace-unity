#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Backtrace.Unity.Editor.iOS
{
    /// <summary>
    /// Links Backtrace.xcframework and CrashReporter.xcframework
    /// embeds, signs Backtrace and sets Swift/iOS settings.
    /// </summary>
    public static class BacktraceXcodePostBuild
    {
        // Runs late after post-processors
        private const int PostBuildOrder = 500;

        [PostProcessBuild(PostBuildOrder)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS) {
                 return;
                 }

            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject(); 
            project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
            string appTargetGuid = project.GetUnityMainTargetGuid();
            string unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
#else
            string appTargetGuid = project.TargetGuidByName("Unity-iPhone");
            string unityFrameworkTargetGuid = appTargetGuid;
#endif
            if (string.IsNullOrEmpty(appTargetGuid) || string.IsNullOrEmpty(unityFrameworkTargetGuid))
            {
                Debug.LogError("[Backtrace] iOS post-build: could not resolve Xcode targets.");
                return;
            }

            // Locate exported xcframeworks
            string FindXCFramework(string folderName)
            {
                var matches = Directory.GetDirectories(buildPath, folderName, SearchOption.AllDirectories);
                return matches.FirstOrDefault();
            }

            var backtraceXCPath = FindXCFramework("Backtrace.xcframework");
            var crashReporterXCPath = FindXCFramework("CrashReporter.xcframework");

            if (string.IsNullOrEmpty(backtraceXCPath))
            {
                Debug.LogError($"[Backtrace] Could not locate Backtrace.xcframework under: {buildPath}");
                return;
            }
            if (string.IsNullOrEmpty(crashReporterXCPath))
            {
                Debug.LogError($"[Backtrace] Could not locate CrashReporter.xcframework under: {buildPath}");
                return;
            }

            // Project-relative paths
            string relBacktraceXC = ToProjectRelative(buildPath, backtraceXCPath);
            string relCrashReporterXC = ToProjectRelative(buildPath, crashReporterXCPath);

            // Add file references
            string backtraceFileGuid = project.FindFileGuidByProjectPath(relBacktraceXC);
            if (string.IsNullOrEmpty(backtraceFileGuid)) {
                backtraceFileGuid = project.AddFile(relBacktraceXC, relBacktraceXC, PBXSourceTree.Source);
                }

            string crashReporterFileGuid = project.FindFileGuidByProjectPath(relCrashReporterXC);
            if (string.IsNullOrEmpty(crashReporterFileGuid)) {
                crashReporterFileGuid = project.AddFile(relCrashReporterXC, relCrashReporterXC, PBXSourceTree.Source);
                }

            // Linking
            project.AddFileToBuild(unityFrameworkTargetGuid, backtraceFileGuid);
            project.AddFileToBuild(unityFrameworkTargetGuid, crashReporterFileGuid);

            // Embedding
            PBXProjectExtensions.AddFileToEmbedFrameworks(project, appTargetGuid, backtraceFileGuid);
            AddBuildPropertyUnique(project, appTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited)");
            AddBuildPropertyUnique(project, appTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");

            // Swift std
            project.SetBuildProperty(appTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            // Obj-C Linker Flag
            AddBuildPropertyUnique(project, unityFrameworkTargetGuid, "OTHER_LDFLAGS", "-ObjC");

            project.WriteToFile(projectPath);
            Debug.Log("[Backtrace] iOS post-build: frameworks linked/embedded. Swift stdlib enabled.");
        }

        private static string ToProjectRelative(string buildPath, string absolutePath)
        {
            var rel = absolutePath.Replace('\\', '/');
            var root = (buildPath + "/").Replace('\\', '/');
            return rel.StartsWith(root) ? rel.Substring(root.Length) : rel;
        }

        private static void AddBuildPropertyUnique(PBXProject proj, string targetGuid, string key, string value)
        {
            var current = proj.GetBuildPropertyForAnyConfig(targetGuid, key);
            if (current == null || !current.Split(' ').Contains(value))
            {
                proj.AddBuildProperty(targetGuid, key, value);
            }
        }
    }
}
#endif
