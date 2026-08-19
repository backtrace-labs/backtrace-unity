#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    /// <summary>
    /// Crash-handler child environment:
    /// reserved variables appear exactly once and the SDK's values replace inherited definitions instead of being appended after them.
    /// </summary>
    public class AndroidCrashHandlerEnvironmentTests
    {
        private const string ClassPath = "/data/app/example/base.apk";
        private const string HandlerPath =
            "/data/app/example/split_config.arm64_v8a.apk!/lib/arm64-v8a/libbacktrace-native.so";

        private static string[] Build(IDictionary inherited)
        {
            return AndroidCrashHandlerEnvironment.BuildEnvironment(
                inherited, ClassPath, HandlerPath, new[] { "/lib/arm64", "/lib" });
        }

        private static int CountKey(string[] environment, string key)
        {
            return environment.Count(entry => entry.StartsWith(key + "=", StringComparison.Ordinal));
        }

        private static string ValueOf(string[] environment, string key)
        {
            var entry = environment.Single(item => item.StartsWith(key + "=", StringComparison.Ordinal));
            return entry.Substring(key.Length + 1);
        }

        [Test]
        public void ReservedVariablesAppearExactlyOnce()
        {
            var inherited = new Hashtable
            {
                { "CLASSPATH", "/inherited/classes.apk" },
                { "BACKTRACE_UNITY_CRASH_HANDLER", "/inherited/handler.so" },
                { "LD_LIBRARY_PATH", "/inherited/lib" },
                { "ANDROID_DATA", "/inherited/data" },
            };

            var environment = Build(inherited);

            foreach (var key in new[] { "CLASSPATH", "BACKTRACE_UNITY_CRASH_HANDLER", "LD_LIBRARY_PATH", "ANDROID_DATA" })
            {
                Assert.AreEqual(1, CountKey(environment, key), key + " must appear exactly once");
            }
        }

        [Test]
        public void SdkValuesReplaceInheritedValues()
        {
            var inherited = new Hashtable
            {
                { "CLASSPATH", "/inherited/classes.apk" },
                { "ANDROID_DATA", "/inherited/data" },
            };

            var environment = Build(inherited);

            Assert.AreEqual(ClassPath, ValueOf(environment, "CLASSPATH"));
            Assert.AreEqual(HandlerPath, ValueOf(environment, "BACKTRACE_UNITY_CRASH_HANDLER"));
            Assert.AreEqual("/data", ValueOf(environment, "ANDROID_DATA"));
        }

        [Test]
        public void UnrelatedInheritedVariablesAreRetained()
        {
            var inherited = new Hashtable { { "PATH", "/system/bin" }, { "HOME", "/data/user/0" } };

            var environment = Build(inherited);

            Assert.AreEqual("/system/bin", ValueOf(environment, "PATH"));
            Assert.AreEqual("/data/user/0", ValueOf(environment, "HOME"));
        }

        [Test]
        public void ValuesContainingEqualsArePreserved()
        {
            var inherited = new Hashtable { { "JAVA_OPTS", "-Da=b -Dc=d" } };

            var environment = Build(inherited);

            Assert.AreEqual("-Da=b -Dc=d", ValueOf(environment, "JAVA_OPTS"));
        }

        [Test]
        public void NullInheritedEnvironmentAndNullValuesDoNotThrow()
        {
            Assert.DoesNotThrow(() => Build(null));

            var inherited = new Hashtable { { "EMPTY", null } };
            var environment = Build(inherited);
            Assert.AreEqual(string.Empty, ValueOf(environment, "EMPTY"));
        }

        [Test]
        public void SearchPathsAreDeduplicatedAndNullEntriesSkipped()
        {
            var environment = AndroidCrashHandlerEnvironment.BuildEnvironment(
                null, ClassPath, HandlerPath, new[] { "/lib/arm64", null, "/lib/arm64", "", "/lib" });

            Assert.AreEqual("/lib/arm64:/lib", ValueOf(environment, "LD_LIBRARY_PATH"));
        }

        [Test]
        public void NullSearchPathListDoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => AndroidCrashHandlerEnvironment.BuildEnvironment(null, ClassPath, HandlerPath, null));
        }

        [Test]
        public void OptionalSearchPathFailuresKeepUsableFallbacks()
        {
            var searchPaths = AndroidCrashHandlerEnvironment.BuildLibrarySearchPaths(
                "/data/app/example/lib/arm64",
                path => { throw new InvalidOperationException("parent lookup failed"); },
                () => { throw new InvalidOperationException("java.library.path lookup failed"); });

            CollectionAssert.AreEqual(
                new[] { "/data/app/example/lib/arm64", "/data/local" },
                searchPaths);
        }
    }
}
#endif
