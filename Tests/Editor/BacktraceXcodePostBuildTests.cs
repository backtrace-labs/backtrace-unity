#if UNITY_EDITOR && UNITY_IOS && UNITY_2019_3_OR_NEWER
using System;
using System.IO;
using System.Text.RegularExpressions;
using Backtrace.Unity.Editor.iOS;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace Backtrace.Unity.Tests.Editor
{
    public class BacktraceXcodePostBuildTests
    {
        private string _root;
        private string _minimumIOS;
        private string ProjectPath { get { return PBXProject.GetPBXProjectPath(_root); } }

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "backtrace-xcode-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "Unity-iPhone.xcodeproj"));
            _minimumIOS = PlayerSettings.iOS.targetOSVersionString;
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            Directory.CreateDirectory(Path.Combine(_root, "Frameworks/Backtrace.xcframework"));
            string crashReporter = Path.Combine(_root, "Frameworks/CrashReporter.xcframework");
            Directory.CreateDirectory(Path.Combine(crashReporter, "ios-arm64/CrashReporter.framework"));
            Directory.CreateDirectory(Path.Combine(crashReporter, "ios-arm64_x86_64-simulator/CrashReporter.framework"));
            var info = new PlistDocument();
            var libraries = info.root.CreateArray("AvailableLibraries");
            foreach (bool simulator in new[] { false, true })
            {
                var library = libraries.AddDict();
                library.SetString("SupportedPlatform", "ios");
                library.SetString("LibraryIdentifier", simulator ? "ios-arm64_x86_64-simulator" : "ios-arm64");
                library.SetString("LibraryPath", "CrashReporter.framework");
                if (simulator) library.SetString("SupportedPlatformVariant", "simulator");
            }
            info.WriteToFile(Path.Combine(crashReporter, "Info.plist"));
            new PlistDocument().WriteToFile(Path.Combine(crashReporter, "PrivacyInfo.xcprivacy"));
            File.WriteAllText(Path.Combine(_root, "PrivacyInfo.xcprivacy"), "host-privacy-sentinel");

            var project = NewProject();
            string app = project.AddTarget("Unity-iPhone", "app", "com.apple.product-type.application");
            string code = project.AddTarget("UnityFramework", "framework", "com.apple.product-type.framework");
            foreach (string target in new[] { app, code })
            {
                project.AddFrameworksBuildPhase(target);
                project.AddResourcesBuildPhase(target);
                project.AddSourcesBuildPhase(target);
            }
            string backtrace = project.AddFile("Frameworks/Backtrace.xcframework", "Frameworks/Backtrace.xcframework");
            string crash = project.AddFile("Frameworks/CrashReporter.xcframework", "Frameworks/CrashReporter.xcframework");
            foreach (string target in new[] { app, code })
            {
                project.AddFileToBuild(target, backtrace);
                project.AddFileToBuild(target, crash);
                PBXProjectExtensions.AddFileToEmbedFrameworks(project, target, backtrace);
                PBXProjectExtensions.AddFileToEmbedFrameworks(project, target, crash);
            }
            string unrelated = project.AddFile("Frameworks/Unrelated.framework", "Frameworks/Unrelated.framework");
            project.AddFileToBuild(code, unrelated);
            PBXProjectExtensions.AddFileToEmbedFrameworks(project, app, unrelated);
            project.AddBuildConfig("ReleaseForProfiling");
            project.SetBuildPropertyForConfig(project.BuildConfigByName(app, "Debug"), "LD_RUNPATH_SEARCH_PATHS", "@loader_path/DebugOnly");
            project.AddBuildPropertyForConfig(project.BuildConfigByName(app, "Debug"), "LD_RUNPATH_SEARCH_PATHS", "\"@loader_path/My Shared Frameworks\"");
            project.SetBuildPropertyForConfig(project.BuildConfigByName(app, "Release"), "LD_RUNPATH_SEARCH_PATHS", "@loader_path/ReleaseOnly");
            project.SetBuildPropertyForConfig(project.BuildConfigByName(app, "ReleaseForProfiling"), "LD_RUNPATH_SEARCH_PATHS", "@loader_path/ProfilingOnly");
            project.WriteToFile(ProjectPath);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerSettings.iOS.targetOSVersionString = _minimumIOS;
            if (_root != null && Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void RepeatedPostprocessingRemovesStaticLinkageAndDuplicateEntries()
        {
            BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root);
            string first = File.ReadAllText(ProjectPath);
            AssertArrangement(first);
            BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root);
            string second = File.ReadAllText(ProjectPath);
            AssertArrangement(second);
            foreach (string type in new[] { "PBXBuildFile", "PBXFileReference", "PBXCopyFilesBuildPhase" })
                Assert.AreEqual(Count(first, "isa = " + type + ";"), Count(second, "isa = " + type + ";"), type);
            Assert.AreEqual("host-privacy-sentinel", File.ReadAllText(Path.Combine(_root, "PrivacyInfo.xcprivacy")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "BacktracePLCrashReporterPrivacy.bundle/PrivacyInfo.xcprivacy")));
        }

        [Test]
        public void RepeatedPostprocessingPreservesPerConfigurationAndQuotedRunPaths()
        {
            BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root);
            BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root);
            var project = new PBXProject();
            project.ReadFromFile(ProjectPath);
            string app = project.GetUnityMainTargetGuid();
            Assert.AreEqual("@loader_path/DebugOnly \"@loader_path/My Shared Frameworks\" $(inherited) @executable_path/Frameworks",
                project.GetBuildPropertyForConfig(project.BuildConfigByName(app, "Debug"), "LD_RUNPATH_SEARCH_PATHS"));
            Assert.AreEqual("@loader_path/ReleaseOnly $(inherited) @executable_path/Frameworks",
                project.GetBuildPropertyForConfig(project.BuildConfigByName(app, "Release"), "LD_RUNPATH_SEARCH_PATHS"));
            Assert.AreEqual("@loader_path/ProfilingOnly $(inherited) @executable_path/Frameworks",
                project.GetBuildPropertyForConfig(project.BuildConfigByName(app, "ReleaseForProfiling"), "LD_RUNPATH_SEARCH_PATHS"));
        }

        [Test]
        public void AppOnlyConfigurationWithEscapedNameRetainsItsRunPaths()
        {
            var project = new PBXProject();
            project.ReadFromFile(ProjectPath);
            string app = project.GetUnityMainTargetGuid();
            string configuration = project.BuildConfigByName(app, "ReleaseForProfiling");
            // The public API cannot create an app-only configuration.
            // Rename just this fixture's existing app configuration, keeping its object ID.
            string pattern = @"(" + Regex.Escape(configuration) + @" /\* ReleaseForProfiling \*/ = \{[\s\S]*?\bname = )ReleaseForProfiling;";
            string renamed = Regex.Replace(project.WriteToString(), pattern,
                match => match.Groups[1].Value + "\"App-only \\\"Profiling\\\"\";");
            Assert.AreNotEqual(project.WriteToString(), renamed);
            project.ReadFromString(renamed);
            Assert.IsNotNull(project.BuildConfigByName(app, "App-only \"Profiling\""));
            project.WriteToFile(ProjectPath);

            BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root);
            BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root);
            project.ReadFromFile(ProjectPath);
            Assert.AreEqual("@loader_path/ProfilingOnly $(inherited) @executable_path/Frameworks",
                project.GetBuildPropertyForConfig(configuration, "LD_RUNPATH_SEARCH_PATHS"));
        }

        [Test]
        public void UnsupportedMinimumIsRejectedWithoutChangingProjectOrPlayerSetting()
        {
            PlayerSettings.iOS.targetOSVersionString = "14.0";
            string before = File.ReadAllText(ProjectPath);
            Assert.Throws<InvalidOperationException>(() => BacktraceXcodePostBuild.OnPostProcessBuild(BuildTarget.iOS, _root));
            Assert.AreEqual(before, File.ReadAllText(ProjectPath));
            Assert.AreEqual("14.0", PlayerSettings.iOS.targetOSVersionString);
        }

        [Test]
        public void BridgeImporterEnablesARCAndDisablesHeaderAutolinkingOnlyForBridge()
        {
            string path = AssetDatabase.GUIDToAssetPath("3dc91150f76c2497c810a0e3f78ff34d");
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            Assert.IsNotNull(importer);
            string flags = importer.GetPlatformData(BuildTarget.iOS, "CompileFlags");
            StringAssert.Contains("-fobjc-arc", flags);
            StringAssert.Contains("-fobjc-exceptions", flags);
            StringAssert.Contains("-fno-autolink", flags);
        }

        private static void AssertArrangement(string value)
        {
            Assert.AreEqual(1, Count(value, @"Backtrace\.xcframework in Frameworks \*/ = "));
            Assert.AreEqual(1, Count(value, @"Backtrace\.xcframework in Embed Frameworks \*/ = "));
            Assert.AreEqual(0, Count(value, @"CrashReporter\.xcframework in [^\r\n]* \*/ = "));
            Assert.AreEqual(1, Count(value, @"Unrelated\.framework in Frameworks \*/ = "));
            Assert.AreEqual(1, Count(value, @"Unrelated\.framework in Embed Frameworks \*/ = "));
        }

        private static int Count(string value, string pattern)
        {
            return Regex.Matches(value, pattern).Count;
        }

        private static PBXProject NewProject()
        {
            // Minimal synthetic Xcode project. These IDs belong only to this fixture.
            var project = new PBXProject();
            project.ReadFromString(@"{archiveVersion=1;classes={};objectVersion=56;objects={
                AAAAAAAAAAAAAAAAAAAAAAAA={isa=PBXProject;buildConfigurationList=BBBBBBBBBBBBBBBBBBBBBBBB;compatibilityVersion=""Xcode 14.0"";mainGroup=CCCCCCCCCCCCCCCCCCCCCCCC;productRefGroup=DDDDDDDDDDDDDDDDDDDDDDDD;targets=();};
                BBBBBBBBBBBBBBBBBBBBBBBB={isa=XCConfigurationList;buildConfigurations=(EEEEEEEEEEEEEEEEEEEEEEEE,FFFFFFFFFFFFFFFFFFFFFFFF);defaultConfigurationIsVisible=0;defaultConfigurationName=Release;};
                CCCCCCCCCCCCCCCCCCCCCCCC={isa=PBXGroup;children=(DDDDDDDDDDDDDDDDDDDDDDDD);sourceTree=""<group>"";};
                DDDDDDDDDDDDDDDDDDDDDDDD={isa=PBXGroup;children=();name=Products;sourceTree=""<group>"";};
                EEEEEEEEEEEEEEEEEEEEEEEE={isa=XCBuildConfiguration;buildSettings={};name=Debug;};
                FFFFFFFFFFFFFFFFFFFFFFFF={isa=XCBuildConfiguration;buildSettings={};name=Release;};
            };rootObject=AAAAAAAAAAAAAAAAAAAAAAAA;}");
            return project;
        }
    }
}
#endif
