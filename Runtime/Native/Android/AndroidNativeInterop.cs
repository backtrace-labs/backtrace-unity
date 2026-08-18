#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// P/Invoke declarations for libbacktrace-native.so. Return types must match the native exports:
    /// only InitializeJavaCrashHandler returns a value; AddAttribute,
    /// DumpWithoutCrash, and Disable are void. Declaring a native void function as returning bool reads undefined register contents.
    ///
    /// This class compiles in the Editor so the EditMode signature tests can pin these declarations by reflection,
    /// but the methods must only ever be INVOKED on an Android player, where the native library is loadable.
    /// </summary>
    internal static class AndroidNativeInterop
    {
        [DllImport("backtrace-native", EntryPoint = "InitializeJavaCrashHandler", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool InitializeJavaCrashHandler(
            IntPtr submissionUrl,
            IntPtr databasePath,
            IntPtr classPath,
            IntPtr keys,
            IntPtr values,
            IntPtr attachments,
            IntPtr environmentVariables);

        [DllImport("backtrace-native", EntryPoint = "AddAttribute", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void AddAttribute(IntPtr key, IntPtr value);

        [DllImport("backtrace-native", EntryPoint = "DumpWithoutCrash", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DumpWithoutCrash(
            IntPtr message,
            [MarshalAs(UnmanagedType.I1)] bool setMainThreadAsFaultingThread);

        [DllImport("backtrace-native", EntryPoint = "Disable", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Disable();
    }
}
#endif
