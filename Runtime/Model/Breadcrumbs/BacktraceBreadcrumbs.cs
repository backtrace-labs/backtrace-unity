using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.Breadcrumbs
{
    internal sealed class BacktraceBreadcrumbs : IBacktraceBreadcrumbs
    {
        /// <summary>
        /// Breadcrumbs log level
        /// </summary>
        public BacktraceBreadcrumbType BreadcrumbsLevel { get; internal set; }

        /// <summary>
        /// Unity engine log level
        /// </summary>
        public UnityEngineLogLevel UnityLogLevel { get; set; }

        /// <summary>
        /// Log manager 
        /// </summary>
        internal readonly IBacktraceLogManager LogManager;

        internal readonly BacktraceBreadcrumbsEventHandler EventHandler;

        /// <summary>
        /// Determine if breadcrumbs are enabled
        /// </summary>
        private bool _enabled = false;
        public BacktraceBreadcrumbs(IBacktraceLogManager logManager)
        {
            LogManager = logManager;
            EventHandler = new BacktraceBreadcrumbsEventHandler(this);
        }
        public void UnregisterEvents()
        {
            EventHandler.Unregister();
        }

        public bool ClearBreadcrumbs()
        {
            return LogManager.Clear();
        }

        public bool EnableBreadcrumbs(BacktraceBreadcrumbType level, UnityEngineLogLevel unityLogLevel)
        {
            if (_enabled)
            {
                return false;
            }
            BreadcrumbsLevel = level;
            UnityLogLevel = unityLogLevel;

            var breadcrumbStorageEnabled = LogManager.Enable();
            if (!breadcrumbStorageEnabled)
            {
                return false;
            }
            EventHandler.Register(level);
            return true;
        }

        public bool FromBacktrace(BacktraceReport report)
        {
            var type = report.ExceptionTypeReport ? UnityEngineLogLevel.Error : UnityEngineLogLevel.Info;
            if (!ShouldLog(type))
            {
                return false;
            }
            return AddBreadcrumbs(
                report.Message,
                BreadcrumbLevel.System,
                type,
                null);
        }

        public bool FromMonoBehavior(string message, LogType type, IDictionary<string, string> attributes)
        {
            return AddBreadcrumbs(message, BreadcrumbLevel.System, ConvertLogTypeToLogLevel(type), attributes);
        }

        public string GetBreadcrumbLogPath()
        {
            return LogManager.BreadcrumbsFilePath;
        }

        public bool Info(string message)
        {
            return Log(message, LogType.Log, null);
        }
        public bool Info(string message, IDictionary<string, string> attributes)
        {
            return Log(message, LogType.Log, attributes);
        }
        public bool Warning(string message)
        {
            return Log(message, LogType.Warning, null);
        }
        public bool Warning(string message, IDictionary<string, string> attributes)
        {
            return Log(message, LogType.Warning, attributes);
        }
        public bool Debug(string message, IDictionary<string, string> attributes)
        {
            return Log(message, LogType.Assert, attributes);
        }
        public bool Debug(string message)
        {
            return Log(message, LogType.Assert);
        }
        public bool Exception(string message)
        {
            return Log(message, LogType.Exception, null);
        }
        public bool Exception(Exception exception, IDictionary<string, string> attributes)
        {
            return Log(exception.Message, LogType.Exception, attributes);
        }
        public bool Exception(Exception exception)
        {
            return Log(exception.Message, LogType.Exception, null);
        }
        public bool Exception(string message, IDictionary<string, string> attributes)
        {
            return Log(message, LogType.Exception, attributes);
        }
        public bool Log(string message, LogType type)
        {
            return Log(message, type, null);
        }
        public bool Log(string message, LogType logType, IDictionary<string, string> attributes)
        {
            var type = ConvertLogTypeToLogLevel(logType);
            if (!ShouldLog(type))
            {
                return false;
            }
            return AddBreadcrumbs(message, BreadcrumbLevel.Manual, type, attributes);
        }
        internal bool AddBreadcrumbs(string message, BreadcrumbLevel level, UnityEngineLogLevel type, IDictionary<string, string> attributes = null)
        {
            if (!BreadcrumbsLevel.HasFlag((BacktraceBreadcrumbType)level))
            {
                return false;
            }
            return LogManager.Add(message, level, type, attributes);
        }
        internal bool ShouldLog(UnityEngineLogLevel type)
        {
            if (!BreadcrumbsLevel.HasFlag(BacktraceBreadcrumbType.Manual))
            {
                return false;
            }
            return UnityLogLevel.HasFlag(type);
        }

        internal UnityEngineLogLevel ConvertLogTypeToLogLevel(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return UnityEngineLogLevel.Warning;
                case LogType.Error:
                case LogType.Exception:
                    return UnityEngineLogLevel.Error;
                case LogType.Assert:
                    return UnityEngineLogLevel.Debug;
                case LogType.Log:
                default:
                    return UnityEngineLogLevel.Info;
            }
        }

        public double BreadcrumbId()
        {
            return LogManager.BreadcrumbId();
        }

        public void Update()
        {
            EventHandler.Update();
        }
    }
}
