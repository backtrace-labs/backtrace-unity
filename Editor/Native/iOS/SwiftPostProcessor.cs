using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Backtrace.Unity.Editor.Build
{
    public class SwiftPostProcessor : IPostprocessBuildWithReport
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
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }
            // We need to construct our own PBX project path that corrently refers to the Bridging header
            var projPath = Path.Combine(report.summary.outputPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
            /*
            if (File.Exists(projPath))
            {
                UnityEngine.Debug.LogWarning("Cannot add Backtrace integration to xCode project. Cannot find xCode proj");
                return;
            }

            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            var targetGuid = proj.GetUnityFrameworkTargetGuid();
            var pathToBacktraceAssets = AssetDatabase.FindAssets("Backtrace_Unity-Swift");
            if (pathToBacktraceAssets == null || pathToBacktraceAssets.Length == 0)
            {
                UnityEngine.Debug.LogWarning("Cannot find Backtrace assets");
                return;
            }
            var backtraceAsset = pathToBacktraceAssets[0];
            var assetPath = AssetDatabase.GUIDToAssetPath(backtraceAsset);
            var xCodeBacktraceAssets = Directory.GetParent(assetPath).Parent.FullName;

            //// Configure build settings
            proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");

            var bridgingHeaderPath = Path.Combine(xCodeBacktraceAssets, "Backtrace_Unity-Bridging-Header.h");
            proj.SetBuildProperty(targetGuid, "SWIFT_OBJC_BRIDGING_HEADER", bridgingHeaderPath);


            proj.SetBuildProperty(targetGuid, "SWIFT_OBJC_INTERFACE_HEADER_NAME", backtraceAsset);
            proj.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");

            proj.WriteToFile(projPath);
            */
        }
    }
}