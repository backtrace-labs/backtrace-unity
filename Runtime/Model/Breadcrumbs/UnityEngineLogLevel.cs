using System;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    /// <summary>
    /// Backtrace Breadcrumbs unity engine log received
    /// </summary>
    [Flags]
    public enum UnityEngineLogLevel
    {
        Debug = 1,
        Warning = 2,
        Info = 4,
        Fatal = 8,
        Error = 16
    }
}
