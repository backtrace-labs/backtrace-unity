#if UNITY_ANDROID || UNITY_EDITOR
using System;
#if UNITY_ANDROID
using UnityEngine;
#endif

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Resolves the ABI of the CURRENT PROCESS, not the device's preferred ABI:
    /// Build.SUPPORTED_ABIS is ordered by device preference, so a 32-bit Unity process on a 64-bit-capable device would wrongly report arm64-v8a from SUPPORTED_ABIS[0].
    /// Build.CPU_ABI is adjusted by Android for the bitness of the running process, which makes it the correct source for a value used to locate this process's native libraries.
    /// </summary>
    internal static class AndroidProcessAbi
    {
        private const string UnknownAbi = "unknown";

        /// <summary>
        /// Pure selection seam. Order:
        /// process ABI (Build.CPU_ABI), then the bitness-matching supported list (API 23+, via Process.is64Bit()), then the device-ordered list, then fail as undetermined.
        /// </summary>
        internal static string Select(
            string processAbi,
            bool? is64Bit,
            string[] supported32BitAbis,
            string[] supported64BitAbis,
            string[] supportedAbis)
        {
            string normalized = Normalize(processAbi);
            if (normalized != null)
            {
                return normalized;
            }

            if (is64Bit.HasValue)
            {
                normalized = FirstValid(is64Bit.Value ? supported64BitAbis : supported32BitAbis);
                if (normalized != null)
                {
                    return normalized;
                }
            }

            normalized = FirstValid(supportedAbis);
            if (normalized != null)
            {
                return normalized;
            }

            throw new InvalidOperationException("Unable to determine the current process ABI.");
        }

        /// <summary>
        /// x86 (32-bit) native crash capture is unsupported, matching the Backtrace Android SDK policy;
        /// managed reporting continues on those devices.
        /// </summary>
        internal static bool SupportsNativeCrashCapture(string abi)
        {
            return !string.IsNullOrEmpty(abi) && !"x86".Equals(abi, StringComparison.Ordinal);
        }

        private static string FirstValid(string[] abis)
        {
            if (abis == null)
            {
                return null;
            }
            for (int index = 0; index < abis.Length; index++)
            {
                string normalized = Normalize(abis[index]);
                if (normalized != null)
                {
                    return normalized;
                }
            }
            return null;
        }

        private static string Normalize(string abi)
        {
            if (abi == null)
            {
                return null;
            }
            string normalized = abi.Trim();
            if (normalized.Length == 0 || UnknownAbi.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return normalized;
        }

#if UNITY_ANDROID
        /// <summary>
        /// Reads the process ABI from the platform.
        /// Throws InvalidOperationException when the ABI cannot be determined; the caller contains that failure.
        /// </summary>
        internal static string Capture()
        {
            using (var build = new AndroidJavaClass("android.os.Build"))
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                string processAbi = TryGetStaticString(build, "CPU_ABI");
                bool? is64Bit = null;
                string[] supported32 = null;
                string[] supported64 = null;
                if (version.GetStatic<int>("SDK_INT") >= 23)
                {
                    using (var process = new AndroidJavaClass("android.os.Process"))
                    {
                        is64Bit = process.CallStatic<bool>("is64Bit");
                    }
                    supported32 = TryGetStaticStringArray(build, "SUPPORTED_32_BIT_ABIS");
                    supported64 = TryGetStaticStringArray(build, "SUPPORTED_64_BIT_ABIS");
                }
                string[] supportedAbis = TryGetStaticStringArray(build, "SUPPORTED_ABIS");
                return Select(processAbi, is64Bit, supported32, supported64, supportedAbis);
            }
        }

        private static string TryGetStaticString(AndroidJavaClass javaClass, string fieldName)
        {
            try
            {
                return javaClass.GetStatic<string>(fieldName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string[] TryGetStaticStringArray(AndroidJavaClass javaClass, string fieldName)
        {
            try
            {
                return javaClass.GetStatic<string[]>(fieldName);
            }
            catch (Exception)
            {
                return null;
            }
        }
#endif
    }
}
#endif
