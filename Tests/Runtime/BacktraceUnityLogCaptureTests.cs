using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public sealed class BacktraceUnityLogCaptureTests
    {
        [Test]
        public void UnityLogAttributes_ShouldClassifyEmptyExceptionStack()
        {
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty,
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived);

            Assert.AreEqual(
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived,
                attributes["backtrace.unity.capture_path"]);
            Assert.AreEqual("Exception", attributes["backtrace.unity.log.type"]);
            Assert.AreEqual("true", attributes["backtrace.unity.log.stacktrace.empty"]);
            Assert.AreEqual("0", attributes["backtrace.unity.log.stacktrace.length"]);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StacklessReasonUnityLogCallback,
                attributes["backtrace.unity.stackless.reason"]);
        }

        [Test]
        public void UnityLogAttributes_ShouldNotClassifyNonEmptyStackAsStackless()
        {
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                "ArgumentNullException: Value cannot be null.",
                "SomeClass.SomeMethod() (at Assets/Test.cs:10)",
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived);

            Assert.AreEqual("false", attributes["backtrace.unity.log.stacktrace.empty"]);
            Assert.False(attributes.ContainsKey("backtrace.unity.stackless.reason"));
        }

        [Test]
        public void LogHandlerAttributes_ShouldRecordOriginalExceptionType()
        {
            var exception = new ArgumentNullException("obj");
            var attributes = BacktraceUnityLogCapture.CreateLogHandlerExceptionAttributes(
                exception,
                "TestObject",
                true);

            Assert.AreEqual(
                BacktraceUnityLogCapture.CapturePathUnityLogHandlerLogException,
                attributes["backtrace.unity.capture_path"]);
            Assert.AreEqual(
                typeof(ArgumentNullException).FullName,
                attributes["backtrace.unity.original_exception.type"]);
            Assert.AreEqual(
                "TestObject",
                attributes["backtrace.unity.original_exception.context_name"]);
        }

        [Test]
        public void ExceptionMessagePrefixes_ShouldIncludeShortAndFullTypeNames()
        {
            var exception = new ArgumentNullException("obj");
            var prefixes = BacktraceUnityLogCapture.CreateExceptionMessagePrefixes(exception);

            Assert.Contains("ArgumentNullException", prefixes);
            Assert.Contains(typeof(ArgumentNullException).FullName, prefixes);
            Assert.Contains("ArgumentNullException: Value cannot be null.\nParameter name: obj", prefixes);
        }

        [Test]
        public void AssignSourceCodeToReport_ShouldPreserveSourceCodeWhenStackIsEmpty()
        {
            var exception = new BacktraceUnhandledException(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty);
            var report = new BacktraceReport(exception);
            report.DiagnosticStack = new List<BacktraceStackFrame>();
            report.AssignSourceCodeToReport("recent Unity log context");

            var data = report.ToBacktraceData(null, -1);
            var json = data.ToJson();

            Assert.That(json, Does.Contain("\"sourceCode\""));
            Assert.That(json, Does.Contain("recent Unity log context"));
        }

        [Test]
        public void AddAnnotation_ShouldSerializeCustomAnnotation()
        {
            var exception = new BacktraceUnhandledException(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty);
            var report = new BacktraceReport(exception);
            report.AddAnnotation(
                "Unity log capture",
                new Dictionary<string, string>
                {
                    { "capturePath", BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived },
                    { "stackTraceEmpty", "true" }
                });

            var data = report.ToBacktraceData(null, -1);
            var json = data.ToJson();

            Assert.That(json, Does.Contain("Unity log capture"));
            Assert.That(json, Does.Contain("stackTraceEmpty"));
            Assert.That(json, Does.Contain(BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived));
        }
    }
}
