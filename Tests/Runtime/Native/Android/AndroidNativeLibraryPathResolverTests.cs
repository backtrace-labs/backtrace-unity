#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    /// <summary>
    /// Pure resolver policy tests: linker-path validation, extracted-library precedence, split scoring with global ambiguity rejection, and the base-APK fallback.
    /// File existence is injected, so no fixture files are required.
    /// </summary>
    public class AndroidNativeLibraryPathResolverTests
    {
        private const string Library = "libbacktrace-native.so";
        private const string BaseApk = "/data/app/example/base.apk";
        private const string Arm64Split = "/data/app/example/split_config.arm64_v8a.apk";
        private const string DataPathFallback = "/data/app/example/base.apk";

        private static Func<string, bool> Exists(params string[] paths)
        {
            var set = new HashSet<string>(paths, StringComparer.Ordinal);
            return set.Contains;
        }

        private static AndroidApplicationInfoSnapshot Snapshot()
        {
            return new AndroidApplicationInfoSnapshot { SourceDir = BaseApk };
        }

        [Test]
        public void AbsoluteLoadedLibraryPathIsAuthoritative()
        {
            var loaded = "/data/app/example/lib/arm64/" + Library;

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), loaded, "arm64-v8a", DataPathFallback, Exists(loaded, BaseApk));

            Assert.AreEqual(loaded, resolved);
        }

        [Test]
        public void ApkBackedLoadedLibraryPathIsAuthoritative()
        {
            var loaded = Arm64Split + "!/lib/arm64-v8a/" + Library;

            // The process ABI deliberately disagrees:
            // Android already proved what it loaded, so the linker answer must not be second-guessed with a C#-derived ABI.
            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), loaded, "armeabi-v7a", DataPathFallback, Exists(Arm64Split, BaseApk));

            Assert.AreEqual(loaded, resolved);
        }

        [Test]
        public void RelativeLoadedLibraryPathIsRejected()
        {
            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), "lib/arm64/" + Library, "arm64-v8a", DataPathFallback, Exists(BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void WrongLibraryNameIsRejected()
        {
            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(),
                "/data/app/example/lib/arm64/libother.so",
                "arm64-v8a",
                DataPathFallback,
                Exists("/data/app/example/lib/arm64/libother.so", BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void MissingOuterApkIsRejected()
        {
            var loaded = Arm64Split + "!/lib/arm64-v8a/" + Library;

            // The split container does not exist on disk, so the loaded path must be rejected.
            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), loaded, "arm64-v8a", DataPathFallback, Exists(BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void MalformedApkEntryIsRejected()
        {
            var loaded = Arm64Split + "!/lib/arm64-v8a/extra/" + Library;

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), loaded, "arm64-v8a", DataPathFallback, Exists(Arm64Split, BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void ExtractedLibraryWinsBeforeAbiLookup()
        {
            var snapshot = Snapshot();
            snapshot.NativeLibraryDir = "/data/app/example/lib/arm64";
            snapshot.SplitSourceDirs = new[] { Arm64Split };
            snapshot.SplitNames = new[] { "config.arm64_v8a" };
            var extracted = "/data/app/example/lib/arm64/" + Library;

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot, null, "arm64-v8a", DataPathFallback, Exists(extracted, Arm64Split, BaseApk));

            Assert.AreEqual(extracted, resolved);
        }

        [Test]
        public void AbiSplitSelectedByExactSplitName()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[] { "/data/app/example/split_feature.apk", Arm64Split };
            snapshot.SplitNames = new[] { "feature", "config.arm64_v8a" };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot,
                null,
                "arm64-v8a",
                DataPathFallback,
                Exists("/data/app/example/split_feature.apk", Arm64Split, BaseApk));

            Assert.AreEqual(Arm64Split + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void AbiSplitSelectedByExactFilenameWhenNamesMissing()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[] { Arm64Split };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot, null, "arm64-v8a", DataPathFallback, Exists(Arm64Split, BaseApk));

            Assert.AreEqual(Arm64Split + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void PublicExactSplitOutranksPrivateLooseSplit()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[] { "/data/app/example/feature.arm64_v8a.apk" };
            snapshot.SplitPublicSourceDirs = new[] { Arm64Split };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot,
                null,
                "arm64-v8a",
                DataPathFallback,
                Exists("/data/app/example/feature.arm64_v8a.apk", Arm64Split, BaseApk));

            Assert.AreEqual(Arm64Split + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void DuplicatePrivateAndPublicPathIsDeduplicated()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[] { Arm64Split };
            snapshot.SplitPublicSourceDirs = new[] { Arm64Split };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot, null, "arm64-v8a", DataPathFallback, Exists(Arm64Split, BaseApk));

            // The same path in both arrays is one candidate, not an ambiguous pair.
            Assert.AreEqual(Arm64Split + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void TwoEqualConfidenceCandidatesFallBackToBaseApk()
        {
            var snapshot = Snapshot();
            var otherSplit = "/data/app/example.two/split_config.arm64_v8a.apk";
            snapshot.SplitSourceDirs = new[] { Arm64Split, otherSplit };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot, null, "arm64-v8a", DataPathFallback, Exists(Arm64Split, otherSplit, BaseApk));

            // Two distinct candidates of equal confidence cannot be told apart without opening the archives;
            // ambiguity falls back instead of picking by array order.
            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void TruncatedSplitNamesCannotReEnableLooseMatching()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[] { "/data/app/example/split_config.xxhdpi.apk", "/data/app/example/feature.arm64_v8a.apk" };
            // splitNames exists but is shorter than splitSourceDirs:
            // the unnamed candidate must not regain the low-confidence loose score that named candidates correctly lose.
            snapshot.SplitNames = new[] { "config.xxhdpi" };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot,
                null,
                "arm64-v8a",
                DataPathFallback,
                Exists("/data/app/example/split_config.xxhdpi.apk", "/data/app/example/feature.arm64_v8a.apk", BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void X86DoesNotMatchX8664()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[] { "/data/app/example/split_config.x86_64.apk" };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot,
                null,
                "x86",
                DataPathFallback,
                Exists("/data/app/example/split_config.x86_64.apk", BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/x86/" + Library, resolved);
        }

        [Test]
        public void LanguageAndDensitySplitsAreIgnored()
        {
            var snapshot = Snapshot();
            snapshot.SplitSourceDirs = new[]
            {
                "/data/app/example/split_config.en.apk",
                "/data/app/example/split_config.xxhdpi.apk",
                Arm64Split
            };

            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                snapshot,
                null,
                "arm64-v8a",
                DataPathFallback,
                Exists(
                    "/data/app/example/split_config.en.apk",
                    "/data/app/example/split_config.xxhdpi.apk",
                    Arm64Split,
                    BaseApk));

            Assert.AreEqual(Arm64Split + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void BaseApkFallbackRemainsStable()
        {
            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), null, "arm64-v8a", DataPathFallback, Exists(BaseApk));

            Assert.AreEqual(BaseApk + "!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void DataPathFallbackIsUsedWhenApplicationInfoIsEmpty()
        {
            var resolved = AndroidNativeLibraryPathResolver.Resolve(
                new AndroidApplicationInfoSnapshot(),
                null,
                "arm64-v8a",
                "/data/app/fallback/base.apk",
                Exists("/data/app/fallback/base.apk"));

            Assert.AreEqual("/data/app/fallback/base.apk!/lib/arm64-v8a/" + Library, resolved);
        }

        [Test]
        public void DirectoryGuessSelectsArm64DirectoryForArm64Process()
        {
            var selected = AndroidNativeLibraryPathResolver.SelectNativeLibraryDirectory(
                new[] { "/data/app/example/lib/arm", "/data/app/example/lib/arm64" }, "arm64-v8a");

            Assert.AreEqual("/data/app/example/lib/arm64", selected);
        }

        [Test]
        public void DirectoryGuessSelectsArmDirectoryForArmV7Process()
        {
            var selected = AndroidNativeLibraryPathResolver.SelectNativeLibraryDirectory(
                new[] { "/data/app/example/lib/arm64", "/data/app/example/lib/arm" }, "armeabi-v7a");

            Assert.AreEqual("/data/app/example/lib/arm", selected);
        }

        [Test]
        public void DirectoryGuessNeverSelectsX8664ForX86Process()
        {
            var selected = AndroidNativeLibraryPathResolver.SelectNativeLibraryDirectory(
                new[] { "/data/app/example/lib/x86_64", "/data/app/example/lib/other" }, "x86");

            // No exact match and more than one candidate: no guess, resolution continues.
            Assert.IsNull(selected);
        }

        [Test]
        public void DirectoryGuessAmbiguousWithoutExactMatchReturnsNoGuess()
        {
            var selected = AndroidNativeLibraryPathResolver.SelectNativeLibraryDirectory(
                new[] { "/data/app/example/lib/a", "/data/app/example/lib/b" }, "arm64-v8a");

            Assert.IsNull(selected);
        }

        [Test]
        public void DirectoryGuessSingleCandidateRemainsACompatibilityFallback()
        {
            var selected = AndroidNativeLibraryPathResolver.SelectNativeLibraryDirectory(
                new[] { "/data/app/example/lib/somedir" }, "arm64-v8a");

            Assert.AreEqual("/data/app/example/lib/somedir", selected);

            // Even with an undetermined ABI, one unique directory is still usable.
            Assert.AreEqual(
                "/data/app/example/lib/somedir",
                AndroidNativeLibraryPathResolver.SelectNativeLibraryDirectory(
                    new[] { "/data/app/example/lib/somedir" }, null));
        }

        [Test]
        public void MissingAbiFailsOnlyWhenTheFallbackNeedsIt()
        {
            var loaded = "/data/app/example/lib/arm64/" + Library;

            // With a valid linker path the ABI is never consulted.
            Assert.DoesNotThrow(() => AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), loaded, null, DataPathFallback, Exists(loaded, BaseApk)));

            // Without one, the metadata fallback cannot construct a path.
            Assert.Throws<InvalidOperationException>(() => AndroidNativeLibraryPathResolver.Resolve(
                Snapshot(), null, null, DataPathFallback, Exists(BaseApk)));
        }
    }
}
#endif
