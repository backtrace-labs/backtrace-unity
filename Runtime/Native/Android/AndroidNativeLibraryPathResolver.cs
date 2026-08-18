#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Resolves the libbacktrace-native.so path without ever opening an APK archive, mirroring the Backtrace Android SDK resolver order:
    /// 1. the validated linker-reported path (authoritative. Android already proved what loaded),
    /// 2. an existing extracted native library,
    /// 3. the installed ABI configuration split selected from metadata,
    /// 4. the historical base-APK fallback.
    /// Only stages 3 and 4 require a process ABI, so a failing ABI provider cannot disable native capture when the linker path or an extracted library is available.
    /// </summary>
    internal static class AndroidNativeLibraryPathResolver
    {
        internal const string NativeLibraryName = "libbacktrace-native.so";
        private const string ApkLibrarySeparator = "!/";

        internal static string Resolve(
            AndroidApplicationInfoSnapshot applicationInfo,
            string loadedLibraryPath,
            string processAbi,
            string baseApkFallback)
        {
            return Resolve(applicationInfo, loadedLibraryPath, processAbi, baseApkFallback, File.Exists);
        }

        /// <summary>
        /// File existence is injectable so the resolution policy is testable in the Editor without fixture files.
        /// </summary>
        internal static string Resolve(
            AndroidApplicationInfoSnapshot applicationInfo,
            string loadedLibraryPath,
            string processAbi,
            string baseApkFallback,
            Func<string, bool> fileExists)
        {
            if (applicationInfo == null)
            {
                throw new ArgumentNullException("applicationInfo");
            }
            if (fileExists == null)
            {
                throw new ArgumentNullException("fileExists");
            }

            string validatedLoadedPath = ValidateLoadedLibraryPath(loadedLibraryPath, fileExists);
            if (validatedLoadedPath != null)
            {
                return validatedLoadedPath;
            }

            string extractedPath = GetExtractedLibraryPath(applicationInfo.NativeLibraryDir, fileExists);
            if (extractedPath != null)
            {
                return extractedPath;
            }

            if (string.IsNullOrEmpty(processAbi))
            {
                throw new InvalidOperationException("Unable to determine the current process ABI.");
            }

            string entry = "lib/" + processAbi + "/" + NativeLibraryName;
            string splitPath = FindAbiSplitPath(applicationInfo, processAbi, fileExists);
            if (splitPath != null)
            {
                return splitPath + ApkLibrarySeparator + entry;
            }

            string baseApk = FirstNonEmpty(
                applicationInfo.SourceDir,
                applicationInfo.PublicSourceDir,
                baseApkFallback);
            if (string.IsNullOrEmpty(baseApk))
            {
                throw new InvalidOperationException("Unable to determine the application APK path.");
            }
            return baseApk + ApkLibrarySeparator + entry;
        }

        /// <summary> 
        /// Selects the native-library directory for the CURRENT PROCESS from extracted-library candidates (for example the directories under an APK-adjacent "lib" directory).
        /// Enumeration order is never a selection policy: an exact ABI-mapped directory name wins
        /// (arm64-v8a maps to "arm64" or "arm64-v8a", armeabi-v7a to "arm", "armeabi-v7a", or "armeabi", x86_64 and x86 exactly, x86 can never select x86_64);
        /// without an exact match a single remaining candidate is a compatibility fallback, and anything ambiguous returns null so split/base-APK resolution decides instead.
        /// </summary>
        internal static string SelectNativeLibraryDirectory(string[] candidateDirectories, string processAbi)
        {
            if (candidateDirectories == null || candidateDirectories.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(processAbi))
            {
                string[] acceptedNames = GetAbiDirectoryNames(processAbi);
                foreach (string candidate in candidateDirectories)
                {
                    if (string.IsNullOrEmpty(candidate))
                    {
                        continue;
                    }
                    string name = NormalizeAbiToken(GetFileName(candidate.TrimEnd('/')));
                    for (int index = 0; index < acceptedNames.Length; index++)
                    {
                        if (acceptedNames[index].Equals(name, StringComparison.Ordinal))
                        {
                            return candidate;
                        }
                    }
                }
            }

            string single = null;
            foreach (string candidate in candidateDirectories)
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }
                if (single != null)
                {
                    return null;
                }
                single = candidate;
            }
            return single;
        }

        private static string[] GetAbiDirectoryNames(string processAbi)
        {
            string normalized = NormalizeAbiToken(processAbi);
            if (normalized.Equals("arm64_v8a", StringComparison.Ordinal))
            {
                return new[] { "arm64", "arm64_v8a" };
            }
            if (normalized.Equals("armeabi_v7a", StringComparison.Ordinal))
            {
                return new[] { "arm", "armeabi_v7a", "armeabi" };
            }
            return new[] { normalized };
        }

        /// <summary>
        /// Structural validation only. The path is deliberately NOT compared against an ABI inferred in C#:
        /// Android has already resolved which module this process loaded,
        /// and comparing against a guessed ABI would reject correct paths in 32-bit processes on 64-bit devices and under native-bridge translation.
        /// </summary>
        internal static string ValidateLoadedLibraryPath(string loadedLibraryPath, Func<string, bool> fileExists)
        {
            if (string.IsNullOrEmpty(loadedLibraryPath))
            {
                return null;
            }

            string path = loadedLibraryPath.Trim();
            int apkSeparatorIndex = path.IndexOf(ApkLibrarySeparator, StringComparison.Ordinal);
            if (apkSeparatorIndex >= 0)
            {
                string containerPath = path.Substring(0, apkSeparatorIndex);
                string entry = path.Substring(apkSeparatorIndex + ApkLibrarySeparator.Length);
                if (!IsBacktraceApkLibraryEntry(entry))
                {
                    return null;
                }
                return Path.IsPathRooted(containerPath) && fileExists(containerPath) ? path : null;
            }

            if (!Path.IsPathRooted(path)
                || !NativeLibraryName.Equals(Path.GetFileName(path), StringComparison.Ordinal)
                || !fileExists(path))
            {
                return null;
            }
            return path;
        }

        /// <summary>
        /// Accepts lib/&lt;abi&gt;/libbacktrace-native.so for any single-segment ABI directory.
        /// </summary>
        private static bool IsBacktraceApkLibraryEntry(string entry)
        {
            const string prefix = "lib/";
            string suffix = "/" + NativeLibraryName;
            if (entry == null
                || !entry.StartsWith(prefix, StringComparison.Ordinal)
                || !entry.EndsWith(suffix, StringComparison.Ordinal))
            {
                return false;
            }

            int abiEnd = entry.Length - suffix.Length;
            if (abiEnd <= prefix.Length)
            {
                return false;
            }

            string abi = entry.Substring(prefix.Length, abiEnd - prefix.Length);
            return abi.IndexOf('/') < 0;
        }

        private static string GetExtractedLibraryPath(string nativeLibraryDir, Func<string, bool> fileExists)
        {
            if (string.IsNullOrEmpty(nativeLibraryDir))
            {
                return null;
            }

            string extractedLibrary = nativeLibraryDir.TrimEnd('/') + "/" + NativeLibraryName;
            return fileExists(extractedLibrary) ? extractedLibrary : null;
        }

        /// <summary>
        /// Selects the ABI split across SplitSourceDirs and SplitPublicSourceDirs together,
        /// deduplicated by path and compared globally by match confidence,
        /// so a loose match in one array can never outrank an exact base configuration split in the other.
        /// Two distinct candidates of equal confidence cannot be told apart without opening the archives,
        /// so an ambiguous result returns null and resolution falls through to the base-APK fallback instead of picking by array order.
        /// </summary>
        internal static string FindAbiSplitPath(
            AndroidApplicationInfoSnapshot applicationInfo,
            string processAbi,
            Func<string, bool> fileExists)
        {
            string[] splitNames = applicationInfo.SplitNames;
            // Loose filename-token matching represents installs without split-name metadata (pre-API-26).
            // It is decided globally: when any split-name metadata exists
            // a candidate without an aligned name (for example one beyond a truncated splitNames array) must not regain the low-confidence score named candidates correctly lose.
            bool allowLooseFilenameMatching = splitNames == null;

            var candidates = new List<KeyValuePair<string, int>>();
            CollectAbiSplitCandidates(
                candidates, applicationInfo.SplitSourceDirs, splitNames, processAbi, allowLooseFilenameMatching, fileExists);
            CollectAbiSplitCandidates(
                candidates,
                applicationInfo.SplitPublicSourceDirs,
                splitNames,
                processAbi,
                allowLooseFilenameMatching,
                fileExists);

            string bestPath = null;
            int bestScore = 0;
            bool ambiguous = false;
            foreach (var candidate in candidates)
            {
                if (candidate.Value > bestScore)
                {
                    bestPath = candidate.Key;
                    bestScore = candidate.Value;
                    ambiguous = false;
                }
                else if (candidate.Value == bestScore && candidate.Value > 0)
                {
                    ambiguous = true;
                }
            }
            return ambiguous ? null : bestPath;
        }

        private static void CollectAbiSplitCandidates(
            List<KeyValuePair<string, int>> candidates,
            string[] splitPaths,
            string[] splitNames,
            string processAbi,
            bool allowLooseFilenameMatching,
            Func<string, bool> fileExists)
        {
            if (splitPaths == null)
            {
                return;
            }

            for (int index = 0; index < splitPaths.Length; index++)
            {
                string splitPath = splitPaths[index];
                if (string.IsNullOrEmpty(splitPath))
                {
                    continue;
                }

                string splitName = splitNames != null && index < splitNames.Length ? splitNames[index] : null;
                int score = GetAbiMatchScore(splitPath, splitName, processAbi, allowLooseFilenameMatching);
                if (score <= 0)
                {
                    continue;
                }

                if (!Path.IsPathRooted(splitPath) || !fileExists(splitPath))
                {
                    continue;
                }

                // Deduplicate by path: the same split commonly appears in both the private and
                // public arrays; keep the highest confidence seen for it.
                int existingIndex = -1;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    if (string.Equals(candidates[candidateIndex].Key, splitPath, StringComparison.Ordinal))
                    {
                        existingIndex = candidateIndex;
                        break;
                    }
                }
                if (existingIndex < 0)
                {
                    candidates.Add(new KeyValuePair<string, int>(splitPath, score));
                }
                else if (candidates[existingIndex].Value < score)
                {
                    candidates[existingIndex] = new KeyValuePair<string, int>(splitPath, score);
                }
            }
        }

        /// <summary>
        /// Match confidence for one split candidate. An exact config.&lt;abi&gt; split name is the strongest evidence, then the standard split_config.&lt;abi&gt;.apk filename.
        /// A loose ABI token in the filename is accepted only when split-name metadata is globally unavailable (pre-API-26 installs):
        /// a candidate that has a split name must match exactly or not at all, so a dynamic-feature ABI split is never mistaken for the base configuration split.
        /// </summary>
        internal static int GetAbiMatchScore(
            string splitPath,
            string splitName,
            string processAbi,
            bool allowLooseFilenameMatching)
        {
            if (string.IsNullOrEmpty(processAbi))
            {
                return 0;
            }

            string normalizedAbi = NormalizeAbiToken(processAbi);
            string normalizedSplitName = string.IsNullOrEmpty(splitName) ? null : NormalizeAbiToken(splitName);
            string normalizedFileName = NormalizeAbiToken(GetFileName(splitPath));

            if (("config." + normalizedAbi).Equals(normalizedSplitName, StringComparison.Ordinal))
            {
                return 300;
            }
            if (("split_config." + normalizedAbi + ".apk").Equals(normalizedFileName, StringComparison.Ordinal))
            {
                return 200;
            }
            if (allowLooseFilenameMatching
                && normalizedSplitName == null
                && ContainsAbiToken(normalizedFileName, normalizedAbi))
            {
                return 100;
            }
            return 0;
        }

        /// <summary>
        /// Bounded token search: the ABI must not continue into an adjacent token character, so "x86" never matches inside "x86_64".
        /// </summary>
        private static bool ContainsAbiToken(string normalizedValue, string normalizedAbi)
        {
            if (string.IsNullOrEmpty(normalizedValue) || string.IsNullOrEmpty(normalizedAbi))
            {
                return false;
            }

            int startIndex = 0;
            while (startIndex < normalizedValue.Length)
            {
                int matchIndex = normalizedValue.IndexOf(normalizedAbi, startIndex, StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    return false;
                }

                int endIndex = matchIndex + normalizedAbi.Length;
                bool validStart = matchIndex == 0 || !IsAbiTokenCharacter(normalizedValue[matchIndex - 1]);
                bool validEnd = endIndex == normalizedValue.Length || !IsAbiTokenCharacter(normalizedValue[endIndex]);
                if (validStart && validEnd)
                {
                    return true;
                }
                startIndex = matchIndex + 1;
            }
            return false;
        }

        private static bool IsAbiTokenCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private static string NormalizeAbiToken(string value)
        {
            return value.ToLower(CultureInfo.InvariantCulture).Replace('-', '_');
        }

        private static string GetFileName(string path)
        {
            int separatorIndex = path.LastIndexOf('/');
            return separatorIndex < 0 ? path : path.Substring(separatorIndex + 1);
        }

        private static string FirstNonEmpty(string first, string second, string third)
        {
            if (!string.IsNullOrEmpty(first))
            {
                return first;
            }
            if (!string.IsNullOrEmpty(second))
            {
                return second;
            }
            return string.IsNullOrEmpty(third) ? null : third;
        }
    }
}
#endif
