#if UNITY_ANDROID
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    /// <summary>
    /// The native exports are: InitializeJavaCrashHandler -> bool; AddAttribute, DumpWithoutCrash, and Disable -> void.
    /// A P/Invoke declaration that disagrees reads undefined register contents, so the private declarations are pinned by reflection.
    /// Compiled only for the Android build target, where the Android NativeClient exists.
    /// </summary>
    public class AndroidNativeInteropSignatureTests
    {
        private static MethodInfo FindPInvoke(string entryPoint)
        {
            var nativeClient = typeof(Backtrace.Unity.BacktraceClient).Assembly
                .GetType("Backtrace.Unity.Runtime.Native.Android.NativeClient");
            Assert.IsNotNull(nativeClient, "Android NativeClient type must exist");

            var method = nativeClient
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .SingleOrDefault(candidate =>
                {
                    var import = candidate.GetCustomAttributes(typeof(System.Runtime.InteropServices.DllImportAttribute), false)
                        .Cast<System.Runtime.InteropServices.DllImportAttribute>()
                        .FirstOrDefault();
                    return import != null && import.EntryPoint == entryPoint;
                });
            Assert.IsNotNull(method, "P/Invoke declaration for " + entryPoint + " must exist");
            return method;
        }

        [Test]
        public void InitializeJavaCrashHandlerReturnsBool()
        {
            Assert.AreEqual(typeof(bool), FindPInvoke("InitializeJavaCrashHandler").ReturnType);
        }

        [Test]
        public void AddAttributeReturnsVoid()
        {
            Assert.AreEqual(typeof(void), FindPInvoke("AddAttribute").ReturnType);
        }

        [Test]
        public void DumpWithoutCrashReturnsVoid()
        {
            Assert.AreEqual(typeof(void), FindPInvoke("DumpWithoutCrash").ReturnType);
        }

        [Test]
        public void DisableReturnsVoid()
        {
            Assert.AreEqual(typeof(void), FindPInvoke("Disable").ReturnType);
        }
    }
}
#endif
