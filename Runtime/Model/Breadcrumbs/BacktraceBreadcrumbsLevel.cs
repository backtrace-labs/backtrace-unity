using System;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    /// <summary>
    /// Breadcrumbs level
    /// </summary>
    [Flags]
    public enum BacktraceBreadcrumbsLevel
    {
        Manual = BreadcrumbLevel.Manual,
        Log = BreadcrumbLevel.Log,
        Navigation = BreadcrumbLevel.Navigation,
        Http = BreadcrumbLevel.Http,
        System = BreadcrumbLevel.System,
        User = BreadcrumbLevel.User,
        Configuration = BreadcrumbLevel.Configuration,
    }

}
