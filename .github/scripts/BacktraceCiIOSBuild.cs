using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// CI fixture only: do not copy this into the SDK's Editor folder.
internal static class BacktraceCiIOSBuild
{
    public static void Build()
    {
        RunEditorTests();
        string[] args = Environment.GetCommandLineArgs();
        Func<string, string> argument = name =>
        {
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 == args.Length) throw new BuildFailedException("Missing " + name);
            return args[index + 1];
        };
        string sdk = argument("-btIOSSDK");
        if (sdk != "Device" && sdk != "Simulator") throw new BuildFailedException("Invalid CI iOS SDK");
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.sdkVersion = sdk == "Device" ? iOSSdkVersion.DeviceSDK : iOSSdkVersion.SimulatorSDK;
        if (sdk == "Simulator")
        {
#if UNITY_6000_0_OR_NEWER
            // Match the arm64 architecture used by the subsequent Xcode build.
            // Unity otherwise exports x86_64-only engine libraries by default.
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
#else
            throw new BuildFailedException("The arm64 iOS Simulator CI build requires Unity 6 or newer.");
#endif
        }
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = argument("-customBuildPath"),
            target = BuildTarget.iOS,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("iOS export failed");
    }

    public static void RunEditorTests()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            throw new BuildFailedException("The iOS Editor tests require the active iOS target.");

        string artifacts = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/ios-editor"));
        Directory.CreateDirectory(artifacts);
        var runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        var callbacks = new EditorTestResults(artifacts);
        runner.RegisterCallbacks(callbacks);
        try
        {
            // These five NUnit tests do not yield.
            // Synchronous execution finishes before game-ci's -quit takes effect and before the player export.
            runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                testNames = new[] { "Backtrace.Unity.Tests.Editor.BacktraceXcodePostBuildTests" }
            }) { runSynchronously = true });
            if (callbacks.Result == null || callbacks.Result.PassCount == 0 ||
                callbacks.Result.FailCount != 0 || callbacks.Result.SkipCount != 0 ||
                callbacks.Result.InconclusiveCount != 0)
                throw new BuildFailedException("iOS Editor tests did not all pass; inspect artifacts/ios-editor.");
        }
        catch (Exception error)
        {
            File.AppendAllText(callbacks.LogPath, error + Environment.NewLine);
            throw;
        }
        finally
        {
            runner.UnregisterCallbacks(callbacks);
            UnityEngine.Object.DestroyImmediate(runner);
        }
    }

    private sealed class EditorTestResults : ICallbacks
    {
        private readonly string _resultsPath;
        public readonly string LogPath;
        public ITestResultAdaptor Result;

        public EditorTestResults(string artifacts)
        {
            _resultsPath = Path.Combine(artifacts, "editmode-results.xml");
            LogPath = Path.Combine(artifacts, "editmode.log");
            if (File.Exists(_resultsPath)) File.Delete(_resultsPath);
            File.WriteAllText(LogPath, "Unity " + Application.unityVersion + ", EditMode, active target " +
                EditorUserBuildSettings.activeBuildTarget + Environment.NewLine);
        }

        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result)
        {
            File.AppendAllText(LogPath, result.FullName + ": " + result.ResultState + Environment.NewLine +
                result.Message + Environment.NewLine + result.StackTrace + Environment.NewLine + result.Output);
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, _resultsPath);
            Result = result;
        }
    }
}
