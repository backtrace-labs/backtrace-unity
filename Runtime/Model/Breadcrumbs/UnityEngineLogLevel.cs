using System;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    /// <summary>
    /// Backtrace Breadcrumbs unity engine log received
    /// </summary>
    [Flags]
    public enum UnityEngineLogLevel
    {
        Assert = 1,
        Warning = 2,
        Log = 4,
        Exception = 8,
        Error = 16
    }
}
