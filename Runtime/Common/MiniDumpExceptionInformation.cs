using Backtrace.Unity.Types;
using System;
using System.Runtime.InteropServices;

namespace Backtrace.Unity.Common
{
    //typedef struct _MINIDUMP_EXCEPTION_INFORMATION {
    //    DWORD ThreadId;
    //    PEXCEPTION_POINTERS ExceptionPointers;
    //    BOOL ClientPointers;
    //} MINIDUMP_EXCEPTION_INFORMATION, *PMINIDUMP_EXCEPTION_INFORMATION;

    /// <summary>
    /// Exception information for current minidump method
    /// Pack=4 is important! So it works also for x64!
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct MiniDumpExceptionInformation
    {

        //====================================================================
        // Win32 Exception stuff
        // These are mostly interesting for Structured exception handling,
        // but need to be exposed for all exceptions (not just SEHException).
        //====================================================================
        //[System.Security.SecurityCritical]  // auto-generated_required
        //[System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
        //[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        //[System.Runtime.InteropServices.ComVisible(true)]
        //public static extern /* struct _EXCEPTION_POINTERS* */ IntPtr GetExceptionPointers();

        /// <summary>
        /// current thread id
        /// </summary>
        internal uint ThreadId;
        /// <summary>
        /// pointer to current exception
        /// </summary>
        internal IntPtr ExceptionPointers;

        /// <summary>
        /// Check who generate a pointer
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        internal bool ClientPointers;


        /// <summary>
        /// Create instance of MiniDumpExceptionInformation
        /// </summary>
        /// <param name="exceptionInfo">Type to check if exception exists</param>
        /// <returns>New instance of MiniDumpExceptionInformation</returns>
        internal static MiniDumpExceptionInformation GetInstance(MinidumpException exceptionInfo)
        {
            MiniDumpExceptionInformation exp;
            exp.ThreadId = SystemHelper.GetCurrentThreadId();
            exp.ClientPointers = false;
            exp.ExceptionPointers = IntPtr.Zero;
            // right now Unity environment doesn't support 
            // GetExceptionPointers
            // because of that we cannot pass exception pointer to minidump write method

            //right now GetExceptionPointers method is not available in .NET Standard 
//#if !NET_STANDARD_2_0
//            try
//            {
//                if (exceptionInfo == MinidumpException.Present)
//                {
//                    exp.ExceptionPointers = GetExceptionPointers();
//                }
//            }
//            catch (Exception e)
//            {
//#if DEBUG
//                UnityEngine.Debug.Log(string.Format("Cannot add exception information to minidump file. Reason: {0}", e));
//#endif
//                ///Operation not supported;
//            }
//#endif
            return exp;
        }
    }
}
