#if UNITY_EDITOR || UNITY_STANDALONE_OSX
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Backtrace.Unity.Model;
using Backtrace.Unity.Runtime.Native;
using Backtrace.Unity.Runtime.Native.OSX;
using Backtrace.Unity.Runtime.Native.Apple;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class MacNativeClientTests
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        // Run in a fresh standalone test player.
        // The native handler and its cooperative lease intentionally remain installed until process exit.
        [UnityTest]
        [Explicit("Requires a fresh standalone player; run this test alone by its exact name.")]
        public IEnumerator PublicRefreshAllowsFirstCaptureAfterInitiallyDisabled()
        {
            var config = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            BacktraceClient client = null;
            try
            {
                Assert.IsNull(BacktraceClient.Instance, "This fixture requires a fresh player.");
                config.ServerUrl = "https://example.invalid/post?format=json";
                config.DestroyOnLoad = true;
                config.Enabled = true;
                config.DatabasePath = Path.Combine(Application.temporaryCachePath,
                    "backtrace-capture-toggle-" + Guid.NewGuid().ToString("N"));
                config.AutoSendMode = false;
                config.CaptureNativeCrashes = false;
                config.HandleANR = false;
                config.OomReports = false;
                config.EnableMetricsSupport = false;
                config.HandleUnhandledExceptions = false;
                config.ReportPerMin = 0;

                client = BacktraceClient.Initialize(config);
                Assert.IsNotNull(client.Database, "A managed database must not trigger disabled native capture.");
                Assert.IsNull(client.NativeClient, "No native wrapper should be created while capture is disabled.");
                client.Refresh();
                Assert.IsNull(client.NativeClient);

                config.CaptureNativeCrashes = true;
                client.Refresh();
                var owner = client.NativeClient;
                Assert.IsNotNull(owner);
                Assert.IsTrue(owner.OnOOM(), "The first enabled Refresh must activate the real native bridge.");
                string marker = Guid.NewGuid().ToString("N");
                client.SetAttribute("test.capture.toggle", marker);
                var attributes = new Dictionary<string, string>();
                owner.GetAttributes(attributes);
                Assert.AreEqual(marker, attributes["test.capture.toggle"]);

                client.Refresh();
                Assert.AreSame(owner, client.NativeClient, "Refresh must retain the active native owner.");
                Assert.IsTrue(owner.OnOOM());

                // Observe the existing destruction path without changing factory/session code or substituting the real native activation.
                var counter = new DisableCountingClient(owner);
                client.NativeClient = counter;
                UnityEngine.Object.Destroy(client.gameObject);
                yield return null;
                Assert.AreEqual(1, counter.DisableCalls);
                Assert.IsFalse(owner.OnOOM());
            }
            finally
            {
                if (client != null) UnityEngine.Object.DestroyImmediate(client.gameObject);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private sealed class DisableCountingClient : INativeClient
        {
            private readonly INativeClient inner;
            internal int DisableCalls;
            internal DisableCountingClient(INativeClient inner) { this.inner = inner; }
            public void GetAttributes(IDictionary<string, string> attributes) { inner.GetAttributes(attributes); }
            public void HandleAnr() { inner.HandleAnr(); }
            public void SetAttribute(string key, string value) { inner.SetAttribute(key, value); }
            public bool OnOOM() { return inner.OnOOM(); }
            public void Update(float time) { inner.Update(time); }
            public void Disable() { DisableCalls++; inner.Disable(); }
            public void PauseAnrThread(bool state) { inner.PauseAnrThread(state); }
        }
#endif
        private sealed class Lease : IDisposable { public void Dispose() { } }
        private sealed class Bridge : IAppleNativeBridge
        {
            internal int Starts, Stops;
            internal Action OnReport;
            internal Action OnDisable;
            public int Version() { return 3; }
            public int Start(AppleNativeRequest request) { Starts++; return 0; }
            public void GetAttributes(out IntPtr p, out int n) { p = IntPtr.Zero; n = 0; }
            public void FreeAttributes(IntPtr p, int n) { }
            public void AddAttribute(string key, string value) { }
            public void Report(string text, bool main, bool debugger)
            { if (OnReport != null) OnReport(); }
            public void Disable()
            { Interlocked.Increment(ref Stops); if (OnDisable != null) OnDisable(); }
        }
        private static AppleNativeRequest Request()
        {
            return new AppleNativeRequest("https://example.invalid/plcrash", null, null,
                false, false, 0, "/tmp/backtrace-unity-wrapper-test");
        }
        [Test]
        public void ManagedDatabaseDisabledDoesNotBlockInjectedNativeStart()
        {
            var config = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var bridge = new Bridge();
            NativeClient client = null;
            try
            {
                config.CaptureNativeCrashes = true; config.Enabled = false; config.HandleANR = false;
                client = new NativeClient(config, new AppleNativeSession(bridge, null), Request(), () => new Lease());
                Assert.AreEqual(1, bridge.Starts); Assert.IsTrue(client.OnOOM());
                ((INativeClient)client).Update(42f);
                Assert.AreEqual(42f, client.LastUpdateTime);
                client.Disable(); client.Disable();
                Assert.IsFalse(client.OnOOM()); Assert.AreEqual(1, bridge.Stops);
            }
            finally { if (client != null) client.Disable(); UnityEngine.Object.DestroyImmediate(config); }
        }
        [Test]
        public void CaptureDisabledDoesNotEnterBridge()
        {
            var config = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var bridge = new Bridge();
            try
            {
                config.CaptureNativeCrashes = false;
                var client = new NativeClient(config, new AppleNativeSession(bridge, null), Request(), () => new Lease());
                Assert.AreEqual(0, bridge.Starts); Assert.IsFalse(client.OnOOM());
                client.Disable(); Assert.AreEqual(0, bridge.Stops);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
        [Test]
        public void DuplicateWrapperCannotStopTheOriginalOwner()
        {
            var config = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null);
            NativeClient first = null;
            try
            {
                config.CaptureNativeCrashes = true; config.HandleANR = false;
                first = new NativeClient(config, session, Request(), () => new Lease());
                var second = new NativeClient(config, session, Request(), () => new Lease());
                second.HandleAnr();
                Assert.IsNull(second.AnrThread, "A duplicate must not start an ANR worker.");
                Assert.IsFalse(second.OnOOM()); second.Disable();
                Assert.IsTrue(first.OnOOM()); Assert.AreEqual(0, bridge.Stops);
            }
            finally { if (first != null) first.Disable(); UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void InterfaceUpdateDispatchesToMacHeartbeatAndBaseBehavior()
        {
            // LastUpdateTime alone cannot catch accidental base-class dispatch: the
            // base implementation updates that field without refreshing the watchdog.
            InterfaceMapping map = typeof(NativeClient).GetInterfaceMap(typeof(INativeClient));
            int index = Array.FindIndex(map.InterfaceMethods, method => method.Name == "Update");
            Assert.GreaterOrEqual(index, 0);
            Assert.AreEqual(typeof(AppleNativeClient), map.TargetMethods[index].DeclaringType);
        }

        [Test]
        public void DisableDoesNotWaitForBlockedAnrReport()
        {
            var config = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var reportEntered = new ManualResetEvent(false);
            var releaseReport = new ManualResetEvent(false);
            var disableReturned = new ManualResetEvent(false);
            var nativeDisabled = new ManualResetEvent(false);
            var bridge = new Bridge
            {
                OnReport = () => { reportEntered.Set(); releaseReport.WaitOne(); },
                OnDisable = () => nativeDisabled.Set()
            };
            NativeClient client = null;
            Thread stopThread = null;
            try
            {
                config.CaptureNativeCrashes = true;
                config.HandleANR = false;
                config.AnrWatchdogTimeout = 1001;
                client = new NativeClient(config, new AppleNativeSession(bridge, null), Request(), () => new Lease());
                client.HandleAnr();
                Thread worker = client.AnrThread;
                Assert.IsNotNull(worker);
                client.HandleAnr();
                Assert.AreSame(worker, client.AnrThread, "Only one watchdog may own the session.");
                Assert.IsTrue(reportEntered.WaitOne(10000), "ANR worker did not enter the blocked report.");

                stopThread = new Thread(() => { client.Disable(); disableReturned.Set(); });
                stopThread.IsBackground = true;
                stopThread.Start();
                Assert.IsTrue(disableReturned.WaitOne(5000), "Disable joined an in-flight native report.");
                Assert.IsFalse(client.OnOOM());
                Assert.AreEqual(0, Volatile.Read(ref bridge.Stops), "Native cleanup must wait for the admitted report.");

                releaseReport.Set();
                Assert.IsTrue(nativeDisabled.WaitOne(5000), "Final report completion did not disable native capture.");
                Assert.IsTrue(worker.Join(5000), "The stopped watchdog did not exit after its report returned.");
                client.Disable();
                Assert.AreEqual(1, Volatile.Read(ref bridge.Stops));
            }
            finally
            {
                releaseReport.Set();
                if (client != null)
                {
                    client.Disable();
                    if (client.AnrThread != null) client.AnrThread.Join(5000);
                }
                if (stopThread != null) stopThread.Join(5000);
                reportEntered.Close(); releaseReport.Close(); disableReturned.Close(); nativeDisabled.Close();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
#endif
