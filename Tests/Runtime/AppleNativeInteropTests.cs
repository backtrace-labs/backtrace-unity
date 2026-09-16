#if UNITY_EDITOR || UNITY_IOS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Backtrace.Unity.Model;
using Backtrace.Unity.Runtime.Native;
using Backtrace.Unity.Runtime.Native.Apple;
using NUnit.Framework;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public sealed class AppleNativeInteropTests
    {
        private sealed class Lease : IDisposable { public void Dispose() { } }
        private sealed class Bridge : IAppleNativeBridge
        {
            internal AppleNativeRequest Request;
            internal int Stops;
            internal int Result;
            public int Version() { return 3; }
            public int Start(AppleNativeRequest request) { Request = request; return Result; }
            public void GetAttributes(out IntPtr entries, out int count) { entries = IntPtr.Zero; count = 0; }
            public void FreeAttributes(IntPtr entries, int count) { }
            public void AddAttribute(string key, string value) { }
            public void Report(string message, bool mainThread, bool ignoreDebugger) { }
            public void Disable() { ++Stops; }
        }

        [Test]
        public void IOSPreservesDefaultStoreAndPassesUnlimitedRate()
        {
            var request = AppleNativeRequest.ForIOS("https://example.invalid/plcrash",
                new Dictionary<string, string> { { "unicode", "日本語 — 😀" } },
                new[] { "/tmp/log.txt" }, true, false, 0);
            Assert.IsNull(request.BasePath);
            Assert.AreEqual(0, request.ReportsPerMinute);
            Assert.Contains("unicode", request.Keys);
            Assert.Contains("日本語 — 😀", request.Values);
        }

        [Test]
        public void MacDoesNotPermitImplicitDefaultStorage()
        {
            Assert.Throws<ArgumentException>(() => new AppleNativeRequest(
                "https://example.invalid/plcrash", null, null, false, false, 0, string.Empty));
            Assert.Throws<ArgumentException>(() => new AppleNativeRequest(
                "https://example.invalid/plcrash", null, null, false, false, 0, "relative"));
        }

        [Test]
        public void IOSWrapperUsesSharedOwnershipWithoutManagedDatabase()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.Enabled = false;
            configuration.CaptureNativeCrashes = true;
            configuration.HandleANR = false;
            var bridge = new Bridge();
            var session = new AppleNativeSession(bridge, _ => { });
            var request = AppleNativeRequest.ForIOS("https://example.invalid/plcrash", null, null, false, false, 17);
            Backtrace.Unity.Runtime.Native.iOS.NativeClient client = null;
            try
            {
                client = new Backtrace.Unity.Runtime.Native.iOS.NativeClient(configuration, session, request, () => new Lease());
                Assert.IsTrue(client.OnOOM());
                Assert.AreEqual(17, bridge.Request.ReportsPerMinute);
                client.Disable();
                client.Disable();
                Assert.AreEqual(1, bridge.Stops);
                Assert.IsFalse(client.OnOOM());
            }
            finally
            {
                if (client != null) client.Disable();
                UnityEngine.Object.DestroyImmediate(configuration);
            }
        }

        [Test]
        public void IOSInterfaceDispatchesToSharedHeartbeat()
        {
            InterfaceMapping map = typeof(Backtrace.Unity.Runtime.Native.iOS.NativeClient).GetInterfaceMap(typeof(INativeClient));
            for (int i = 0; i < map.InterfaceMethods.Length; ++i)
                if (map.InterfaceMethods[i].Name == "Update")
                {
                    Assert.AreEqual(typeof(AppleNativeClient), map.TargetMethods[i].DeclaringType);
                    return;
                }
            Assert.Fail("INativeClient.Update was not mapped.");
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void IOSFailureDoesNotBecomeActive(int code)
        {
            var bridge = new Bridge { Result = code };
            var session = new AppleNativeSession(bridge, _ => { });
            var token = new object();
            Assert.IsFalse(session.Start(token, AppleNativeRequest.ForIOS(
                "https://example.invalid/plcrash", null, null, false, false, 0), () => new Lease()));
            Assert.IsFalse(session.IsActive(token));
        }
    }
}
#endif
