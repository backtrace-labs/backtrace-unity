#if UNITY_EDITOR || BACKTRACE_STANDALONE_TEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Backtrace.Unity.Runtime.Native.OSX;
using Backtrace.Unity.Runtime.Native.Apple;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class MacNativeSessionTests
    {
        private static AppleNativeRequest Request(IDictionary<string, string> values = null)
        { return new AppleNativeRequest("https://example.invalid/u/token/plcrash", values,
            new[] { "/tmp/é.log", "/tmp/é.log" }, true, false, 0, "/tmp/private-native"); }
        private sealed class Lease : IDisposable
        { internal bool Disposed; public void Dispose() { Disposed = true; } }
        private sealed class Bridge : IAppleNativeBridge
        {
            internal int Abi = 3, Result, Starts, Stops, Frees, Reports;
            internal Action OnStart, OnGet, OnReport;
            internal bool ThrowReport, ThrowStart, ThrowFree, InvalidUtf8Output;
            internal AppleNativeRequest Received;
            internal readonly List<string> Writes = new List<string>();
            internal readonly List<string> Order = new List<string>();
            private Utf8Memory memory;
            public int Version() { return Abi; }
            public int Start(AppleNativeRequest request)
            { Starts++; Received = request; if (OnStart != null) OnStart(); if (ThrowStart) throw new InvalidOperationException("token-secret"); return Result; }
            public void GetAttributes(out IntPtr pointer, out int count)
            {
                if (OnGet != null) OnGet();
                memory = new Utf8Memory();
                // A char* table has the same packed layout as Entry {char*, char*}.
                pointer = memory.Array(new[] { "clé", "日本語🙂", "error.type", "Crash" }); count = 2;
                if (InvalidUtf8Output) Marshal.WriteByte(Marshal.ReadIntPtr(pointer, IntPtr.Size), 0, 0xff);
            }
            public void FreeAttributes(IntPtr pointer, int count)
            { Frees++; Order.Add("free"); memory.Dispose(); if (ThrowFree) throw new InvalidOperationException("secret"); }
            public void AddAttribute(string key, string value) { Writes.Add(key + "=" + value); Order.Add(value); }
            public void Report(string message, bool main, bool ignore)
            { Reports++; Order.Add("report"); if (OnReport != null) OnReport(); if (ThrowReport) throw new InvalidOperationException("secret"); }
            public void Disable() { Stops++; Order.Add("disable"); }
        }
        [TestCase(0)] [TestCase(1)]
        public void SuccessIsOperational(int result)
        {
            var bridge = new Bridge { Result = result }; var lease = new Lease();
            var session = new AppleNativeSession(bridge, null); var owner = new object();
            Assert.IsTrue(session.Start(owner, Request(), () => lease));
            Assert.AreEqual(0, bridge.Received.ReportsPerMinute);
            Assert.AreEqual(1, bridge.Received.Attachments.Length);
            session.Stop(owner); session.Stop(owner);
            Assert.AreEqual(1, bridge.Stops); Assert.IsFalse(lease.Disposed);
            Assert.IsFalse(session.Start(new object(), Request(), () => new Lease()));
        }
        [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void RejectedBeforeInstallationReleasesLease(int result)
        {
            var bridge = new Bridge { Result = result }; var lease = new Lease();
            var session = new AppleNativeSession(bridge, null);
            Assert.IsFalse(session.Start(new object(), Request(), () => lease));
            Assert.IsTrue(lease.Disposed); Assert.AreEqual(AppleSessionState.Idle, session.State);
            Assert.AreEqual(0, bridge.Stops);
        }
        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(1234)]
        public void AmbiguousInstallationRetainsCaptureLease(int result)
        {
            var bridge = new Bridge { Result = result }; var lease = new Lease();
            var session = new AppleNativeSession(bridge, null);
            Assert.IsFalse(session.Start(new object(), Request(), () => lease));
            Assert.IsFalse(lease.Disposed); Assert.AreEqual(AppleSessionState.Disabled, session.State);
            Assert.IsFalse(session.Start(new object(), Request(), () => new Lease()));
        }
        [Test]
        public void VersionMismatchDoesNotAcquireCaptureLease()
        {
            var bridge = new Bridge { Abi = 2 }; bool acquired = false;
            var session = new AppleNativeSession(bridge, null);
            Assert.IsFalse(session.Start(new object(), Request(), () => { acquired = true; return new Lease(); }));
            Assert.IsFalse(acquired); Assert.AreEqual(0, bridge.Starts);
        }
        [Test]
        public void CompetingProcessDoesNotStartNative()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null);
            Assert.IsFalse(session.Start(new object(), Request(), () => null));
            Assert.AreEqual(0, bridge.Starts); Assert.AreEqual(AppleSessionState.Idle, session.State);
        }
        [Test]
        public void DuplicateClientCannotStopOwner()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null);
            var owner = new object(); var peer = new object();
            Assert.IsTrue(session.Start(owner, Request(), () => new Lease()));
            Assert.IsFalse(session.Start(peer, Request(), () => new Lease()));
            session.Stop(peer); Assert.IsTrue(session.IsActive(owner)); Assert.AreEqual(0, bridge.Stops);
        }
        [Test]
        public void StopDuringStartDisablesExactlyOnce()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null); var owner = new object();
            bridge.OnStart = () => session.Stop(owner);
            Assert.IsFalse(session.Start(owner, Request(), () => new Lease()));
            session.Stop(owner); Assert.AreEqual(1, bridge.Stops);
        }
        [Test]
        public void StopBeforeNativeStartReleasesCaptureLease()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null);
            var owner = new object(); var lease = new Lease();
            Assert.IsFalse(session.Start(owner, Request(), () => { session.Stop(owner); return lease; }));
            Assert.IsTrue(lease.Disposed); Assert.AreEqual(0, bridge.Starts);
            Assert.AreEqual(0, bridge.Stops); Assert.AreEqual(AppleSessionState.Idle, session.State);
        }
        [Test]
        public void NativeThrowNeverLogsCredential()
        {
            var logs = new List<string>(); var bridge = new Bridge { ThrowStart = true };
            var session = new AppleNativeSession(bridge, logs.Add);
            Assert.IsFalse(session.Start(new object(), Request(), () => new Lease()));
            Assert.IsFalse(string.Join(" ", logs.ToArray()).Contains("secret"));
            Assert.AreEqual(AppleSessionState.Disabled, session.State);
        }
        [Test]
        public void UnicodeRoundTripsAndNativeArrayFreed()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null); var owner = new object();
            session.Start(owner, Request(), () => new Lease()); var output = new Dictionary<string, string>();
            session.GetAttributes(owner, output);
            Assert.AreEqual("日本語🙂", output["clé"]); Assert.IsFalse(output.ContainsKey("error.type"));
            Assert.AreEqual(1, bridge.Frees);
        }
        [Test]
        public void StopDuringReadFinalizesAfterNativeFree()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null); var owner = new object();
            session.Start(owner, Request(), () => new Lease()); bridge.OnGet = () => session.Stop(owner);
            session.GetAttributes(owner, new Dictionary<string, string>());
            Assert.AreEqual("free,disable", string.Join(",", bridge.Order.ToArray()));
        }
        [Test]
        public void ThrowingDestinationStillFreesArray()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null); var owner = new object();
            session.Start(owner, Request(), () => new Lease());
            var readOnly = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
            session.GetAttributes(owner, readOnly); Assert.AreEqual(1, bridge.Frees);
        }
        [Test]
        public void InvalidNativeUtf8StillFreesArray()
        {
            var bridge = new Bridge { InvalidUtf8Output = true };
            var session = new AppleNativeSession(bridge, null); var owner = new object();
            session.Start(owner, Request(), () => new Lease());
            var destination = new Dictionary<string, string>();
            session.GetAttributes(owner, destination); session.Stop(owner);
            Assert.AreEqual(0, destination.Count); Assert.AreEqual(1, bridge.Frees);
            Assert.AreEqual(1, bridge.Stops);
        }
        [Test]
        public void FreeFailureDoesNotLeaveOperationInFlight()
        {
            var bridge = new Bridge { ThrowFree = true }; var session = new AppleNativeSession(bridge, null); var owner = new object();
            session.Start(owner, Request(), () => new Lease());
            session.GetAttributes(owner, new Dictionary<string, string>()); session.Stop(owner);
            Assert.AreEqual(1, bridge.Stops);
        }
        [Test]
        public void AnrFailureRestoresAttributeBeforeDisable()
        {
            var bridge = new Bridge { ThrowReport = true }; var session = new AppleNativeSession(bridge, null); var owner = new object();
            session.Start(owner, Request(), () => new Lease()); bridge.OnReport = () => session.Stop(owner);
            session.ReportAnr(owner, "ANR");
            Assert.AreEqual("Hang,report,Crash,disable", string.Join(",", bridge.Order.ToArray()));
        }
        [Test]
        public void RequestDoesNotMutateManagedAttributes()
        {
            var input = new Dictionary<string, string> { { "z", "last" }, { "a", "first" }, { "error.type", "Exception" } };
            var request = Request(input);
            Assert.AreEqual("Exception", input["error.type"]);
            Assert.AreEqual("a,error.type,z", string.Join(",", request.Keys));
            Assert.AreEqual("first,Crash,last", string.Join(",", request.Values));
        }
        [TestCase("bad\0key")]
        public void InvalidUtf8IsRejected(string value)
        { Assert.Throws<ArgumentException>(() => Request(new Dictionary<string, string> { { value, "v" } })); }
        [TestCase("")] [TestCase("..")] [TestCase("../x")] [TestCase("a/b")] [TestCase("a\\b")]
        public void InvalidBundleIdIsRejected(string id)
        { Assert.IsFalse(MacNativePaths.ValidIdentifier(id)); }
        [Test]
        public void InvalidSurrogateIsRejected()
        { Assert.Throws<System.Text.EncoderFallbackException>(() => Utf8Memory.Validate(new string(new[] { (char)0xd800 }))); }
        [Test]
        public void NativeArgumentBudgetIncludesAllStringTerminators()
        {
            const string url = "https://example.invalid/plcrash", path = "/tmp/private-native";
            var attributes = new Dictionary<string, string>();
            // Eight user keys plus the injected error.type=Crash pair.
            // Fill the remaining byte budget without exceeding any individual string limit.
            int remaining = 8 * 1024 * 1024 - Utf8Memory.ByteCount(url) - 1 -
                Utf8Memory.ByteCount(path) - 1 - ("error.type".Length + "Crash".Length + 2) - 8 * 4;
            for (int index = 0; index < 8; ++index)
            {
                int size = Math.Min(remaining, Utf8Memory.MaximumBytes);
                attributes["k" + index] = new string('x', size); remaining -= size;
            }
            var request = new AppleNativeRequest(url, attributes, null, false, false, 0, path);
            Assert.AreEqual(9, request.Keys.Length);
            attributes["k7"] += "x";
            Assert.Throws<ArgumentException>(() =>
                new AppleNativeRequest(url, attributes, null, false, false, 0, path));
        }
        [Test]
        public void ShutdownDoesNotWaitOnBlockedNativeReport()
        {
            var bridge = new Bridge(); var session = new AppleNativeSession(bridge, null); var owner = new object();
            var entered = new ManualResetEvent(false); var release = new ManualResetEvent(false);
            session.Start(owner, Request(), () => new Lease());
            bridge.OnReport = () => { entered.Set(); release.WaitOne(); };
            var worker = new Thread(() => session.ReportAnr(owner, "ANR")) { IsBackground = true };
            worker.Start();
            try
            {
                Assert.IsTrue(entered.WaitOne(5000));
                session.Stop(owner);
                Assert.AreEqual(AppleSessionState.Stopping, session.State);
                Assert.AreEqual(0, bridge.Stops);
            }
            finally
            {
                release.Set();
                bool exited = worker.Join(5000);
                if (exited) { entered.Close(); release.Close(); }
                Assert.IsTrue(exited);
            }
            Assert.AreEqual(1, bridge.Stops);
        }
        [Test]
        public void AllNativeBooleansAreOneByte()
        {
            var method = typeof(AppleNativeBridge).GetMethod("StartNative", BindingFlags.NonPublic | BindingFlags.Static);
            var dll = (DllImportAttribute)Attribute.GetCustomAttribute(method, typeof(DllImportAttribute));
            Assert.AreEqual(CallingConvention.Cdecl, dll.CallingConvention); Assert.IsTrue(dll.ExactSpelling);
            foreach (var p in method.GetParameters().Where(p => p.ParameterType == typeof(bool)))
                Assert.AreEqual(UnmanagedType.I1, ((MarshalAsAttribute)Attribute.GetCustomAttribute(p, typeof(MarshalAsAttribute))).Value);
        }
    }
}
#endif
