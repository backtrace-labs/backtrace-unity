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
    /// Links Backtrace.xcframework + CrashReporter.xcframework on UnityFramework,
    /// embeds and signs Backtrace (dynamic) in the app target, and sets Swift/iOS settings.
    /// </summary>
    public static class BacktraceXcodePostBuild
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;

            var projPath = PBXProject.GetPBXProjectPath(buildPath);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

#if UNITY_2019_3_OR_NEWER
            string appTargetGuid = proj.GetUnityMainTargetGuid();
            string ufTargetGuid  = proj.GetUnityFrameworkTargetGuid();
#else
            string appTargetGuid = proj.TargetGuidByName("Unity-iPhone");
            string ufTargetGuid  = appTargetGuid;
#endif
            // Find the xcframeworks that Unity copied into the export (regardless of folder)
            string FindXCFramework(string name)
            {
                var dirs = Directory.GetDirectories(buildPath, name, SearchOption.AllDirectories);
                return dirs.FirstOrDefault();
            }

            var backtraceXC = FindXCFramework("Backtrace.xcframework");
            var crashXC     = FindXCFramework("CrashReporter.xcframework");

            if (string.IsNullOrEmpty(backtraceXC))
            {
                Debug.LogError("[Backtrace] Could not locate Backtrace.xcframework in the exported Xcode project.");
                return;
            }
            if (string.IsNullOrEmpty(crashXC))
            {
                Debug.LogError("[Backtrace] Could not locate CrashReporter.xcframework in the exported Xcode project.");
                return;
            }

            // Add files to project with paths relative to project
            string relBacktrace = backtraceXC.Replace(buildPath + "/", "");
            string relCrash     = crashXC.Replace(buildPath + "/", "");

            string btGuid  = proj.AddFile(relBacktrace, relBacktrace, PBXSourceTree.Source);
            string crGuid  = proj.AddFile(relCrash,     relCrash,     PBXSourceTree.Source);

            // Link both on UnityFramework
            proj.AddFileToBuild(ufTargetGuid, btGuid);
            proj.AddFileToBuild(ufTargetGuid, crGuid);

            // Embed Backtrace (dynamic) on the app target
            PBXProjectExtensions.AddFileToEmbedFrameworks(proj, appTargetGuid, btGuid);
            proj.AddBuildProperty(appTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");

            // Swift / platform settings (SDK requires iOS 13+)
            proj.SetBuildProperty(appTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            proj.SetBuildProperty(appTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET", "13.0");
            proj.SetBuildProperty(ufTargetGuid,  "IPHONEOS_DEPLOYMENT_TARGET", "13.0");

            // Recommended & safe for Obj-C categories
            proj.AddBuildProperty(ufTargetGuid, "OTHER_LDFLAGS", "-ObjC");

            proj.WriteToFile(projPath);
            Debug.Log("[Backtrace] iOS post-build: frameworks linked/embedded; Swift stdlib enabled; iOS 13.0 set.");
        }
    }
}
#endif
