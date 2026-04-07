#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Backtrace.Unity.Editor.MacOS
{
    /// <summary>
    /// Repairs the exported Backtrace.framework symlink layout on macOS when a package
    /// import/export flow has flattened the framework's versioned aliases before code signing.
    /// </summary>
    internal static class BacktraceMacFrameworkLayoutPostBuild
    {
        private const int PostBuildOrder = -1000;

        [PostProcessBuild(PostBuildOrder)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (!buildTarget.ToString().StartsWith("StandaloneOSX", StringComparison.Ordinal))
            {
                return;
            }

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                Debug.LogWarning("[Backtrace] macOS framework layout check skipped because symlink repair is only supported when building from the macOS editor.");
                return;
            }

            var frameworkPath = Path.Combine(
                buildPath,
                "Contents",
                "PlugIns",
                "BacktraceMacUnity.bundle",
                "Contents",
                "Frameworks",
                "Backtrace.framework"
            );

            // Do not fail the whole build if the macOS bundle is not present in the exported player.
            if (!Directory.Exists(frameworkPath))
            {
                return;
            }

            var versionsDir = Path.Combine(frameworkPath, "Versions");
            var versionADir = Path.Combine(versionsDir, "A");
            var versionBinary = Path.Combine(versionADir, "Backtrace");
            var versionResourcesDir = Path.Combine(versionADir, "Resources");
            var versionInfoPlist = Path.Combine(versionResourcesDir, "Info.plist");

            // Only repair when the authoritative versioned payload exists.
            if (!Directory.Exists(versionADir)
                || !File.Exists(versionBinary)
                || !Directory.Exists(versionResourcesDir)
                || !File.Exists(versionInfoPlist))
            {
                throw new BuildFailedException(
                    "[Backtrace] Exported Backtrace.framework is missing the expected versioned payload under Versions/A. " +
                    "The SDK cannot safely repair this framework layout. Reinstall the package via OpenUPM or Git and rebuild."
                );
            }

            if (HasCanonicalLayout(frameworkPath))
            {
                return;
            }

            Debug.Log("[Backtrace] Repairing exported Backtrace.framework symlink layout before signing.");

            RemoveIfPresent(Path.Combine(versionsDir, "Current"));
            RemoveIfPresent(Path.Combine(frameworkPath, "Backtrace"));
            RemoveIfPresent(Path.Combine(frameworkPath, "Resources"));
            RemoveIfPresent(Path.Combine(frameworkPath, "A"));

            CreateSymlink("A", Path.Combine(versionsDir, "Current"));
            CreateSymlink("Versions/Current/Backtrace", Path.Combine(frameworkPath, "Backtrace"));
            CreateSymlink("Versions/Current/Resources", Path.Combine(frameworkPath, "Resources"));

            if (!HasCanonicalLayout(frameworkPath))
            {
                throw new BuildFailedException(
                    "[Backtrace] Failed to repair exported Backtrace.framework to the canonical macOS layout."
                );
            }

            Debug.Log("[Backtrace] Repaired exported Backtrace.framework symlink layout.");
        }

        private static bool HasCanonicalLayout(string frameworkPath)
        {
            var versionsDir = Path.Combine(frameworkPath, "Versions");
            var versionADir = Path.Combine(versionsDir, "A");
            var versionBinary = Path.Combine(versionADir, "Backtrace");
            var versionInfoPlist = Path.Combine(versionADir, "Resources", "Info.plist");

            if (!Directory.Exists(versionADir) || !File.Exists(versionBinary) || !File.Exists(versionInfoPlist))
            {
                return false;
            }

            return IsSymlinkWithTarget(Path.Combine(versionsDir, "Current"), "A")
                && IsSymlinkWithTarget(Path.Combine(frameworkPath, "Backtrace"), "Versions/Current/Backtrace")
                && IsSymlinkWithTarget(Path.Combine(frameworkPath, "Resources"), "Versions/Current/Resources")
                && !PathExists(Path.Combine(frameworkPath, "A"));
        }

        private static bool IsSymlinkWithTarget(string path, string expectedTarget)
        {
            return IsSymlink(path)
                && string.Equals(ReadLink(path), expectedTarget, StringComparison.Ordinal);
        }

        private static bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path) || IsSymlink(path);
        }

        private static bool IsSymlink(string path)
        {
            return Execute("/bin/test", "-L " + Quote(path)).ExitCode == 0;
        }

        private static string ReadLink(string path)
        {
            var result = Execute("/usr/bin/readlink", Quote(path));
            return result.ExitCode == 0 ? result.StandardOutput.Trim() : string.Empty;
        }

        private static void CreateSymlink(string target, string linkPath)
        {
            EnsureSuccess("/bin/ln", "-sfn " + Quote(target) + " " + Quote(linkPath));
        }

        private static void RemoveIfPresent(string path)
        {
            if (!PathExists(path))
            {
                return;
            }

            EnsureSuccess("/bin/rm", "-rf " + Quote(path));
        }

        private static void EnsureSuccess(string fileName, string arguments)
        {
            var result = Execute(fileName, arguments);
            if (result.ExitCode == 0)
            {
                return;
            }

            throw new BuildFailedException(
                string.Format(
                    "[Backtrace] Command failed: {0} {1}\nExitCode: {2}\nstdout:\n{3}\nstderr:\n{4}",
                    fileName,
                    arguments,
                    result.ExitCode,
                    result.StandardOutput,
                    result.StandardError
                )
            );
        }

        private static CommandResult Execute(string fileName, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();

                process.WaitForExit();

                return new CommandResult(process.ExitCode, standardOutput, standardError);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private sealed class CommandResult
        {
            public readonly int ExitCode;
            public readonly string StandardOutput;
            public readonly string StandardError;

            public CommandResult(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }
        }
    }
}
#endif