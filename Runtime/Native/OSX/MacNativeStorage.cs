#if UNITY_STANDALONE_OSX || UNITY_EDITOR || BACKTRACE_STANDALONE_TEST
using System;
using Backtrace.Unity.Runtime.Native.Apple;
using System.IO;
using System.Runtime.InteropServices;

namespace Backtrace.Unity.Runtime.Native.OSX
{
    internal sealed class MacNativePaths
    {
        internal readonly string CacheRoot, Identifier, Root, BasePath, CaptureLockPath, LivePath, LegacyPath;
        internal MacNativePaths(string cacheRoot, string identifier)
        {
            if (string.IsNullOrEmpty(cacheRoot) || !Path.IsPathRooted(cacheRoot))
                throw new ArgumentException("The cache root must be absolute.");
            if (!ValidIdentifier(identifier)) throw new ArgumentException("Invalid bundle identifier.");
            CacheRoot = Path.GetFullPath(cacheRoot).TrimEnd(Path.DirectorySeparatorChar);
            if (CacheRoot.Length == 0) throw new ArgumentException("The filesystem root is not a cache root.");
            Identifier = identifier;
            Root = Path.Combine(Path.Combine(Path.Combine(Path.Combine(CacheRoot, identifier), "Backtrace"), "NativeCrash"), "v1");
            BasePath = Path.Combine(Root, "plcrash");
            CaptureLockPath = Path.Combine(Root, "capture.lock");
            LivePath = Path.Combine(Path.Combine(Path.Combine(BasePath, "com.plausiblelabs.crashreporter.data"), identifier), "live_report.plcrash");
            LegacyPath = Path.Combine(Path.Combine(Path.Combine(CacheRoot, "com.plausiblelabs.crashreporter.data"), identifier), "live_report.plcrash");
        }
        internal static bool ValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "." || value == ".." || value.Length > 255) return false;
            foreach (char c in value)
                if (c == '/' || c == '\\' || char.IsControl(c) || char.IsWhiteSpace(c)) return false;
            return true;
        }
    }

    internal static class MacNativeEnvironment
    {
        private const string Foundation = "/System/Library/Frameworks/Foundation.framework/Foundation";
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        private const string SystemLibrary = "/usr/lib/libSystem.B.dylib";
        [DllImport(Foundation, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr NSSearchPathForDirectoriesInDomains(UIntPtr directory, UIntPtr domain, [MarshalAs(UnmanagedType.I1)] bool expand);
        [DllImport(ObjC, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC, EntryPoint="objc_msgSend", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr objc_autoreleasePoolPush();
        [DllImport(ObjC, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void objc_autoreleasePoolPop(IntPtr pool);
        [DllImport(SystemLibrary, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr realpath(IntPtr path, IntPtr buffer);
        [DllImport(SystemLibrary, ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern void free(IntPtr buffer);
        private static IntPtr Message(IntPtr target, string selector)
        { return target == IntPtr.Zero ? IntPtr.Zero : Send(target, sel_registerName(selector)); }
        private static string StringValue(IntPtr value)
        { return Utf8Memory.Read(Message(value, "UTF8String")); }

        internal static MacNativePaths ReadPaths()
        {
            IntPtr pool = objc_autoreleasePoolPush();
            try
            {
                // Foundation uses the same sandbox-aware cache root and main bundle as PLCrashReporter.
                IntPtr caches = NSSearchPathForDirectoriesInDomains(new UIntPtr(13), new UIntPtr(1), true);
                string cachePath = StringValue(Message(caches, "firstObject"));
                if (string.IsNullOrEmpty(cachePath) || !Path.IsPathRooted(cachePath))
                    throw new IOException("Foundation returned no absolute cache path.");
                Directory.CreateDirectory(cachePath);
                string cacheRoot = Canonical(cachePath);
                IntPtr bundle = Message(objc_getClass("NSBundle"), "mainBundle");
                string identifier = StringValue(Message(bundle, "bundleIdentifier"));
                return new MacNativePaths(cacheRoot, identifier);
            }
            finally { objc_autoreleasePoolPop(pool); }
        }
        internal static string Canonical(string path)
        {
            using (var memory = new Utf8Memory())
            {
                IntPtr result = realpath(memory.String(path), IntPtr.Zero);
                if (result == IntPtr.Zero) throw new IOException("Unable to resolve native storage.");
                try { return Utf8Memory.Read(result); } finally { free(result); }
            }
        }
        internal static IDisposable Acquire(MacNativePaths paths)
        {
            // Build one component at a time and reject canonical aliases below the trusted cache root.
            string current = paths.CacheRoot;
            foreach (string part in new[] { paths.Identifier, "Backtrace", "NativeCrash", "v1", "plcrash" })
            {
                current = Path.Combine(current, part);
                Directory.CreateDirectory(current);
                if (!string.Equals(Canonical(current), current, StringComparison.Ordinal))
                    throw new IOException("A private storage component is aliased.");
            }
            return MacCaptureLease.TryAcquire(paths.CaptureLockPath);
        }
    }

    /// <summary>A cooperative single-writer lock for PLCrashReporter's one live payload slot.</summary>
    internal sealed class MacCaptureLease : IDisposable
    {
        private const string Library = "/usr/lib/libSystem.B.dylib";
        [DllImport(Library, EntryPoint="open", ExactSpelling=true, SetLastError=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern int Open(IntPtr path, int flags);
        [DllImport(Library, EntryPoint="flock", ExactSpelling=true, SetLastError=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern int Flock(int fd, int operation);
        [DllImport(Library, EntryPoint="close", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl)]
        private static extern int Close(int fd);
        private int descriptor;
        private MacCaptureLease(int descriptor) { this.descriptor = descriptor; }
        internal static MacCaptureLease TryAcquire(string path)
        {
            // CreateNew maps to exclusive creation: never truncate an existing lock inode.
            // Let the runtime perform creation instead of passing a variadic mode_t through P/Invoke.
            // Apple arm64 has a distinct ABI for variadic arguments to open().
            try
            {
                using (var created = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite)) { }
            }
            catch (IOException)
            {
                // A concurrent process may have created it.
                // The no-follow open below is authoritative; missing/inaccessible files still fail without truncation.
            }
            int fd;
            // Darwin: O_RDWR | O_NOFOLLOW | O_CLOEXEC | O_NONBLOCK; no O_CREAT/no varargs.
            using (var memory = new Utf8Memory()) fd = Open(memory.String(path), 0x2 | 0x100 | 0x1000000 | 0x4);
            if (fd < 0) throw new IOException("Unable to open capture lock.");
            if (Flock(fd, 2 | 4) == 0) return new MacCaptureLease(fd);
            int error = Marshal.GetLastWin32Error(); Close(fd);
            if (error == 35) return null; // EWOULDBLOCK on Darwin.
            throw new IOException("Unable to acquire capture lock.");
        }
        public void Dispose()
        {
            int fd = System.Threading.Interlocked.Exchange(ref descriptor, -1);
            if (fd >= 0) Close(fd);
            // Never unlink the lock path: another process may already hold its inode open.
        }
    }
}
#endif
