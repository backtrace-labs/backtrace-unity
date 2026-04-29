#if BACKTRACE_UNITY_ADDRESSABLES
using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Backtrace.Unity.Integrations
{
    public static class BacktraceAddressablesIntegration
    {
        private const string CapturePath = "Addressables.ResourceManager.ExceptionHandler";
        private const string AnnotationName = "Unity Addressables operation";

        private static bool _enabled;
        private static BacktraceClient _client;
        private static BacktraceAddressablesOptions _options;
        private static Action<AsyncOperationHandle, Exception> _previousHandler;

        public static void Enable(
            BacktraceClient client,
            BacktraceAddressablesOptions options = null)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }
            if (_enabled)
            {
                return;
            }
            _enabled = true;
            _client = client;
            _options = options ?? new BacktraceAddressablesOptions();
            _previousHandler = Addressables.ResourceManager.ExceptionHandler;
            Addressables.ResourceManager.ExceptionHandler = HandleAddressablesException;
        }

        public static void Disable()
        {
            if (!_enabled)
            {
                return;
            }
            Addressables.ResourceManager.ExceptionHandler = _previousHandler;
            _enabled = false;
            _client = null;
            _options = null;
            _previousHandler = null;
        }

        private static void HandleAddressablesException(
            AsyncOperationHandle operation,
            Exception exception)
        {
            var client = _client != null ? _client : BacktraceClient.Instance;
            var options = _options ?? new BacktraceAddressablesOptions();
            if (client != null && exception != null)
            {
                SendBacktraceReport(client, operation, exception, options);
            }
            if (options.ForwardToUnityLogging)
            {
                ForwardToUnityLogging(client, operation, exception, options);
            }
        }

        private static void SendBacktraceReport(
            BacktraceClient client,
            AsyncOperationHandle operation,
            Exception exception,
            BacktraceAddressablesOptions options)
        {
            var attributes = CreateAttributes(operation, exception);
            var report = new BacktraceReport(exception, attributes);
            report.AddAnnotation(
                AnnotationName,
                CreateAnnotation(operation, exception, options));
            client.Send(report);
        }

        private static Dictionary<string, string> CreateAttributes(
            AsyncOperationHandle operation,
            Exception exception)
        {
            var attributes = new Dictionary<string, string>
            {
                { "backtrace.unity.capture_path", CapturePath },
                { "unity.addressables.exception.type", exception.GetType().FullName },
                { "unity.addressables.exception.stack_present", ToInvariantString(!string.IsNullOrEmpty(exception.StackTrace)) }
            };
            if (string.IsNullOrEmpty(exception.StackTrace))
            {
                attributes["backtrace.unity.stackless.reason"] =
                    "addressables_exception_without_managed_stack";
            }
            TryAdd(attributes, "unity.addressables.operation.debug_name", delegate { return operation.DebugName; });
            TryAdd(attributes, "unity.addressables.operation.status", delegate { return operation.Status.ToString(); });
            TryAdd(attributes, "unity.addressables.operation.is_done", delegate { return ToInvariantString(operation.IsDone); });
            TryAdd(attributes, "unity.addressables.operation.is_valid", delegate { return ToInvariantString(operation.IsValid()); });
            TryAdd(attributes, "unity.addressables.operation.percent_complete", delegate
            {
                return operation.PercentComplete.ToString("R", CultureInfo.InvariantCulture);
            });
            TryAdd(attributes, "unity.addressables.operation.exception_present", delegate
            {
                return ToInvariantString(operation.OperationException != null);
            });
            return attributes;
        }

        private static Dictionary<string, string> CreateAnnotation(
            AsyncOperationHandle operation,
            Exception exception,
            BacktraceAddressablesOptions options)
        {
            var maxLength = Math.Max(options.MaxAnnotationValueLength, 1024);
            var annotation = new Dictionary<string, string>
            {
                { "capturePath", CapturePath },
                { "exceptionType", exception.GetType().FullName },
                { "exceptionMessage", Truncate(exception.Message, maxLength) },
                { "exceptionStackTrace", Truncate(exception.StackTrace, maxLength) },
                { "exceptionStackTraceEmpty", ToInvariantString(string.IsNullOrEmpty(exception.StackTrace)) }
            };
            TryAdd(annotation, "operation.DebugName", delegate { return operation.DebugName; });
            TryAdd(annotation, "operation.Status", delegate { return operation.Status.ToString(); });
            TryAdd(annotation, "operation.IsDone", delegate { return ToInvariantString(operation.IsDone); });
            TryAdd(annotation, "operation.IsValid", delegate { return ToInvariantString(operation.IsValid()); });
            TryAdd(annotation, "operation.PercentComplete", delegate
            {
                return operation.PercentComplete.ToString("R", CultureInfo.InvariantCulture);
            });
            TryAdd(annotation, "operation.OperationException.Type", delegate
            {
                return operation.OperationException == null
                    ? string.Empty
                    : operation.OperationException.GetType().FullName;
            });
            TryAdd(annotation, "operation.OperationException.Message", delegate
            {
                return operation.OperationException == null
                    ? string.Empty
                    : Truncate(operation.OperationException.Message, maxLength);
            });
            TryAdd(annotation, "operation.OperationException.StackTrace", delegate
            {
                return operation.OperationException == null
                    ? string.Empty
                    : Truncate(operation.OperationException.StackTrace, maxLength);
            });
            return annotation;
        }

        private static void ForwardToUnityLogging(
            BacktraceClient client,
            AsyncOperationHandle operation,
            Exception exception,
            BacktraceAddressablesOptions options)
        {
            if (exception != null &&
                options.SuppressForwardedUnityLogReport &&
                client != null)
            {
                client.SuppressNextUnityLogReport(exception, LogType.Exception);
            }
            if (_previousHandler != null)
            {
                _previousHandler(operation, exception);
                return;
            }
            Addressables.LogException(operation, exception);
        }

        private static void TryAdd(
            IDictionary<string, string> target,
            string key,
            Func<string> valueFactory)
        {
            try
            {
                target[key] = valueFactory.Invoke() ?? string.Empty;
            }
            catch (Exception exception)
            {
                target[key] = "unavailable-" + exception.GetType().Name;
            }
        }

        private static string ToInvariantString(bool value)
        {
            return value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength) + "\n...[truncated]";
        }
    }
}
#endif
