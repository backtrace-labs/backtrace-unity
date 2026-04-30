using Backtrace.Unity.Types;
using Backtrace.Unity.WebGL;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    internal sealed class BacktraceUnityLogReportFactory
    {
        private readonly BacktraceConfiguration _configuration;

        internal BacktraceUnityLogReportFactory(BacktraceConfiguration configuration)
        {
            _configuration = configuration;
        }

        internal BacktraceReport CreateReport(
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath,
            BacktraceUnityLogExceptionCandidate candidate)
        {
            if (candidate != null && type == LogType.Exception)
            {
                return CreateFromCandidate(
                    candidate,
                    message,
                    stackTrace,
                    type,
                    isMainThread,
                    capturePath);
            }
            return CreateFromUnityCallback(
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
        }

        private BacktraceReport CreateFromUnityCallback(
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            var exception = BacktraceUnhandledException.CreateFromUnityLogCallback(
                message,
                stackTrace,
                type,
                allowEnvironmentStackFallback: false);
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
            attributes["backtrace.unity.stack_source"] =
                string.IsNullOrEmpty(stackTrace)
                    ? BacktraceUnityLogCapture.StackSourceUnavailable
                    : BacktraceUnityLogCapture.StackSourceUnityCallback;
            var report = BacktraceReport.CreateWithoutEnvironmentStackFallback(
                exception,
                attributes);
            report.Attributes["error.message"] = message;
            AddUnityLogAnnotation(
                report,
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
            FinalizeStacklessClassification(report, stackTrace, type);
            AttachWebGLJavaScriptStackIfNeeded(report, stackTrace, type);
            return report;
        }

        private BacktraceReport CreateFromCandidate(
            BacktraceUnityLogExceptionCandidate candidate,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            if (candidate.Exception != null &&
                !string.IsNullOrEmpty(candidate.Exception.StackTrace))
            {
                var originalReport = CreateFromOriginalExceptionStack(
                    candidate,
                    message,
                    stackTrace,
                    type,
                    isMainThread,
                    capturePath);
                if (HasFrames(originalReport))
                {
                    AddCandidateAnnotations(
                        originalReport,
                        candidate,
                        message,
                        stackTrace,
                        type,
                        isMainThread,
                        capturePath);
                    FinalizeStacklessClassification(originalReport, stackTrace, type);
                    AttachWebGLJavaScriptStackIfNeeded(originalReport, stackTrace, type);
                    return originalReport;
                }
            }

            if (!string.IsNullOrEmpty(stackTrace))
            {
                var callbackReport =
                    CreateFromUnityCallbackStackWithOriginalExceptionMetadata(
                        candidate,
                        message,
                        stackTrace,
                        type,
                        isMainThread,
                        capturePath);
                AddCandidateAnnotations(
                    callbackReport,
                    candidate,
                    message,
                    stackTrace,
                    type,
                    isMainThread,
                    capturePath);
                FinalizeStacklessClassification(callbackReport, stackTrace, type);
                AttachWebGLJavaScriptStackIfNeeded(callbackReport, stackTrace, type);
                return callbackReport;
            }

            var stacklessReport = CreateStacklessCandidateReport(
                candidate,
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
            AddCandidateAnnotations(
                stacklessReport,
                candidate,
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
            FinalizeStacklessClassification(stacklessReport, stackTrace, type);
            AttachWebGLJavaScriptStackIfNeeded(stacklessReport, stackTrace, type);
            return stacklessReport;
        }

        private BacktraceReport CreateFromOriginalExceptionStack(
            BacktraceUnityLogExceptionCandidate candidate,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                message,
                stackTrace,
                type,
                isMainThread,
                BacktraceUnityLogCapture.CreateLogHandlerAndCallbackCapturePath(capturePath));
            BacktraceUnityLogCapture.MergeAttributes(
                attributes,
                BacktraceUnityLogCapture.CreateOriginalExceptionAttributes(
                    candidate.Exception,
                    candidate.ContextName,
                    candidate.IsMainThread));
            attributes["backtrace.unity.stack_source"] =
                BacktraceUnityLogCapture.StackSourceOriginalException;
            var report = BacktraceReport.CreateWithoutEnvironmentStackFallback(
                candidate.Exception,
                attributes);
            report.Attributes["error.type"] =
                BacktraceDefaultClassifierTypes.UnhandledExceptionType;
            report.Attributes["error.message"] = message;
            return report;
        }

        private BacktraceReport CreateFromUnityCallbackStackWithOriginalExceptionMetadata(
            BacktraceUnityLogExceptionCandidate candidate,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            var exception = BacktraceUnhandledException.CreateFromUnityLogCallback(
                message,
                stackTrace,
                type,
                allowEnvironmentStackFallback: false);
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                message,
                stackTrace,
                type,
                isMainThread,
                BacktraceUnityLogCapture.CreateLogHandlerAndCallbackCapturePath(capturePath));
            BacktraceUnityLogCapture.MergeAttributes(
                attributes,
                BacktraceUnityLogCapture.CreateOriginalExceptionAttributes(
                    candidate.Exception,
                    candidate.ContextName,
                    candidate.IsMainThread));
            attributes["backtrace.unity.stack_source"] =
                BacktraceUnityLogCapture.StackSourceUnityCallback;
            var report = BacktraceReport.CreateWithoutEnvironmentStackFallback(
                exception,
                attributes);
            report.Attributes["error.message"] = message;
            return report;
        }

        private BacktraceReport CreateStacklessCandidateReport(
            BacktraceUnityLogExceptionCandidate candidate,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                message,
                stackTrace,
                type,
                isMainThread,
                BacktraceUnityLogCapture.CreateLogHandlerAndCallbackCapturePath(capturePath));
            BacktraceUnityLogCapture.MergeAttributes(
                attributes,
                BacktraceUnityLogCapture.CreateOriginalExceptionAttributes(
                    candidate.Exception,
                    candidate.ContextName,
                    candidate.IsMainThread));
            attributes["backtrace.unity.stack_source"] =
                BacktraceUnityLogCapture.StackSourceUnavailable;
            var report = BacktraceReport.CreateWithoutEnvironmentStackFallback(
                candidate.Exception,
                attributes);
            report.Attributes["error.type"] =
                BacktraceDefaultClassifierTypes.UnhandledExceptionType;
            report.Attributes["error.message"] = message;
            return report;
        }

        private static void AddCandidateAnnotations(
            BacktraceReport report,
            BacktraceUnityLogExceptionCandidate candidate,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            AddUnityLogAnnotation(
                report,
                message,
                stackTrace,
                type,
                isMainThread,
                capturePath);
            report.AddAnnotation(
                BacktraceUnityLogCapture.UnityLogHandlerExceptionAnnotationName,
                BacktraceUnityLogCapture.CreateOriginalExceptionAnnotation(
                    candidate.Exception,
                    candidate.ContextName));
        }

        private static void AddUnityLogAnnotation(
            BacktraceReport report,
            string message,
            string stackTrace,
            LogType type,
            bool isMainThread,
            string capturePath)
        {
            report.AddAnnotation(
                BacktraceUnityLogCapture.UnityLogCaptureAnnotationName,
                BacktraceUnityLogCapture.CreateUnityLogAnnotation(
                    message,
                    stackTrace,
                    type,
                    isMainThread,
                    capturePath));
        }

        private static void FinalizeStacklessClassification(
            BacktraceReport report,
            string stackTrace,
            LogType type)
        {
            if (report == null ||
                !BacktraceUnityLogCapture.IsReportableUnityLogType(type))
            {
                return;
            }
            var hasFrames = HasFrames(report);
            report.Attributes["backtrace.unity.report.frames.empty"] =
                BacktraceUnityLogCapture.ToInvariantString(!hasFrames);
            if (hasFrames)
            {
                return;
            }
            string stackSource;
            report.Attributes.TryGetValue(
                "backtrace.unity.stack_source",
                out stackSource);
            if (stackSource == BacktraceUnityLogCapture.StackSourceOriginalException)
            {
                report.Attributes["backtrace.unity.stackless.reason"] =
                    BacktraceUnityLogCapture.StacklessReasonOriginalExceptionUnparsed;
                return;
            }
            if (stackSource == BacktraceUnityLogCapture.StackSourceUnityCallback &&
                !string.IsNullOrEmpty(stackTrace))
            {
                report.Attributes["backtrace.unity.stackless.reason"] =
                    BacktraceUnityLogCapture.StacklessReasonUnityCallbackUnparsed;
                return;
            }
            if (string.IsNullOrEmpty(stackTrace))
            {
                report.Attributes["backtrace.unity.stackless.reason"] =
                    BacktraceUnityLogCapture.StacklessReasonUnityLogCallback;
            }
            else if (!report.Attributes.ContainsKey("backtrace.unity.stackless.reason"))
            {
                report.Attributes["backtrace.unity.stackless.reason"] =
                    BacktraceUnityLogCapture.StacklessReasonUnityCallbackUnparsed;
            }
        }

        private static bool HasFrames(BacktraceReport report)
        {
            return report != null &&
                report.DiagnosticStack != null &&
                report.DiagnosticStack.Count != 0;
        }

        private void AttachWebGLJavaScriptStackIfNeeded(
            BacktraceReport report,
            string unityStackTrace,
            LogType type)
        {
#if UNITY_WEBGL
            if (report == null ||
                _configuration == null ||
                _configuration.WebGLJavaScriptStackFallback ==
                    BacktraceWebGLJavaScriptStackFallbackMode.Disabled)
            {
                return;
            }
            if (!BacktraceUnityLogCapture.IsStacklessUnityLogReport(unityStackTrace, type))
            {
                return;
            }
            if (HasFrames(report))
            {
                return;
            }
            var javascriptStack = BacktraceWebGLJavaScriptStack.Capture();
            var hasJavaScriptStack = !string.IsNullOrEmpty(javascriptStack);
            report.Attributes["backtrace.webgl.javascript_stack.present"] =
                BacktraceUnityLogCapture.ToInvariantString(hasJavaScriptStack);
            report.Attributes["backtrace.webgl.javascript_stack.kind"] =
                "javascript_stack_at_backtrace_capture_time";
            if (!hasJavaScriptStack)
            {
                return;
            }
            report.AddAnnotation(
                BacktraceWebGLJavaScriptStack.AnnotationName,
                BacktraceWebGLJavaScriptStack.CreateAnnotation(javascriptStack));
#endif
        }
    }
}
