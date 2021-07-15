namespace Backtrace.Unity.Runtime.Native.Android
{
    internal enum UnwindingMode
    {
        LOCAL = 0,
        REMOTE = 1,
        REMOTE_DUMPWITHOUTCRASH = 2,
        LOCAL_DUMPWITHOUTCRASH = 3,
        LOCAL_CONTEXT = 4
    }
}
