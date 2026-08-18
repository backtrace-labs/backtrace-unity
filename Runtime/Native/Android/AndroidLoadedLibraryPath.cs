#if UNITY_ANDROID
using System;
using System.Runtime.InteropServices;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Locates the exact libbacktrace-native.so module path selected by Android's native linker.
    /// The library is already loaded by the P/Invoke bridge, so dlopen only bumps its reference count;
    /// dlsym on the exported initialization symbol plus dladdr then reports the containing module;
    /// either an extracted file path or an APK-backed path such as "/data/app/.../split_config.arm64_v8a.apk!/lib/arm64-v8a/libbacktrace-native.so".
    /// That answer is authoritative: Android has already proved which module this process loaded.
    /// </summary>
    internal static class AndroidLoadedLibraryPath
    {
        // RTLD_LAZY has the same value on 32- and 64-bit bionic.
        // RTLD_NOW (2 on LP64) must NOT be used here: on LP32 bionic the value 2 means RTLD_GLOBAL,
        // which would irreversibly promote every exported symbol of the already-loaded library into the global group and let later dlopen'ed libraries bind against them.
        // Binding mode is irrelevant anyway, the library is already loaded and relocated; dlopen only bumps its reference count.
        private const int RtldLazy = 1;
        private const string NativeLibraryName = "libbacktrace-native.so";
        private const string AnchorSymbol = "InitializeJavaCrashHandler";

        private static readonly object HandleLock = new object();

        // Intentionally retained for process lifetime. The SDK depends on this module for the rest of the process.
        private static IntPtr _libraryHandle;

        [StructLayout(LayoutKind.Sequential)]
        private struct DlInfo
        {
            internal IntPtr FileName;
            internal IntPtr BaseAddress;
            internal IntPtr SymbolName;
            internal IntPtr SymbolAddress;
        }

        [DllImport("libdl.so", EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr DlOpen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flags);

        [DllImport("libdl.so", EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr DlSym(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string symbol);

        [DllImport("libdl.so", EntryPoint = "dladdr", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DlAddr(IntPtr address, out DlInfo information);

        /// <summary>
        /// Returns the linker-reported path of the loaded Backtrace native library, or null when module metadata is unavailable.
        /// Never throws.
        /// </summary>
        internal static string TryGet()
        {
            try
            {
                lock (HandleLock)
                {
                    if (_libraryHandle == IntPtr.Zero)
                    {
                        _libraryHandle = DlOpen(NativeLibraryName, RtldLazy);
                    }
                    if (_libraryHandle == IntPtr.Zero)
                    {
                        return null;
                    }

                    IntPtr anchor = DlSym(_libraryHandle, AnchorSymbol);
                    if (anchor == IntPtr.Zero)
                    {
                        return null;
                    }

                    DlInfo information;
                    if (DlAddr(anchor, out information) == 0 || information.FileName == IntPtr.Zero)
                    {
                        return null;
                    }

                    string path = Marshal.PtrToStringAnsi(information.FileName);
                    return string.IsNullOrEmpty(path) ? null : path;
                }
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch (SEHException)
            {
                return null;
            }
        }
    }
}
#endif
