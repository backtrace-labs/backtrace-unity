using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Backtrace.Unity.Common
{
    /// <summary>
    /// Get information about dll's
    /// </summary>
    internal static class SystemHelper
    {
        /// <summary>
        /// Get current thread Id
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        internal static extern uint GetCurrentThreadId();

        /// <summary>
        /// Check if library exists
        /// </summary>
        /// <param name="lpFileName">Library name</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// Check if library is available
        /// </summary>
        /// <param name="libraryName">library name</param>
        internal static bool IsLibraryAvailable(string libraryName)
        {
            try
            {
                return LoadLibrary(libraryName) != IntPtr.Zero;
            }
            catch (TypeLoadException)
            {
                Trace.WriteLine("Cannot use library to generate minidump file");
            }
            catch (Exception)
            {
                //Operation not supported - library not exists or something really bad happend
                Trace.WriteLine("Cannot load libraries required to generate minidump files");
            }
            return false;
        }

        /// <summary>
        /// Check if libraries are available in system
        /// </summary>
        /// <param name="libraries">Library name to check</param>
        internal static bool IsLibraryAvailable(string[] libraries)
        {
            if (libraries == null || libraries.Length == 0)
            {
                return true;
            }
            return !libraries.Any(n => !IsLibraryAvailable(n));
        }

        /// <summary>
        /// Get current system name
        /// </summary>
        /// <param name="architecture">System architecture</param>
        /// <returns>System name</returns>
        internal static string Name(string architecture)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "IOS";
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                    return "Linux";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "Mac OS";
                case RuntimePlatform.PS3:
                    return "PS3";
                case RuntimePlatform.PS4:
                    return "PS4";
                case RuntimePlatform.TizenPlayer:
                case RuntimePlatform.SamsungTVPlayer:
                    return "Samsung TV";
                case RuntimePlatform.tvOS:
                    return "Apple tvOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.WiiU:
                    return "WiiU";
                case RuntimePlatform.Switch:
                    return "Switch";
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    return "Windows";
                case RuntimePlatform.XBOX360:
                case RuntimePlatform.XboxOne:
                    return "Xbox";
                default:
                    return "NaCl";
            }
        }

        internal static string CpuArchitecture()
        {
            return Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.ToLower();
        }

        /// <summary>
        /// Detect a system assemblies - assemblies that root namespace is "System" or "Microsoft
        /// If assembly is null, method will return false
        /// </summary>
        /// <param name="assembly">Assembly to check</param>
        /// <returns>True if assembly is from Microsoft of System</returns>
        internal static bool SystemAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }
            var assemblyName = assembly.GetName().Name;
            return SystemAssembly(assemblyName);
        }
        /// <summary>
        /// Detect a system assemblies - assemblies that root namespace is "System" or "Microsoft
        /// </summary>
        /// <returns>True if assembly is from Microsoft of System</returns>
        internal static bool SystemAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                return false;
            }
            return (assemblyName.StartsWith("Microsoft.")
                || assemblyName.StartsWith("mscorlib")
                || assemblyName.Equals("System")
                || assemblyName.StartsWith("System."));
        }
    }
}
