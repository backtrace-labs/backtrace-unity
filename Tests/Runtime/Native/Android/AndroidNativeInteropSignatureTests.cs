#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    /// <summary>
    /// The native exports are: InitializeJavaCrashHandler -> bool; AddAttribute, DumpWithoutCrash, and Disable -> void.
    /// A P/Invoke declaration that disagrees reads undefined register contents, so the declarations are pinned by reflection.
    /// AndroidNativeInterop compiles in the Editor (without ever being invoked there)
    /// This suite executes in the in-editor PlayMode CI lane on every PR instead of requiring an Android player.
    /// (It lives in the Backtrace.Unity.Tests.Runtime assembly, which Unity classifies as a PlayMode test assembly; the editmode lane compiles it but does not run it.)
    /// </summary>
    public class AndroidNativeInteropSignatureTests
    {
        private static MethodInfo FindPInvoke(string entryPoint)
        {
            var method = typeof(AndroidNativeInterop)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(candidate =>
                {
                    var import = candidate.GetCustomAttributes(typeof(DllImportAttribute), false)
                        .Cast<DllImportAttribute>()
                        .FirstOrDefault();
                    return import != null && import.EntryPoint == entryPoint;
                });
            Assert.IsNotNull(method, "P/Invoke declaration for " + entryPoint + " must exist");
            return method;
        }

        [Test]
        public void InitializeJavaCrashHandlerReturnsBool()
        {
            var method = FindPInvoke("InitializeJavaCrashHandler");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            // Native C++ bool must marshal as one byte; the default marshalling is four bytes.
            var marshal = method.ReturnParameter
                .GetCustomAttributes(typeof(MarshalAsAttribute), false)
                .Cast<MarshalAsAttribute>()
                .FirstOrDefault();
            Assert.IsNotNull(marshal, "bool return must declare explicit marshalling");
            Assert.AreEqual(UnmanagedType.I1, marshal.Value);
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

        [Test]
        public void AllDeclarationsUseCdecl()
        {
            foreach (var entryPoint in new[] { "InitializeJavaCrashHandler", "AddAttribute", "DumpWithoutCrash", "Disable" })
            {
                var import = FindPInvoke(entryPoint)
                    .GetCustomAttributes(typeof(DllImportAttribute), false)
                    .Cast<DllImportAttribute>()
                    .First();
                Assert.AreEqual(CallingConvention.Cdecl, import.CallingConvention, entryPoint);
            }
        }
    }
}
#endif
