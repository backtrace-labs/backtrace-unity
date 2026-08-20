#if UNITY_ANDROID || UNITY_EDITOR
using System;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    /// <summary>
    /// Tests the optional linker-path provider boundary without loading libdl in the editor.
    /// </summary>
    public class AndroidLoadedLibraryPathTests
    {
        [Test]
        public void ProviderPathIsPreserved()
        {
            const string expected = "/data/app/example/lib/arm64/libbacktrace-native.so";

            string result = AndroidLoadedLibraryPath.TryGet(() => expected);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EmptyProviderPathReturnsNull()
        {
            Assert.IsNull(AndroidLoadedLibraryPath.TryGet(() => string.Empty));
        }

        [Test]
        public void UnexpectedProviderExceptionReturnsNull()
        {
            string result = AndroidLoadedLibraryPath.TryGet(
                () => { throw new InvalidOperationException("provider failed"); });

            Assert.IsNull(result);
        }
    }
}
#endif
