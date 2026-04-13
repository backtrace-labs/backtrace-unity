#if UNITY_EDITOR

using System;
using System.Collections.Generic;
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
    /// Restore exported Backtrace macOS native plugin layout after Unity build/export.
    ///
    /// Repair Backtrace.framework symlink layout when Unity flattens versioned framework aliases.
    /// Normalize Xcode project references from "Plugins" to "PlugIns" when Unity emits incorrect casing.
    ///
    /// This runs only for macOS standalone exports.
    /// </summary>
    internal static class BacktraceMacFrameworkLayoutPostBuild
    {
        private const int PostBuildOrder = -1000;
        private const int CommandTimeoutMilliseconds = 30000;

        private const string PlugInsDirectoryName = "PlugIns";
        private const string LegacyPluginsDirectoryName = "Plugins";

        private const string PluginBundleName = "BacktraceMacUnity.bundle";
        private const string FrameworkExtension = ".framework";
        private const string FrameworkName = "Backtrace.framework";
        private const string VersionsDirectoryName = "Versions";
        private const string CurrentVersionLinkName = "Current";
        private const string ResourcesDirectoryName = "Resources";
        private const string InfoPlistFileName = "Info.plist";
        private const string XcodeProjectFileName = "project.pbxproj";

        [PostProcessBuild(PostBuildOrder)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (!IsMacStandaloneBuildTarget(buildTarget))
            {
                return;
            }

            if (string.IsNullOrEmpty(buildPath) || !Directory.Exists(buildPath))
            {
                return;
            }

            NormalizeXcodeProjectPluginPathCasing(buildPath);

            var frameworkPaths = FindFrameworkPaths(buildPath);
            if (frameworkPaths.Count == 0)
            {
                return;
            }

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                throw new BuildFailedException(
                    "[Backtrace] Detected the Backtrace macOS native plugin in the exported player, but framework verification and repair " +
                    "requires building from the macOS editor so the SDK can create and validate the required POSIX symlinks. " +
                    "Build the macOS player on a macOS machine, or disable the Backtrace macOS native plugin for this build.");
            }

            for (var i = 0; i < frameworkPaths.Count; i++)
            {
                RepairFrameworkLayout(frameworkPaths[i]);
            }
        }

        private static void NormalizeXcodeProjectPluginPathCasing(string buildPath)
        {
            string[] projectFiles;

            try
            {
                projectFiles = Directory.GetFiles(buildPath, XcodeProjectFileName, SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Failed to search for Xcode project files under '{0}'. {1}",
                        buildPath,
                        ex.Message));
            }

            for (var i = 0; i < projectFiles.Length; i++)
            {
                NormalizeSingleXcodeProjectPluginPathCasing(projectFiles[i]);
            }
        }

        private static void NormalizeSingleXcodeProjectPluginPathCasing(string projectFilePath)
        {
            var xcodeProjectDirectory = Path.GetDirectoryName(projectFilePath);
            if (string.IsNullOrEmpty(xcodeProjectDirectory))
            {
                return;
            }

            var projectRootDirectory = Directory.GetParent(xcodeProjectDirectory);
            if (projectRootDirectory == null)
            {
                return;
            }

            var projectRootPath = projectRootDirectory.FullName;

            var correctPluginBundlePath = Path.Combine(projectRootPath, PlugInsDirectoryName, PluginBundleName);
            var legacyPluginBundlePath = Path.Combine(projectRootPath, LegacyPluginsDirectoryName, PluginBundleName);

            var hasCorrectPlugInsPath = Directory.Exists(correctPluginBundlePath);
            var hasLegacyPluginsPath = Directory.Exists(legacyPluginBundlePath);

            if (!hasCorrectPlugInsPath || hasLegacyPluginsPath)
            {
                return;
            }

            string contents;
            try
            {
                contents = File.ReadAllText(projectFilePath);
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Failed to read Xcode project file '{0}'. {1}",
                        projectFilePath,
                        ex.Message));
            }

            var originalContents = contents;

            // Normalize only the plugin path casing relevant to the Backtrace macOS plugin.
            contents = contents.Replace(
                "/" + LegacyPluginsDirectoryName + "/" + PluginBundleName,
                "/" + PlugInsDirectoryName + "/" + PluginBundleName);

            contents = contents.Replace(
                "=" + " " + LegacyPluginsDirectoryName + "/" + PluginBundleName + ";",
                "= " + PlugInsDirectoryName + "/" + PluginBundleName + ";");

            contents = contents.Replace(
                "= " + LegacyPluginsDirectoryName + ";",
                "= " + PlugInsDirectoryName + ";");

            contents = contents.Replace(
                "/BacktraceTestProject/" + LegacyPluginsDirectoryName + "/" + PluginBundleName,
                "/BacktraceTestProject/" + PlugInsDirectoryName + "/" + PluginBundleName);

            // Also cover quoted path strings that Xcode may emit.
            contents = contents.Replace(
                "\"" + LegacyPluginsDirectoryName + "/" + PluginBundleName + "\"",
                "\"" + PlugInsDirectoryName + "/" + PluginBundleName + "\"");

            contents = contents.Replace(
                "\"BacktraceTestProject/" + LegacyPluginsDirectoryName + "/" + PluginBundleName + "\"",
                "\"BacktraceTestProject/" + PlugInsDirectoryName + "/" + PluginBundleName + "\"");

            if (string.Equals(contents, originalContents, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                File.WriteAllText(projectFilePath, contents);
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Failed to update Xcode project file '{0}' with corrected PlugIns path casing. {1}",
                        projectFilePath,
                        ex.Message));
            }

            Debug.Log(
                string.Format(
                    "[Backtrace] Normalized Xcode project PlugIns path casing in '{0}'.",
                    projectFilePath));
        }

        private static void RepairFrameworkLayout(string frameworkPath)
        {
            var binaryName = GetFrameworkBinaryName(frameworkPath);
            var versionsDirectory = Path.Combine(frameworkPath, VersionsDirectoryName);
            var activeVersionName = ResolveActiveVersionName(frameworkPath, versionsDirectory, binaryName);
            var issues = GetCanonicalLayoutIssues(frameworkPath, versionsDirectory, activeVersionName, binaryName);

            if (issues.Count == 0)
            {
                return;
            }

            Debug.Log(
                string.Format(
                    "[Backtrace] Repairing exported framework layout at '{0}'. Detected issues: {1}",
                    frameworkPath,
                    string.Join("; ", issues.ToArray())));

            RemoveIfPresent(Path.Combine(versionsDirectory, CurrentVersionLinkName));
            RemoveIfPresent(Path.Combine(frameworkPath, binaryName));
            RemoveIfPresent(Path.Combine(frameworkPath, ResourcesDirectoryName));
            RemoveIfPresent(Path.Combine(frameworkPath, activeVersionName));

            CreateSymlink(activeVersionName, Path.Combine(versionsDirectory, CurrentVersionLinkName));
            CreateSymlink(
                VersionsDirectoryName + "/" + CurrentVersionLinkName + "/" + binaryName,
                Path.Combine(frameworkPath, binaryName));
            CreateSymlink(
                VersionsDirectoryName + "/" + CurrentVersionLinkName + "/" + ResourcesDirectoryName,
                Path.Combine(frameworkPath, ResourcesDirectoryName));

            var remainingIssues = GetCanonicalLayoutIssues(frameworkPath, versionsDirectory, activeVersionName, binaryName);
            if (remainingIssues.Count != 0)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Failed to repair exported framework layout at '{0}'. Remaining issues: {1}",
                        frameworkPath,
                        string.Join("; ", remainingIssues.ToArray())));
            }

            Debug.Log(
                string.Format(
                    "[Backtrace] Repaired exported framework layout at '{0}' using version '{1}'.",
                    frameworkPath,
                    activeVersionName));
        }

        private static string ResolveActiveVersionName(string frameworkPath, string versionsDirectory, string binaryName)
        {
            if (!Directory.Exists(versionsDirectory))
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Exported framework at '{0}' is missing its '{1}' directory. The SDK cannot safely repair this layout. Reinstall the Backtrace package and rebuild.",
                        frameworkPath,
                        VersionsDirectoryName));
            }

            var currentPath = Path.Combine(versionsDirectory, CurrentVersionLinkName);

            var versionFromCurrentSymlink = TryResolveVersionNameFromCurrentSymlink(currentPath, versionsDirectory, binaryName);
            if (!string.IsNullOrEmpty(versionFromCurrentSymlink))
            {
                return versionFromCurrentSymlink;
            }

            var versionFromCurrentFile = TryResolveVersionNameFromCurrentFile(currentPath, versionsDirectory, binaryName);
            if (!string.IsNullOrEmpty(versionFromCurrentFile))
            {
                return versionFromCurrentFile;
            }

            var validVersions = FindValidVersionDirectories(versionsDirectory, binaryName);
            if (validVersions.Count == 1)
            {
                return validVersions[0];
            }

            if (validVersions.Count == 0)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Exported framework at '{0}' is missing a valid versioned payload under '{1}'. Expected a directory containing '{2}' and '{3}/{4}'. Reinstall the Backtrace package and rebuild.",
                        frameworkPath,
                        versionsDirectory,
                        binaryName,
                        ResourcesDirectoryName,
                        InfoPlistFileName));
            }

            throw new BuildFailedException(
                string.Format(
                    "[Backtrace] Exported framework at '{0}' contains multiple valid versioned payloads ({1}) but the active version could not be resolved from '{2}'. The SDK cannot safely choose which version to activate. Reinstall the Backtrace package and rebuild.",
                    frameworkPath,
                    string.Join(", ", validVersions.ToArray()),
                    currentPath));
        }

        private static string TryResolveVersionNameFromCurrentSymlink(string currentPath, string versionsDirectory, string binaryName)
        {
            if (!IsSymlink(currentPath))
            {
                return string.Empty;
            }

            var linkTarget = ReadLink(currentPath);
            if (string.IsNullOrEmpty(linkTarget))
            {
                return string.Empty;
            }

            var candidateVersionName = ExtractTerminalPathComponent(linkTarget);
            if (string.IsNullOrEmpty(candidateVersionName))
            {
                return string.Empty;
            }

            return HasValidVersionPayload(Path.Combine(versionsDirectory, candidateVersionName), binaryName)
                ? candidateVersionName
                : string.Empty;
        }

        private static string TryResolveVersionNameFromCurrentFile(string currentPath, string versionsDirectory, string binaryName)
        {
            if (!File.Exists(currentPath) || Directory.Exists(currentPath))
            {
                return string.Empty;
            }

            string fileContents;
            try
            {
                fileContents = File.ReadAllText(currentPath).Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(fileContents))
            {
                return string.Empty;
            }

            var candidateVersionName = ExtractTerminalPathComponent(fileContents);
            if (string.IsNullOrEmpty(candidateVersionName))
            {
                return string.Empty;
            }

            return HasValidVersionPayload(Path.Combine(versionsDirectory, candidateVersionName), binaryName)
                ? candidateVersionName
                : string.Empty;
        }

        private static string ExtractTerminalPathComponent(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var trimmed = path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFileName(trimmed);
        }

        private static List<string> FindValidVersionDirectories(string versionsDirectory, string binaryName)
        {
            var validVersions = new List<string>();

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(versionsDirectory);
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Failed to enumerate framework versions under '{0}'. {1}",
                        versionsDirectory,
                        ex.Message));
            }

            for (var i = 0; i < directories.Length; i++)
            {
                var versionDirectory = directories[i];
                var versionName = Path.GetFileName(versionDirectory);
                if (string.Equals(versionName, CurrentVersionLinkName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (HasValidVersionPayload(versionDirectory, binaryName))
                {
                    validVersions.Add(versionName);
                }
            }

            return validVersions;
        }

        private static bool HasValidVersionPayload(string versionDirectory, string binaryName)
        {
            if (!Directory.Exists(versionDirectory))
            {
                return false;
            }

            var binaryPath = Path.Combine(versionDirectory, binaryName);
            var resourcesDirectory = Path.Combine(versionDirectory, ResourcesDirectoryName);
            var infoPlistPath = Path.Combine(resourcesDirectory, InfoPlistFileName);

            return File.Exists(binaryPath)
                   && Directory.Exists(resourcesDirectory)
                   && File.Exists(infoPlistPath);
        }

        private static List<string> GetCanonicalLayoutIssues(string frameworkPath, string versionsDirectory, string activeVersionName, string binaryName)
        {
            var issues = new List<string>();
            var activeVersionDirectory = Path.Combine(versionsDirectory, activeVersionName);

            if (!HasValidVersionPayload(activeVersionDirectory, binaryName))
            {
                issues.Add(
                    string.Format(
                        "missing valid versioned payload under '{0}'",
                        activeVersionDirectory));
                return issues;
            }

            var currentPath = Path.Combine(versionsDirectory, CurrentVersionLinkName);
            if (!IsSymlinkWithTarget(currentPath, activeVersionName))
            {
                issues.Add(
                    string.Format(
                        "'{0}/{1}' is not a symlink to '{2}'",
                        VersionsDirectoryName,
                        CurrentVersionLinkName,
                        activeVersionName));
            }

            var rootBinaryPath = Path.Combine(frameworkPath, binaryName);
            var expectedBinaryTarget = VersionsDirectoryName + "/" + CurrentVersionLinkName + "/" + binaryName;
            if (!IsSymlinkWithTarget(rootBinaryPath, expectedBinaryTarget))
            {
                issues.Add(
                    string.Format(
                        "'{0}' is not a symlink to '{1}'",
                        binaryName,
                        expectedBinaryTarget));
            }

            var rootResourcesPath = Path.Combine(frameworkPath, ResourcesDirectoryName);
            var expectedResourcesTarget = VersionsDirectoryName + "/" + CurrentVersionLinkName + "/" + ResourcesDirectoryName;
            if (!IsSymlinkWithTarget(rootResourcesPath, expectedResourcesTarget))
            {
                issues.Add(
                    string.Format(
                        "'{0}' is not a symlink to '{1}'",
                        ResourcesDirectoryName,
                        expectedResourcesTarget));
            }

            var unexpectedRootVersionEntry = Path.Combine(frameworkPath, activeVersionName);
            if (PathExists(unexpectedRootVersionEntry))
            {
                issues.Add(
                    string.Format(
                        "unexpected top-level '{0}' entry exists",
                        activeVersionName));
            }

            return issues;
        }

        private static List<string> FindFrameworkPaths(string buildPath)
        {
            var frameworkPaths = new List<string>();

            if (string.IsNullOrEmpty(buildPath) || !Directory.Exists(buildPath))
            {
                return frameworkPaths;
            }

            string[] bundlePaths;
            try
            {
                bundlePaths = Directory.GetDirectories(buildPath, PluginBundleName, SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Failed to locate '{0}' in exported build output '{1}'. {2}",
                        PluginBundleName,
                        buildPath,
                        ex.Message));
            }

            for (var i = 0; i < bundlePaths.Length; i++)
            {
                if (!IsUnderPlugInsDirectory(bundlePaths[i]))
                {
                    continue;
                }

                var frameworkPath = Path.Combine(bundlePaths[i], "Contents", "Frameworks", FrameworkName);
                if (!Directory.Exists(frameworkPath))
                {
                    continue;
                }

                if (!frameworkPaths.Contains(frameworkPath))
                {
                    frameworkPaths.Add(frameworkPath);
                }
            }

            return frameworkPaths;
        }

        private static bool IsUnderPlugInsDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var normalized = path.Replace('\\', '/');
            return normalized.Contains("/" + PlugInsDirectoryName + "/");
        }

        private static string GetFrameworkBinaryName(string frameworkPath)
        {
            var frameworkDirectoryName = Path.GetFileName(frameworkPath);
            if (string.IsNullOrEmpty(frameworkDirectoryName) || !frameworkDirectoryName.EndsWith(FrameworkExtension, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Invalid framework path '{0}'. Expected a path ending with '{1}'.",
                        frameworkPath,
                        FrameworkExtension));
            }

            return frameworkDirectoryName.Substring(0, frameworkDirectoryName.Length - FrameworkExtension.Length);
        }

        private static bool IsMacStandaloneBuildTarget(BuildTarget buildTarget)
        {
            var name = buildTarget.ToString();
            return string.Equals(name, "StandaloneOSX", StringComparison.Ordinal)
                   || string.Equals(name, "StandaloneOSXIntel", StringComparison.Ordinal)
                   || string.Equals(name, "StandaloneOSXIntel64", StringComparison.Ordinal)
                   || string.Equals(name, "StandaloneOSXUniversal", StringComparison.Ordinal);
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
            if (result.TimedOut)
            {
                throw new BuildFailedException(
                    string.Format(
                        "[Backtrace] Command timed out after {0} ms: {1} {2}",
                        CommandTimeoutMilliseconds,
                        fileName,
                        arguments));
            }

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
                    result.StandardError));
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

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    throw new BuildFailedException(
                        string.Format(
                            "[Backtrace] Failed to start command '{0} {1}'. {2}",
                            fileName,
                            arguments,
                            ex.Message));
                }

                if (!process.WaitForExit(CommandTimeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }

                    return new CommandResult(-1, SafeReadToEnd(process.StandardOutput), SafeReadToEnd(process.StandardError), true);
                }

                return new CommandResult(
                    process.ExitCode,
                    SafeReadToEnd(process.StandardOutput),
                    SafeReadToEnd(process.StandardError),
                    false);
            }
        }

        private static string SafeReadToEnd(StreamReader reader)
        {
            try
            {
                return reader.ReadToEnd();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string Quote(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private struct CommandResult
        {
            public readonly int ExitCode;
            public readonly string StandardOutput;
            public readonly string StandardError;
            public readonly bool TimedOut;

            public CommandResult(int exitCode, string standardOutput, string standardError, bool timedOut)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput ?? string.Empty;
                StandardError = standardError ?? string.Empty;
                TimedOut = timedOut;
            }
        }
    }
}

#endif
