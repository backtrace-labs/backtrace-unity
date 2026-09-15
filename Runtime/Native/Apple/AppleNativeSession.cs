#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR || BACKTRACE_STANDALONE_TEST
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Backtrace.Unity.Runtime.Native.Apple
{
    internal enum AppleSessionState { Idle, Starting, Active, Stopping, Disabled, Unavailable }

    /// <summary>
    /// One managed owner, one native installation.
    /// Never holds the state lock across a native call, user dictionary operation, or logger.
    /// Disable waits logically for admitted operations, not by blocking a Unity thread on a worker thread.
    /// </summary>
    internal sealed class AppleNativeSession
    {
        private readonly object gate = new object();
        private readonly object attributeGate = new object();
        private readonly IAppleNativeBridge bridge;
        private readonly Action<string> warning;
        private object owner;
        private AppleSessionState state;
        private bool stopRequested, disableSent;
        private int operations;
        // Deliberately retained after native Disable: the fatal handler is still installed.
        private IDisposable captureLease;

        internal AppleNativeSession(IAppleNativeBridge bridge, Action<string> warning)
        {
            if (bridge == null) throw new ArgumentNullException("bridge");
            this.bridge = bridge; this.warning = warning;
        }
        internal AppleSessionState State { get { lock (gate) return state; } }
        internal bool IsActive(object token)
        { lock (gate) return token != null && ReferenceEquals(owner, token) && state == AppleSessionState.Active; }

        internal bool Start(object token, AppleNativeRequest request, Func<IDisposable> acquireCaptureLease)
        {
            if (token == null || request == null || acquireCaptureLease == null)
                throw new ArgumentNullException("A native start dependency is missing.");
            bool admitted;
            lock (gate)
            {
                admitted = state == AppleSessionState.Idle;
                if (admitted) { owner = token; state = AppleSessionState.Starting; stopRequested = false; }
            }
            if (!admitted) { Warn("BT_MAC_START_NOT_AVAILABLE"); return false; }
            IDisposable lease = null;
            bool enteredNativeStart = false;
            try
            {
                if (bridge.Version() != 3)
                {
                    FinishPreflight(AppleSessionState.Unavailable);
                    Warn("BT_MAC_ABI_MISMATCH"); return false;
                }
                lease = acquireCaptureLease();
                if (lease == null)
                {
                    FinishPreflight(AppleSessionState.Idle);
                    Warn("BT_MAC_CAPTURE_BUSY"); return false;
                }
                bool cancelled;
                lock (gate)
                {
                    cancelled = stopRequested;
                    if (cancelled) { state = AppleSessionState.Idle; owner = null; }
                    else enteredNativeStart = true;
                }
                if (cancelled) return false;
                int result = bridge.Start(request);
                bool active = result == (int)AppleNativeResult.Success || result == (int)AppleNativeResult.AlreadyActive;
                bool knownPreInstallRejection = result >= 2 && result <= 4;
                lock (gate)
                {
                    if (active)
                    {
                        captureLease = lease; lease = null;
                        state = stopRequested ? AppleSessionState.Stopping : AppleSessionState.Active;
                    }
                    else if (knownPreInstallRejection)
                    { state = AppleSessionState.Idle; owner = null; }
                    else
                    {
                        // Cocoa 5 can be returned AFTER handlerInstallationAttempted.
                        // It is not safe to release this process's capture lock or retry.
                        captureLease = lease; lease = null;
                        state = AppleSessionState.Disabled; owner = null;
                    }
                }
                if (!active) Warn("BT_MAC_INIT_RESULT_" + result.ToString(System.Globalization.CultureInfo.InvariantCulture));
                CompleteStopIfQuiescent();
                return IsActive(token);
            }
            catch (DllNotFoundException)
            { FinishPreflight(AppleSessionState.Unavailable); Warn("BT_MAC_LIBRARY_MISSING"); return false; }
            catch (EntryPointNotFoundException)
            { FinishPreflight(AppleSessionState.Unavailable); Warn("BT_MAC_EXPORT_MISSING"); return false; }
            catch (Exception)
            {
                lock (gate)
                {
                    state = enteredNativeStart ? AppleSessionState.Disabled : AppleSessionState.Idle;
                    owner = null;
                    if (enteredNativeStart) { captureLease = lease; lease = null; }
                }
                Warn(enteredNativeStart ? "BT_MAC_RESTART_REQUIRED" : "BT_MAC_PREFLIGHT_FAILED");
                return false;
            }
            finally
            {
                try { if (lease != null) lease.Dispose(); }
                catch (Exception) { Warn("BT_MAC_CAPTURE_RELEASE_FAILED"); }
            }
        }
        private void FinishPreflight(AppleSessionState next)
        { lock (gate) { state = next; owner = null; } }
        private bool Enter(object token)
        {
            lock (gate)
            {
                if (token == null || !ReferenceEquals(owner, token) || state != AppleSessionState.Active) return false;
                ++operations; return true;
            }
        }
        private void Exit()
        { lock (gate) --operations; CompleteStopIfQuiescent(); }

        internal void SetAttribute(object token, string key, string value)
        {
            if (string.IsNullOrEmpty(key) || value == null || !Enter(token)) return;
            try
            {
                Utf8Memory.Validate(key); Utf8Memory.Validate(value);
                lock (attributeGate) bridge.AddAttribute(key, value);
            }
            catch (Exception) { Warn("BT_MAC_ATTRIBUTE_FAILED"); }
            finally { Exit(); }
        }
        internal void GetAttributes(object token, IDictionary<string, string> destination)
        {
            if (destination == null || !Enter(token)) return;
            IntPtr entries = IntPtr.Zero; int count = 0;
            try
            {
                lock (attributeGate) bridge.GetAttributes(out entries, out count);
                if (count < 0 || count > 16384 || (entries == IntPtr.Zero && count != 0))
                    throw new InvalidOperationException("Invalid native attribute response.");
                int stride = Marshal.SizeOf(typeof(AppleNativeEntry));
                for (int i = 0; i < count; ++i)
                {
                    var address = new IntPtr(checked(entries.ToInt64() + (long)i * stride));
                    var entry = (AppleNativeEntry)Marshal.PtrToStructure(address, typeof(AppleNativeEntry));
                    string key = Utf8Memory.Read(entry.Key);
                    if (key.Length != 0 && key != "error.type") destination[key] = Utf8Memory.Read(entry.Value);
                }
            }
            catch (Exception) { Warn("BT_MAC_ATTRIBUTE_READ_FAILED"); }
            finally
            {
                try { if (entries != IntPtr.Zero) bridge.FreeAttributes(entries, count); }
                catch (Exception) { Warn("BT_MAC_ATTRIBUTE_FREE_FAILED"); }
                finally { Exit(); }
            }
        }
        internal void ReportAnr(object token, string message)
        {
            if (!Enter(token)) return;
            bool addFailed = false, reportFailed = false, restoreFailed = false;
            try
            {
                lock (attributeGate)
                {
                    try { bridge.AddAttribute("error.type", "Hang"); }
                    catch (Exception) { addFailed = true; }
                    try { bridge.Report(message, true, true); }
                    catch (Exception) { reportFailed = true; }
                    finally
                    {
                        try { bridge.AddAttribute("error.type", "Crash"); }
                        catch (Exception) { restoreFailed = true; }
                    }
                }
                if (addFailed) Warn("BT_MAC_ANR_ATTRIBUTE_FAILED");
                if (reportFailed) Warn("BT_MAC_ANR_REPORT_FAILED");
                if (restoreFailed) Warn("BT_MAC_ANR_RESTORE_FAILED");
            }
            finally { Exit(); }
        }
        internal void Stop(object token)
        {
            lock (gate)
            {
                if (token == null || !ReferenceEquals(owner, token)) return;
                stopRequested = true;
                if (state == AppleSessionState.Active) state = AppleSessionState.Stopping;
            }
            CompleteStopIfQuiescent();
        }
        private void CompleteStopIfQuiescent()
        {
            bool call;
            lock (gate)
            {
                call = state == AppleSessionState.Stopping && operations == 0 && !disableSent;
                if (call) { disableSent = true; state = AppleSessionState.Disabled; owner = null; }
            }
            if (!call) return;
            try { bridge.Disable(); }
            catch (Exception) { Warn("BT_MAC_DISABLE_FAILED"); }
            // Do not dispose captureLease: Cocoa still owns a fatal handler and a live slot.
            GC.KeepAlive(captureLease);
        }
        private void Warn(string code)
        {
#if UNITY_IOS && !UNITY_EDITOR
            code = code.Replace("BT_MAC_", "BT_IOS_");
#endif
            try { if (warning != null) warning(code); } catch (Exception) { }
        }
    }
}
#endif
