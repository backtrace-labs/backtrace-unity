#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Builds the crash-handler child-process environment without duplicate keys:
    /// the inherited process environment is copied first, then the SDK-reserved variables REPLACE any inherited definition.
    /// Appending the inherited environment after the reserved values (the historical behavior) could define;
    /// CLASSPATH, BACKTRACE_UNITY_CRASH_HANDLER, LD_LIBRARY_PATH, or ANDROID_DATA twice, and which definition wins would then depend on the consumer.
    /// </summary>
    internal static class AndroidCrashHandlerEnvironment
    {
        internal const string CrashHandlerVariable = "BACKTRACE_UNITY_CRASH_HANDLER";

        /// <summary>
        /// CLASSPATH must remain the base APK containing the Java crash-handler classes;
        /// the native handler path may point at an ABI split.
        /// </summary>
        internal static string[] BuildEnvironment(
            IDictionary inheritedEnvironment,
            string classPath,
            string handlerPath,
            IEnumerable<string> librarySearchPaths)
        {
            var environment = new Dictionary<string, string>(StringComparer.Ordinal);
            if (inheritedEnvironment != null)
            {
                foreach (DictionaryEntry item in inheritedEnvironment)
                {
                    string key = Convert.ToString(item.Key, CultureInfo.InvariantCulture);
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }
                    environment[key] = item.Value == null
                        ? string.Empty
                        : Convert.ToString(item.Value, CultureInfo.InvariantCulture);
                }
            }

            environment["CLASSPATH"] = classPath;
            environment[CrashHandlerVariable] = handlerPath;
            environment["LD_LIBRARY_PATH"] = string.Join(
                ":",
                (librarySearchPaths ?? Enumerable.Empty<string>())
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct()
                    .ToArray());
            environment["ANDROID_DATA"] = "/data";

            return environment
                .Select(item => item.Key + "=" + item.Value)
                .ToArray();
        }
    }
}
#endif
