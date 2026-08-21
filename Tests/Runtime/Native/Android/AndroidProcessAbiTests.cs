#if UNITY_ANDROID || UNITY_EDITOR
using System;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    /// <summary>
    /// Process-ABI selection policy: the ABI of the running process wins;
    /// the device-preferred list is only a defensive fallback, preserving process bitness when available.
    /// </summary>
    public class AndroidProcessAbiTests
    {
        [Test]
        public void ProcessAbiWins()
        {
            var abi = AndroidProcessAbi.Select(
                "armeabi-v7a",
                true,
                new[] { "armeabi-v7a" },
                new[] { "arm64-v8a" },
                new[] { "arm64-v8a", "armeabi-v7a" });

            Assert.AreEqual("armeabi-v7a", abi);
        }

        [Test]
        public void ThirtyTwoBitProcessOnSixtyFourBitDeviceSelectsArmV7()
        {
            var abi = AndroidProcessAbi.Select(
                null,
                false,
                new[] { "armeabi-v7a", "armeabi" },
                new[] { "arm64-v8a" },
                new[] { "arm64-v8a", "armeabi-v7a" });

            Assert.AreEqual("armeabi-v7a", abi);
        }

        [Test]
        public void SixtyFourBitProcessSelectsArm64()
        {
            var abi = AndroidProcessAbi.Select(
                string.Empty,
                true,
                new[] { "armeabi-v7a" },
                new[] { "arm64-v8a" },
                new[] { "armeabi-v7a", "arm64-v8a" });

            Assert.AreEqual("arm64-v8a", abi);
        }

        [Test]
        public void UnknownIsRejected()
        {
            var abi = AndroidProcessAbi.Select(
                "unknown",
                null,
                null,
                null,
                new[] { "unknown", "arm64-v8a" });

            Assert.AreEqual("arm64-v8a", abi);
        }

        [Test]
        public void SupportedAbisIsTheLastFallback()
        {
            var abi = AndroidProcessAbi.Select(null, null, null, null, new[] { "x86_64" });

            Assert.AreEqual("x86_64", abi);
        }

        [Test]
        public void EmptyInputsFailDeterministically()
        {
            Assert.Throws<InvalidOperationException>(
                () => AndroidProcessAbi.Select(" ", null, new string[0], new string[0], new string[0]));
            Assert.Throws<InvalidOperationException>(
                () => AndroidProcessAbi.Select(null, true, null, new[] { "unknown", "" }, null));
        }

        [Test]
        public void X86IsKnownButNativeCaptureIsDisabled()
        {
            var abi = AndroidProcessAbi.Select("x86", false, null, null, null);

            Assert.AreEqual("x86", abi);
            Assert.IsFalse(AndroidProcessAbi.SupportsNativeCrashCapture(abi));
            Assert.IsTrue(AndroidProcessAbi.SupportsNativeCrashCapture("x86_64"));
            Assert.IsTrue(AndroidProcessAbi.SupportsNativeCrashCapture("arm64-v8a"));
            Assert.IsTrue(AndroidProcessAbi.SupportsNativeCrashCapture("armeabi-v7a"));
        }
    }
}
#endif
