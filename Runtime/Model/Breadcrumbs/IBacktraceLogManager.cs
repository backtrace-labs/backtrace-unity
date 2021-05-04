﻿using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    internal interface IBacktraceLogManager
    {
        string BreadcrumbsFilePath { get; }
        bool Add(string message, BreadcrumbLevel level, LogType type, IDictionary<string, string> attributes);
        bool Clear();
        bool Enable();
    }
}
