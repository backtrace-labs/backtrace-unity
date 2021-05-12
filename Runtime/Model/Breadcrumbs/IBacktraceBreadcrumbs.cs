using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    public interface IBacktraceBreadcrumbs
    {
        BacktraceBreadcrumbType BreadcrumbsLevel { get; }
        bool EnableBreadcrumbs(BacktraceBreadcrumbType level, UnityEngineLogLevel unityLogLevel);
        bool ClearBreadcrumbs();
        bool AddBreadcrumb(string message, LogType type, IDictionary<string, string> attributes);
        bool AddBreadcrumb(string message, LogType type);
        bool AddBreadcrumb(string message);
        bool Debug(string message);
        bool Debug(string message, IDictionary<string, string> attributes);
        bool Info(string message);
        bool Info(string message, IDictionary<string, string> attributes);
        bool Warning(string message);
        bool Warning(string message, IDictionary<string, string> attributes);
        bool Exception(Exception exception);
        bool Exception(Exception exception, IDictionary<string, string> attributes);
        bool Exception(string message);
        bool Exception(string message, IDictionary<string, string> attributes);
        bool FromBacktrace(BacktraceReport report);
        bool FromMonoBehavior(string message, LogType type, IDictionary<string, string> attributes);
        string GetBreadcrumbLogPath();
        void UnregisterEvents();

    }
}
