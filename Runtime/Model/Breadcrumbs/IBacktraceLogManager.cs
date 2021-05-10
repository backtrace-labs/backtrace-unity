using System.Collections.Generic;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    internal interface IBacktraceLogManager
    {
        string BreadcrumbsFilePath { get; }
        bool Add(string message, BreadcrumbLevel level, UnityEngineLogLevel type, IDictionary<string, string> attributes);
        bool Clear();
        bool Enable();
        int Length();
        long BreadcrumbId();
    }
}
