using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Copied only into synthetic CI projects, never imported by the shipping SDK.
internal static class BacktraceCiMacBuild
{
    public static void Build()
    {
        string backendName = Argument("-btMacBackend");
        ScriptingImplementation backend;
        if (backendName == "Mono") backend = ScriptingImplementation.Mono2x;
        else if (backendName == "IL2CPP") backend = ScriptingImplementation.IL2CPP;
        else throw new BuildFailedException("CI must select the Mono or IL2CPP macOS backend explicitly.");

        string output = Argument("-customBuildPath");
        if (!output.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("The macOS CI fixture requires direct .app output.");

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != backend)
            throw new BuildFailedException("The requested macOS scripting backend was not applied.");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0) throw new BuildFailedException("The macOS CI fixture has no build scene.");
        Debug.Log("Backtrace CI macOS scripting backend: " + backendName);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("The macOS " + backendName + " player build failed.");
    }

    private static string Argument(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; ++index)
            if (arguments[index] == name) return arguments[index + 1];
        throw new BuildFailedException("Missing CI build argument: " + name);
    }
}
