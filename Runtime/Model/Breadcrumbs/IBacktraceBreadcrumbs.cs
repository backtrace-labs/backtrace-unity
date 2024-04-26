using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    public interface IBacktraceBreadcrumbs
    {
        BacktraceBreadcrumbType BreadcrumbsLevel { get; }
        bool EnableBreadcrumbs();
        [Obsolete("Please use EnableBreadcrumbs instead. This function will be removed in the future updates")]
        bool EnableBreadcrumbs(BacktraceBreadcrumbType level, UnityEngineLogLevel unityLogLevel);
        bool ClearBreadcrumbs();
        bool Log(string message, BreadcrumbLevel level, LogType logType, IDictionary<string, string> attributes);
        bool Log(string message, LogType type, IDictionary<string, string> attributes);
        bool Log(string message, LogType type);
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
        double BreadcrumbId();
        void UnregisterEvents();
        void Update();
        string Archive();
    }
}
