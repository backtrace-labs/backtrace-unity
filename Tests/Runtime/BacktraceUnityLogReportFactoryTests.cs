using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public sealed class BacktraceUnityLogReportFactoryTests
    {
        [Test]
        public void StacklessOriginalExceptionAndEmptyUnityCallback_ShouldProduceZeroFrames()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.WebGLJavaScriptStackFallback =
                BacktraceWebGLJavaScriptStackFallbackMode.Disabled;
            var factory = new BacktraceUnityLogReportFactory(configuration);
            var exception = new ArgumentNullException("obj");
            var candidate = new BacktraceUnityLogExceptionCandidate
            {
                Exception = exception,
                ContextName = "TestContext",
                IsMainThread = true,
                ThreadId = 1
            };

            var report = factory.CreateReport(
                "ArgumentNullException: " + exception.Message,
                string.Empty,
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived,
                candidate);

            Assert.NotNull(report.DiagnosticStack);
            Assert.AreEqual(0, report.DiagnosticStack.Count);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StackSourceUnavailable,
                report.Attributes["backtrace.unity.stack_source"]);
            Assert.AreEqual(
                "false",
                report.Attributes["backtrace.unity.original_exception.stack_present"]);
            Assert.AreEqual(
                "true",
                report.Attributes["backtrace.unity.report.frames.empty"]);
        }

        [Test]
        public void ThrownOriginalException_ShouldUseOriginalExceptionStack()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var factory = new BacktraceUnityLogReportFactory(configuration);
            Exception exception = null;
            try
            {
                throw new ArgumentNullException("obj");
            }
            catch (Exception caught)
            {
                exception = caught;
            }
            var candidate = new BacktraceUnityLogExceptionCandidate
            {
                Exception = exception,
                ContextName = "TestContext",
                IsMainThread = true,
                ThreadId = 1
            };

            var report = factory.CreateReport(
                "ArgumentNullException: " + exception.Message,
                string.Empty,
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived,
                candidate);

            Assert.NotNull(report.DiagnosticStack);
            Assert.Greater(report.DiagnosticStack.Count, 0);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StackSourceOriginalException,
                report.Attributes["backtrace.unity.stack_source"]);
            Assert.AreEqual(
                "true",
                report.Attributes["backtrace.unity.original_exception.stack_present"]);
        }

        [Test]
        public void StacklessOriginalExceptionAndUnityCallbackStack_ShouldUseUnityCallbackStack()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var factory = new BacktraceUnityLogReportFactory(configuration);
            var exception = new ArgumentNullException("obj");
            var candidate = new BacktraceUnityLogExceptionCandidate
            {
                Exception = exception,
                ContextName = "TestContext",
                IsMainThread = true,
                ThreadId = 1
            };

            var report = factory.CreateReport(
                "ArgumentNullException: " + exception.Message,
                "ExampleClass.DoWork() (at Assets/ExampleClass.cs:42)",
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived,
                candidate);

            Assert.AreEqual(
                BacktraceUnityLogCapture.StackSourceUnityCallback,
                report.Attributes["backtrace.unity.stack_source"]);
            Assert.AreEqual(
                "false",
                report.Attributes["backtrace.unity.original_exception.stack_present"]);
        }

        [Test]
        public void UnityCallbackOnlyWithEmptyStack_ShouldProduceStacklessDiagnostics()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var factory = new BacktraceUnityLogReportFactory(configuration);

            var report = factory.CreateReport(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty,
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived,
                null);

            Assert.NotNull(report.DiagnosticStack);
            Assert.AreEqual(0, report.DiagnosticStack.Count);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StacklessReasonUnityLogCallback,
                report.Attributes["backtrace.unity.stackless.reason"]);
            Assert.AreEqual(
                "true",
                report.Attributes["backtrace.unity.report.frames.empty"]);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StackSourceUnavailable,
                report.Attributes["backtrace.unity.stack_source"]);
        }

        [Test]
        public void UnityCallbackOnlyWithStack_ShouldRecordUnityCallbackStackSource()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            var factory = new BacktraceUnityLogReportFactory(configuration);

            var report = factory.CreateReport(
                "ArgumentNullException: Value cannot be null.",
                "ExampleClass.DoWork() (at Assets/ExampleClass.cs:42)",
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived,
                null);

            Assert.AreEqual(
                BacktraceUnityLogCapture.StackSourceUnityCallback,
                report.Attributes["backtrace.unity.stack_source"]);
        }

        [Test]
        public void JavaScriptStackFallback_ShouldBeDisabledByDefault()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            Assert.AreEqual(
                BacktraceWebGLJavaScriptStackFallbackMode.Disabled,
                configuration.WebGLJavaScriptStackFallback);
        }
    }
}
