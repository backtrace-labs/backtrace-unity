#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR || BACKTRACE_STANDALONE_TEST
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Backtrace.Unity.Runtime.Native.Apple
{
    internal enum AppleNativeResult
    {
        Success = 0, AlreadyActive = 1, InvalidArguments = 2,
        StorageFailure = 3, InvalidUrl = 4, ClientFailure = 5,
        UnexpectedFailure = 6, RestartRequired = 7
    }

    internal sealed class AppleNativeRequest
    {
        internal readonly string Url;
        internal readonly string[] Keys, Values, Attachments;
        internal readonly bool Oom, Unwinding;
        internal readonly int ReportsPerMinute;
        internal readonly string BasePath;

        internal AppleNativeRequest(string url, IDictionary<string, string> attributes,
            IEnumerable<string> attachments, bool oom, bool unwinding, int rate, string basePath)
            : this(url, attributes, attachments, oom, unwinding, rate, basePath, false)
        {
        }

        internal static AppleNativeRequest ForIOS(string url, IDictionary<string, string> attributes,
            IEnumerable<string> attachments, bool oom, bool unwinding, int rate)
        {
            // Preserve released iOS PLCrashReporter storage. Changing this path would strand pending reports created by an older iOS package.
            return new AppleNativeRequest(url, attributes, attachments, oom, unwinding, rate, null, true);
        }

        private AppleNativeRequest(string url, IDictionary<string, string> attributes,
            IEnumerable<string> attachments, bool oom, bool unwinding, int rate, string basePath,
            bool useDefaultIOSStorage)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http") ||
                string.IsNullOrEmpty(uri.Host) || rate < 0)
                throw new ArgumentException("Invalid native configuration.");
            Utf8Memory.Validate(url);
            if (!useDefaultIOSStorage)
            {
                Utf8Memory.Validate(basePath);
                if (string.IsNullOrEmpty(basePath) || !System.IO.Path.IsPathRooted(basePath))
                    throw new ArgumentException("An absolute macOS native storage path is required.");
            }
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (attributes != null)
                foreach (var pair in attributes)
                {
                    if (string.IsNullOrEmpty(pair.Key)) continue;
                    Utf8Memory.Validate(pair.Key);
                    Utf8Memory.Validate(pair.Value ?? string.Empty);
                    sorted[pair.Key] = pair.Value ?? string.Empty;
                    if (sorted.Count > 16384) throw new ArgumentException("Too many native attributes.");
                }
            sorted["error.type"] = "Crash";
            if (sorted.Count > 16384) throw new ArgumentException("Too many native attributes.");
            Keys = new string[sorted.Count]; Values = new string[sorted.Count];
            int index = 0;
            foreach (var pair in sorted) { Keys[index] = pair.Key; Values[index++] = pair.Value; }
            var paths = new SortedDictionary<string, bool>(StringComparer.Ordinal);
            if (attachments != null)
                foreach (string path in attachments)
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    Utf8Memory.Validate(path);
                    paths[path] = true;
                    if (paths.Count > 1024) throw new ArgumentException("Too many native attachments.");
                }
            if (paths.Count > 1024) throw new ArgumentException("Too many native attachments.");
            Attachments = new string[paths.Count]; paths.Keys.CopyTo(Attachments, 0);
            // Every native string includes its terminating NUL, including these two standalone arguments (the arrays below account for theirs individually).
            long totalBytes = (long)Utf8Memory.ByteCount(url) + 1 + (basePath == null ? 0 : Utf8Memory.ByteCount(basePath) + 1);
            foreach (string value in Keys) totalBytes += Utf8Memory.ByteCount(value) + 1;
            foreach (string value in Values) totalBytes += Utf8Memory.ByteCount(value) + 1;
            foreach (string value in Attachments) totalBytes += Utf8Memory.ByteCount(value) + 1;
            if (totalBytes > 8L * 1024 * 1024) throw new ArgumentException("Native argument budget exceeded.");
            Url = url; BasePath = basePath; Oom = oom; Unwinding = unwinding; ReportsPerMinute = rate;
        }
    }

    internal interface IAppleNativeBridge
    {
        int Version();
        int Start(AppleNativeRequest request);
        void GetAttributes(out IntPtr entries, out int count);
        void FreeAttributes(IntPtr entries, int count);
        void AddAttribute(string key, string value);
        void Report(string message, bool mainThread, bool ignoreDebugger);
        void Disable();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AppleNativeEntry { internal IntPtr Key; internal IntPtr Value; }

    /// <summary>Only frees memory allocated by this managed call, never native-owned entries.</summary>
    internal sealed class Utf8Memory : IDisposable
    {
        internal const int MaximumBytes = 1024 * 1024;
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);
        private readonly List<IntPtr> allocations = new List<IntPtr>();
        private bool disposed;

        internal static int ByteCount(string value) { Validate(value); return Encoding.GetByteCount(value); }
        internal static void Validate(string value)
        {
            if (value == null || value.IndexOf('\0') >= 0 || Encoding.GetByteCount(value) > MaximumBytes)
                throw new ArgumentException("Invalid UTF-8 native argument.");
        }
        internal IntPtr String(string value)
        {
            if (disposed) throw new ObjectDisposedException("Utf8Memory");
            Validate(value);
            byte[] bytes = Encoding.GetBytes(value);
            IntPtr memory = Marshal.AllocHGlobal(checked(bytes.Length + 1));
            Remember(memory);
            Marshal.Copy(bytes, 0, memory, bytes.Length);
            Marshal.WriteByte(memory, bytes.Length, 0);
            return memory;
        }
        internal IntPtr Array(string[] values)
        {
            if (disposed) throw new ObjectDisposedException("Utf8Memory");
            if (values.Length == 0) return IntPtr.Zero;
            IntPtr memory = Marshal.AllocHGlobal(checked(values.Length * IntPtr.Size));
            Remember(memory);
            for (int i = 0; i < values.Length; ++i)
                Marshal.WriteIntPtr(memory, checked(i * IntPtr.Size), String(values[i]));
            return memory;
        }
        private void Remember(IntPtr memory)
        {
            try { allocations.Add(memory); }
            catch { Marshal.FreeHGlobal(memory); throw; }
        }
        internal static string Read(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return string.Empty;
            int size = 0;
            while (size <= MaximumBytes && Marshal.ReadByte(pointer, size) != 0) size++;
            if (size > MaximumBytes) throw new ArgumentException("Native string exceeds the ABI bound.");
            byte[] bytes = new byte[size]; Marshal.Copy(pointer, bytes, 0, size);
            return Encoding.GetString(bytes);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = allocations.Count - 1; i >= 0; --i) Marshal.FreeHGlobal(allocations[i]);
            allocations.Clear();
        }
    }

    internal sealed class AppleNativeBridge : IAppleNativeBridge
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string Library = "__Internal";
#else
        private const string Library = "BacktraceMacUnity";
#endif
        [DllImport(Library, EntryPoint="BacktraceUnityBridgeVersion", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern int VersionNative();
        [DllImport(Library, EntryPoint="StartBacktraceIntegrationV3", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern int StartNative(IntPtr url, IntPtr keys, IntPtr values, int count,
            [MarshalAs(UnmanagedType.I1)] bool oom, IntPtr attachments, int attachmentCount,
            [MarshalAs(UnmanagedType.I1)] bool unwind, int rate, IntPtr basePath);
        [DllImport(Library, EntryPoint="GetAttributes", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void GetNative(out IntPtr entries, out int count);
        [DllImport(Library, EntryPoint="FreeAttributes", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void FreeNative(IntPtr entries, int count);
        [DllImport(Library, EntryPoint="AddAttribute", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void AddNative(IntPtr key, IntPtr value);
        [DllImport(Library, EntryPoint="NativeReport", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void ReportNative(IntPtr message, [MarshalAs(UnmanagedType.I1)] bool mainThread,
            [MarshalAs(UnmanagedType.I1)] bool ignoreDebugger);
        [DllImport(Library, EntryPoint="Disable", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void DisableNative();
        public int Version() { return VersionNative(); }
        public int Start(AppleNativeRequest request)
        {
            using (var memory = new Utf8Memory())
                return StartNative(memory.String(request.Url), memory.Array(request.Keys),
                    memory.Array(request.Values), request.Keys.Length, request.Oom,
                    memory.Array(request.Attachments), request.Attachments.Length, request.Unwinding,
                    request.ReportsPerMinute, request.BasePath == null ? IntPtr.Zero : memory.String(request.BasePath));
        }
        public void GetAttributes(out IntPtr entries, out int count) { GetNative(out entries, out count); }
        public void FreeAttributes(IntPtr entries, int count) { FreeNative(entries, count); }
        public void AddAttribute(string key, string value)
        { using (var memory = new Utf8Memory()) AddNative(memory.String(key), memory.String(value)); }
        public void Report(string message, bool mainThread, bool ignoreDebugger)
        { using (var memory = new Utf8Memory()) ReportNative(memory.String(message), mainThread, ignoreDebugger); }
        public void Disable() { DisableNative(); }
    }
}
#endif
