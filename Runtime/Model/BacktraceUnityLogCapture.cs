using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    internal static class BacktraceUnityLogCapture
    {
        internal const string UnityLogCaptureAnnotationName = "Unity log capture";
        internal const string UnityLogHandlerExceptionAnnotationName =
            "Unity log handler exception";
        internal const string CapturePathUnityLogMessageReceived =
            "Application.logMessageReceived";
        internal const string CapturePathUnityLogMessageReceivedThreaded =
            "Application.logMessageReceivedThreaded";
        internal const string CapturePathUnityLogHandler =
            "Debug.unityLogger.logHandler.LogException";

        internal const string CapturePathUnityLogHandlerAndCallback =
            "Debug.unityLogger.logHandler.LogException+Application.logMessageReceived";

        internal static string CreateLogHandlerAndCallbackCapturePath(
            string callbackCapturePath)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}+{1}",
                CapturePathUnityLogHandler,
                string.IsNullOrEmpty(callbackCapturePath)
                    ? CapturePathUnityLogMessageReceived
                    : callbackCapturePath);
        }

        internal static bool IsLogHandlerAndCallbackCapturePath(string capturePath)
        {
            return !string.IsNullOrEmpty(capturePath) &&
                capturePath.StartsWith(
                    CapturePathUnityLogHandler + "+",
                    StringComparison.Ordinal);
        }
        internal const string StacklessReasonUnityLogCallback =
            "unity_log_callback_empty_stacktrace";
        internal const string StacklessReasonUnityCallbackUnparsed =
            "unity_log_callback_stacktrace_unparsed";
        internal const string StacklessReasonOriginalException =
            "original_exception_without_managed_stacktrace";
        internal const string StacklessReasonOriginalExceptionUnparsed =
            "original_exception_stacktrace_unparsed";
        internal const string StackSourceOriginalException =
            "original_exception_stacktrace";
        internal const string StackSourceUnityCallback =
            "unity_log_callback_stacktrace";
        internal const string StackSourceUnavailable =
            "unavailable";

        private const int MaxAnnotationValueLength = 16 * 1024;

        internal static bool IsReportableUnityLogType(LogType type)
        {
            return type == LogType.Error || type == LogType.Exception;
        }

        internal static bool IsStacklessUnityLogReport(string stackTrace, LogType type)
        {
            return IsReportableUnityLogType(type) && string.IsNullOrEmpty(stackTrace);
        }

        internal static Dictionary<string, string> CreateUnityLogAttributes(
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            var stackTraceLength = string.IsNullOrEmpty(stackTrace) ? 0 : stackTrace.Length;
            var messageLength = string.IsNullOrEmpty(message) ? 0 : message.Length;
            var attributes = new Dictionary<string, string>
            {
                { "backtrace.unity.capture_path", capturePath ?? string.Empty },
                { "backtrace.unity.log.type", type.ToString() },
                { "backtrace.unity.log.stacktrace.empty", ToInvariantString(stackTraceLength == 0) },
                { "backtrace.unity.log.stacktrace.length", stackTraceLength.ToString(CultureInfo.InvariantCulture) },
                { "backtrace.unity.log.message.length", messageLength.ToString(CultureInfo.InvariantCulture) },
                { "backtrace.unity.log.thread.id", Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) },
                { "backtrace.unity.log.thread.is_main", ToInvariantString(isMainThread) },
                { "backtrace.unity.stacktrace_log_type.error", GetStackTraceLogType(LogType.Error, isMainThread) },
                { "backtrace.unity.stacktrace_log_type.exception", GetStackTraceLogType(LogType.Exception, isMainThread) },
            };
#if UNITY_WEBGL
            attributes["backtrace.unity.platform.webgl"] = "true";
#else
            attributes["backtrace.unity.platform.webgl"] = "false";
#endif
#if ENABLE_IL2CPP
            attributes["backtrace.unity.scripting_backend"] = "IL2CPP";
#else
            attributes["backtrace.unity.scripting_backend"] = "Mono";
#endif
            if (IsStacklessUnityLogReport(stackTrace, type))
            {
                attributes["backtrace.unity.stackless.reason"] =
                    StacklessReasonUnityLogCallback;
            }
            return attributes;
        }

        internal static Dictionary<string, string> CreateUnityLogAnnotation(
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            return new Dictionary<string, string>
            {
                { "capturePath", capturePath ?? string.Empty },
                { "logType", type.ToString() },
                { "message", Truncate(message) },
                { "stackTrace", Truncate(stackTrace) },
                { "stackTraceEmpty", ToInvariantString(string.IsNullOrEmpty(stackTrace)) },
                { "threadId", Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) },
                { "isMainThread", ToInvariantString(isMainThread) },
                { "stackTraceLogType.Error", GetStackTraceLogType(LogType.Error, isMainThread) },
                { "stackTraceLogType.Exception", GetStackTraceLogType(LogType.Exception, isMainThread) },
                {
                    "note",
                    "Unity supplied this data to Application.logMessageReceived/logMessageReceivedThreaded. " +
                    "If stackTrace is empty, Backtrace cannot reconstruct the original managed throw-site stack from this callback alone."
                }
            };
        }

        internal static Dictionary<string, string> CreateOriginalExceptionAttributes(
            Exception exception,
            string contextName,
            bool isMainThread)
        {
            var exceptionType = exception == null ? string.Empty : exception.GetType().FullName;
            var stackPresent = exception != null && !string.IsNullOrEmpty(exception.StackTrace);
            var attributes = new Dictionary<string, string>
            {
                { "backtrace.unity.original_exception.source", "Debug.unityLogger.logHandler" },
                { "backtrace.unity.original_exception.type", exceptionType },
                { "backtrace.unity.original_exception.stack_present", ToInvariantString(stackPresent) },
                { "backtrace.unity.original_exception.context_name", contextName ?? string.Empty },
                { "backtrace.unity.original_exception.thread.id", Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) },
                { "backtrace.unity.original_exception.thread.is_main", ToInvariantString(isMainThread) }
            };
            if (!stackPresent)
            {
                attributes["backtrace.unity.original_exception.stackless_reason"] =
                    StacklessReasonOriginalException;
            }
            return attributes;
        }

        internal static Dictionary<string, string> CreateOriginalExceptionAnnotation(
            Exception exception,
            string contextName)
        {
            return new Dictionary<string, string>
            {
                { "source", "Debug.unityLogger.logHandler" },
                { "exceptionType", exception == null ? string.Empty : exception.GetType().FullName },
                { "exceptionMessage", exception == null ? string.Empty : Truncate(exception.Message) },
                { "exceptionStackTrace", exception == null ? string.Empty : Truncate(exception.StackTrace) },
                { "exceptionStackTraceEmpty", ToInvariantString(exception == null || string.IsNullOrEmpty(exception.StackTrace)) },
                { "contextName", contextName ?? string.Empty },
                {
                    "note",
                    "Backtrace observed this Exception object through Debug.unityLogger.logHandler before Unity emitted the log callback."
                }
            };
        }

        internal static List<string> CreateExceptionMessagePrefixes(Exception exception)
        {
            var result = new List<string>();
            if (exception == null)
            {
                return result;
            }
            var type = exception.GetType();
            var message = exception.Message ?? string.Empty;
            AddPrefix(result, type.Name, message);
            AddPrefix(result, type.FullName, message);
            return result;
        }

        internal static string NormalizeUnityExceptionMessage(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }
            var message = exception.Message ?? string.Empty;
            return string.IsNullOrEmpty(message)
                ? exception.GetType().Name
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1}",
                    exception.GetType().Name,
                    message);
        }

        internal static string ToInvariantString(bool value)
        {
            return value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        internal static void MergeAttributes(
            IDictionary<string, string> target,
            IDictionary<string, string> source)
        {
            if (target == null || source == null)
            {
                return;
            }
            foreach (var item in source)
            {
                target[item.Key] = item.Value ?? string.Empty;
            }
        }

        private static void AddPrefix(
            ICollection<string> result,
            string typeName,
            string message)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return;
            }
            result.Add(typeName);
            if (!string.IsNullOrEmpty(message))
            {
                result.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1}",
                    typeName,
                    message));
            }
        }

        private static string GetStackTraceLogType(LogType logType, bool canCallUnityApi)
        {
            if (!canCallUnityApi)
            {
                return "unavailable-background-thread";
            }
            try
            {
                return Application.GetStackTraceLogType(logType).ToString();
            }
            catch (Exception exception)
            {
                return "unavailable-" + exception.GetType().Name;
            }
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Length <= MaxAnnotationValueLength
                ? value
                : value.Substring(0, MaxAnnotationValueLength) + "\n...[truncated]";
        }
    }
}
