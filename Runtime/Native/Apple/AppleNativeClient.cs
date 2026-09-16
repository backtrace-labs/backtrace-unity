#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native.Base;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR
using Backtrace.Unity.Runtime.Native.OSX;
#endif

namespace Backtrace.Unity.Runtime.Native.Apple
{
    internal class AppleNativeClient : NativeClientBase, INativeClient
    {
        // This runtime is never reset by a Unity initialization attribute.
        // The native handler outlives managed SDK disablement. Editor construction does not load the plugin.
        private static readonly AppleNativeSession ProcessSession =
            new AppleNativeSession(new AppleNativeBridge(), Warn);
        private readonly AppleNativeSession session;
        private readonly object token = new object();
        private readonly object watchdogGate = new object();
        private Watchdog watchdog;
        private volatile bool disabled;

        protected AppleNativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs,
            IDictionary<string, string> attributes, ICollection<string> attachments) : base(configuration, breadcrumbs)
        {
            session = ProcessSession;
#if (UNITY_STANDALONE_OSX || UNITY_IOS) && !UNITY_EDITOR
            if (!configuration.CaptureNativeCrashes) return;
            try
            {
#if UNITY_IOS
                var request = AppleNativeRequest.ForIOS(
                    new BacktraceCredentials(configuration.GetValidServerUrl()).GetPlCrashReporterSubmissionUrl().ToString(),
                    attributes, attachments, configuration.OomReports, configuration.ClientSideUnwinding,
                    configuration.ReportPerMin);
                CaptureNativeCrashes = session.Start(token, request, () => IOSCaptureScope.Instance);
#else
                MacNativePaths paths = MacNativeEnvironment.ReadPaths();
                var request = new AppleNativeRequest(
                    new BacktraceCredentials(configuration.GetValidServerUrl()).GetPlCrashReporterSubmissionUrl().ToString(),
                    attributes, attachments, configuration.OomReports, configuration.ClientSideUnwinding,
                    configuration.ReportPerMin, paths.BasePath);
                CaptureNativeCrashes = session.Start(token, request, () => MacNativeEnvironment.Acquire(paths));
#endif
                if (CaptureNativeCrashes && configuration.HandleANR) HandleAnr();
            }
            catch (Exception) { Warn("BT_MAC_CONFIGURATION_FAILED"); }
#endif
        }
        protected AppleNativeClient(BacktraceConfiguration configuration, AppleNativeSession injectedSession,
            AppleNativeRequest request, Func<IDisposable> captureLease) : base(configuration, null)
        {
            if (injectedSession == null) throw new ArgumentNullException("injectedSession");
            session = injectedSession;
            if (configuration.CaptureNativeCrashes)
                CaptureNativeCrashes = session.Start(token, request, captureLease);
        }
        public void GetAttributes(IDictionary<string, string> result)
        { if (!disabled) session.GetAttributes(token, result); }
        public void SetAttribute(string key, string value)
        { if (!disabled) session.SetAttribute(token, key, value); }
        public bool OnOOM() { return !disabled && session.IsActive(token); }

        // Reimplements INativeClient.Update; keeps the base breadcrumb behavior.
        public new void Update(float time)
        {
            base.Update(time);
            lock (watchdogGate) if (watchdog != null) watchdog.Heartbeat();
        }
        public void HandleAnr()
        {
            bool startFailed = false;
            lock (watchdogGate)
            {
                if (disabled || watchdog != null || !session.IsActive(token)) return;
                var candidate = new Watchdog(this, AnrWatchdogTimeout);
                try { candidate.Start(); watchdog = candidate; }
                catch (Exception) { candidate.DisposeBeforeStart(); startFailed = true; }
            }
            if (startFailed) Warn("BT_MAC_WATCHDOG_START_FAILED");
        }
        public override void Disable()
        {
            lock (watchdogGate)
            {
                if (disabled) return;
                disabled = true; StopAnr = true; CaptureNativeCrashes = false;
                if (watchdog != null) { watchdog.Stop(); watchdog = null; }
            }
            session.Stop(token);
            base.Disable();
        }
        private static void Warn(string code)
        {
#if UNITY_IOS && !UNITY_EDITOR
            code = code.Replace("BT_MAC_", "BT_IOS_");
#endif
            try { UnityEngine.Debug.LogWarning(code); } catch (Exception) { }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private sealed class IOSCaptureScope : IDisposable
        {
            internal static readonly IOSCaptureScope Instance = new IOSCaptureScope();
            private IOSCaptureScope() { }
            public void Dispose() { }
        }
#endif

        private sealed class Watchdog
        {
            private readonly AppleNativeClient client;
            private readonly int timeout;
            private readonly object gate = new object();
            private readonly ManualResetEvent stop = new ManualResetEvent(false);
            private long heartbeat;
            private bool stopped, closed;
            internal Watchdog(AppleNativeClient client, int timeout)
            { this.client = client; this.timeout = Math.Max(1000, timeout); heartbeat = Stopwatch.GetTimestamp(); }
            internal void Heartbeat() { Interlocked.Exchange(ref heartbeat, Stopwatch.GetTimestamp()); }
            internal void Start()
            {
                var thread = new Thread(Run) { IsBackground = true, Name = "Backtrace Apple ANR" };
                client.AnrThread = thread; thread.Start();
            }
            internal void Stop() { lock (gate) { stopped = true; if (!closed) stop.Set(); } }
            internal void DisposeBeforeStart() { lock (gate) { stopped = true; closed = true; stop.Close(); } }
            private void Run()
            {
                long reportedHeartbeat = -1;
                try
                {
                    while (!stop.WaitOne(Math.Min(timeout, 1000)))
                    {
                        lock (gate) if (stopped) return;
                        if (client.disabled || !client.session.IsActive(client.token)) return;
                        if (client.PreventAnr) { Heartbeat(); reportedHeartbeat = -1; continue; }
                        long last = Interlocked.Read(ref heartbeat);
                        double elapsedMs = (Stopwatch.GetTimestamp() - last) * 1000.0 / Stopwatch.Frequency;
                        if (last == reportedHeartbeat || elapsedMs < timeout) continue;
                        reportedHeartbeat = last;
                        client.OnAnrDetection();
                        client.session.ReportAnr(client.token, AnrMessage);
                    }
                }
                catch (Exception) { Warn("BT_MAC_WATCHDOG_FAILED"); }
                finally { lock (gate) { closed = true; stop.Close(); } }
            }
        }
    }
}

#endif
