#if UNITY_EDITOR && UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace Backtrace.Unity.Editor.iOS
{
    /// <summary>
    /// Required iOS project wiring only. No downloads, re-signing, source rebuild,
    /// project-wide ARC/Swift changes, scene migration, or macOS build callbacks.
    /// </summary>
    public static class BacktraceXcodePostBuild
    {
        [PostProcessBuild(500)]
        public static void OnPostProcessBuild(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;
            RequireIOS15(PlayerSettings.iOS.targetOSVersionString);
            string root = Path.GetFullPath(buildPath);
            string backtrace = FindOne(root, "Backtrace.xcframework");
            string crashReporter = FindOne(root, "CrashReporter.xcframework");
            string deviceHeaders;
            string simulatorHeaders;
            GetIOSHeaderPaths(crashReporter, out deviceHeaders, out simulatorHeaders);
            var project = new PBXProject();
            string projectPath = PBXProject.GetPBXProjectPath(root);
            project.ReadFromFile(projectPath);
#if UNITY_2019_3_OR_NEWER
            string app = project.GetUnityMainTargetGuid();
            string code = project.GetUnityFrameworkTargetGuid();
#else
            string app = project.TargetGuidByName("Unity-iPhone");
            string code = app;
#endif
            if (string.IsNullOrEmpty(app) || string.IsNullOrEmpty(code))
                throw new InvalidOperationException("[Backtrace] Cannot resolve iOS Xcode targets.");

            string backtraceFile = FindOrAdd(project, Relative(root, backtrace));
            string crashFile = FindOrAdd(project, Relative(root, crashReporter));
            // Remove any importer-created linkage/embedding before installing our deterministic arrangement.
            // Removal concerns only these two file refs.
            RemoveBuildEntries(project, new[] { app, code }, new[] { backtraceFile, crashFile });
            project.AddFileToBuild(code, backtraceFile);
            var appRunPaths = new Dictionary<string, string[]>();
            foreach (string guid in AppBuildConfigurationGuids(project, app))
            {
                appRunPaths[guid] = BuildPropertyValues(project.GetBuildPropertyForConfig(guid, "LD_RUNPATH_SEARCH_PATHS"));
            }
            PBXProjectExtensions.AddFileToEmbedFrameworks(project, app, backtraceFile);
            // Unity's embedding helper replaces this property internally.
            // Restore each host configuration before adding the required runtime search path.
            foreach (var runPath in appRunPaths)
            {
                project.UpdateBuildPropertyForConfig(runPath.Key, "LD_RUNPATH_SEARCH_PATHS", runPath.Value,
                    new[] { "@executable_path/Frameworks" });
            }

            // CrashReporter is static and is already incorporated into the dynamic Backtrace.framework.
            // Compile the bridge against its headers but do not link/embed the static archive a second time.
            // The .mm file uses -fno-autolink and resolves PLCrashReporterConfig via NSClassFromString.
            AddProperty(project, code, "FRAMEWORK_SEARCH_PATHS[sdk=iphoneos*]",
                "\"$(SRCROOT)/" + Relative(root, deviceHeaders) + "\"");
            AddProperty(project, code, "FRAMEWORK_SEARCH_PATHS[sdk=iphonesimulator*]",
                "\"$(SRCROOT)/" + Relative(root, simulatorHeaders) + "\"");
            AddProperty(project, app, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");
            AddProperty(project, code, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");
            project.SetBuildProperty(app, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.AddFrameworkToProject(code, "Foundation.framework", false);
            project.AddFrameworkToProject(code, "CoreData.framework", false);

            // Static code's privacy declarations are resources, not executable content.
            // Keep them in a separate bundle rather than overwrite or merge the host application's PrivacyInfo.xcprivacy.
            string privacy = Path.Combine(crashReporter, "PrivacyInfo.xcprivacy");
            if (!File.Exists(privacy))
                throw new InvalidOperationException("[Backtrace] PLCrashReporter privacy manifest is missing.");
            string resourceRelative = "BacktracePLCrashReporterPrivacy.bundle";
            string resourcePath = Path.Combine(root, resourceRelative);
            Directory.CreateDirectory(resourcePath);
            File.Copy(privacy, Path.Combine(resourcePath, "PrivacyInfo.xcprivacy"), true);
            // Unity need not export ordinary attribution text beside a native plugin.
            // Resolve it from the installed Assets/UPM package, not from an assumed export.
            string notices = SourceNoticesDirectory();
            foreach (string name in new[] { "PLCrashReporter-LICENSE.txt", "PLCrashReporter-ThirdPartyNotices.txt" })
            {
                string source = Path.Combine(notices, name);
                if (!File.Exists(source)) throw new InvalidOperationException("[Backtrace] Missing PLCrashReporter attribution.");
                File.Copy(source, Path.Combine(resourcePath, name), true);
            }
            var info = new PlistDocument();
            info.root.SetString("CFBundleIdentifier", "io.backtrace.unity.plcrashreporter.privacy");
            info.root.SetString("CFBundleName", "BacktracePLCrashReporterPrivacy");
            info.root.SetString("CFBundlePackageType", "BNDL");
            info.root.SetString("CFBundleVersion", "1");
            info.WriteToFile(Path.Combine(resourcePath, "Info.plist"));
            string resourceFile = FindOrAdd(project, resourceRelative);
            project.RemoveFileFromBuild(app, resourceFile);
            project.AddFileToBuild(app, resourceFile);
            project.WriteToFile(projectPath);
        }

        private static string SourceNoticesDirectory()
        {
            string asset = AssetDatabase.GUIDToAssetPath("3dc91150f76c2497c810a0e3f78ff34d");
            if (string.IsNullOrEmpty(asset))
                throw new InvalidOperationException("[Backtrace] Cannot resolve the iOS bridge source asset.");
            string physical = asset;
#if UNITY_2019_1_OR_NEWER
            if (asset.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asset);
                if (package == null)
                    throw new InvalidOperationException("[Backtrace] Cannot resolve the installed package.");
                string prefix = "Packages/" + package.name + "/";
                if (!asset.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidOperationException("[Backtrace] Unexpected package asset path.");
                physical = Path.Combine(package.resolvedPath, asset.Substring(prefix.Length));
            }
#endif
            return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(physical)), "ThirdPartyNotices");
        }

        private static void RequireIOS15(string value)
        {
            Version version;
            if (!Version.TryParse(value, out version) || version.CompareTo(new Version(15, 0)) < 0)
                throw new InvalidOperationException(
                    "[Backtrace] Cocoa requires iOS 15.0 or newer. " +
                    "Set Player Settings > iOS > Target minimum iOS Version. " +
                    "This integration does not silently raise your application's minimum OS.");
        }

        private static void GetIOSHeaderPaths(string xcframework, out string device, out string simulator)
        {
            var plist = new PlistDocument();
            plist.ReadFromFile(Path.Combine(xcframework, "Info.plist"));
            device = null;
            simulator = null;
            foreach (PlistElement item in plist.root["AvailableLibraries"].AsArray().values)
            {
                PlistElementDict library = item.AsDict();
                if (library["SupportedPlatform"].AsString() != "ios")
                    throw new InvalidOperationException("[Backtrace] Use the dedicated Unity iOS XCFramework archive.");
                string variant = library.values.ContainsKey("SupportedPlatformVariant")
                    ? library["SupportedPlatformVariant"].AsString() : string.Empty;
                string identifier = library["LibraryIdentifier"].AsString();
                string name = library["LibraryPath"].AsString();
                if (Path.GetFileName(identifier) != identifier || name != "CrashReporter.framework")
                    throw new InvalidOperationException("[Backtrace] Unexpected CrashReporter XCFramework layout.");
                string path = Path.Combine(xcframework, identifier);
                if (!Directory.Exists(Path.Combine(path, name)))
                    throw new InvalidOperationException("[Backtrace] Missing CrashReporter slice.");
                if (variant == string.Empty && device == null) device = path;
                else if (variant == "simulator" && simulator == null) simulator = path;
                else throw new InvalidOperationException("[Backtrace] Ambiguous CrashReporter slices.");
            }
            if (device == null || simulator == null)
                throw new InvalidOperationException("[Backtrace] Both iOS device and simulator slices are required.");
        }

        private static string FindOne(string root, string name)
        {
            string[] paths = Directory.GetDirectories(root, name, SearchOption.AllDirectories);
            if (paths.Length != 1)
                throw new InvalidOperationException("[Backtrace] Expected exactly one exported " + name + ".");
            return paths[0];
        }

        private static string Relative(string root, string path)
        {
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("[Backtrace] Framework is outside the exported project.");
            return fullPath.Substring(prefix.Length).Replace('\\', '/');
        }

        private static string FindOrAdd(PBXProject project, string path)
        {
            string guid = project.FindFileGuidByProjectPath(path);
            if (string.IsNullOrEmpty(guid)) guid = project.FindFileGuidByRealPath(path);
            return string.IsNullOrEmpty(guid) ? project.AddFile(path, path, PBXSourceTree.Source) : guid;
        }

        private static void RemoveBuildEntries(PBXProject project, string[] targets, string[] files)
        {
            // Unity indexes one build entry per target/file even when a framework is both linked and embedded.
            // Re-read after each removal pass to find remaining entries; only the two SDK file references are affected.
            for (int pass = 0; pass < 64; ++pass)
            {
                string before = project.WriteToString();
                foreach (string target in targets.Distinct())
                    foreach (string file in files)
                        project.RemoveFileFromBuild(target, file);
                string after = project.WriteToString();
                if (before == after) return;
                project.ReadFromString(after);
            }
            throw new InvalidOperationException("[Backtrace] Cannot normalize duplicate iOS framework build entries.");
        }

        private static string[] BuildPropertyValues(string value)
        {
            if (string.IsNullOrEmpty(value)) return new string[0];
            // GetBuildPropertyForConfig joins the values with spaces. Preserve
            // quoted paths as individual values when restoring that list.
            return Regex.Matches(value, @"(?:[^\s""]|""(?:\\.|[^""])*"")+")
                .Cast<Match>().Select(match => match.Value).ToArray();
        }

        private static IEnumerable<string> AppBuildConfigurationGuids(PBXProject project, string app)
        {
            // BuildConfigNames exposes only project-level names, while the embed helper overwrites every app configuration, including app-only ones.
            // Read names solely from Unity's serialized configuration section, then resolve ownership with the public API. No raw project text is edited.
            string section = Regex.Match(project.WriteToString(),
                @"/\* Begin XCBuildConfiguration section \*/(?<body>[\s\S]*?)/\* End XCBuildConfiguration section \*/")
                .Groups["body"].Value;
            var names = new HashSet<string>(project.BuildConfigNames(), StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(section, @"\bname = (?<name>""(?:\\.|[^""])*""|[^;\r\n]+);"))
            {
                string name = match.Groups["name"].Value.Trim();
                if (name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"')
                    name = Regex.Replace(name.Substring(1, name.Length - 2), @"\\([""\\])", "$1");
                names.Add(name);
            }
            return names.Select(name => project.BuildConfigByName(app, name))
                .Where(guid => !string.IsNullOrEmpty(guid)).Distinct();
        }

        private static void AddProperty(PBXProject project, string target, string name, string value)
        {
            // PBXProject de-duplicates values; preserve settings supplied by the host.
            project.UpdateBuildProperty(target, name, new[] { "$(inherited)", value }, new string[0]);
        }
    }
}
#endif
