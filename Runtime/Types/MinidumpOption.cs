﻿namespace Backtrace.Unity.Types
{
    /// <summary>
    /// Minidump generation options. These values are available in dbghelp.h and you can get more information here: http://blog.kalmbach-software.de/2008/12/13/writing-minidumps-in-c/
    /// </summary>
    public enum MiniDumpType: uint
    {
#pragma warning disable 1591
        // From dbghelp.h:
        None = 0x7FFFE,
        Normal = 0x00000000,
        WithDataSegs = 0x00000001,
        WithFullMemory = 0x00000002,
        WithHandleData = 0x00000004,
        FilterMemory = 0x00000008,
        ScanMemory = 0x00000010,
        WithUnloadedModules = 0x00000020,
        WithIndirectlyReferencedMemory = 0x00000040,
        FilterModulePaths = 0x00000080,
        WithProcessThreadData = 0x00000100,
        WithPrivateReadWriteMemory = 0x00000200,
        WithoutOptionalData = 0x00000400,
        WithFullMemoryInfo = 0x00000800,
        WithThreadInfo = 0x00001000,
        WithCodeSegs = 0x00002000,
        WithoutAuxiliaryState = 0x00004000,
        WithFullAuxiliaryState = 0x00008000,
        WithPrivateWriteCopyMemory = 0x00010000,
        IgnoreInaccessibleMemory = 0x00020000,
        ValidTypeFlags = 0x0003ffff,
#pragma warning restore 1591
    };
}
