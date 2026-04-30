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
        public void UnityLogAttributes_ShouldClassifyEmptyCallbackStack()
        {
            var attributes = BacktraceUnityLogCapture.CreateUnityLogAttributes(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty,
                LogType.Exception,
                true,
                BacktraceUnityLogCapture.CapturePathUnityLogMessageReceived);

            Assert.AreEqual("Exception", attributes["backtrace.unity.log.type"]);
            Assert.AreEqual("true", attributes["backtrace.unity.log.stacktrace.empty"]);
            Assert.AreEqual("0", attributes["backtrace.unity.log.stacktrace.length"]);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StacklessReasonUnityLogCallback,
                attributes["backtrace.unity.stackless.reason"]);
        }

        [Test]
        public void UnityLogAttributes_ShouldNotClassifyNonEmptyCallbackStackAsStackless()
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
        public void OriginalExceptionAttributes_ShouldRecordThrownExceptionStackPresence()
        {
            Exception exception = null;
            try
            {
                throw new ArgumentNullException("obj");
            }
            catch (Exception caught)
            {
                exception = caught;
            }
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var attributes = BacktraceUnityLogCapture.CreateOriginalExceptionAttributes(
                exception,
                "TestContext",
                true,
                threadId);

            Assert.AreEqual(
                typeof(ArgumentNullException).FullName,
                attributes["backtrace.unity.original_exception.type"]);
            Assert.AreEqual(
                "true",
                attributes["backtrace.unity.original_exception.stack_present"]);
            Assert.AreEqual(
                threadId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                attributes["backtrace.unity.original_exception.thread.id"]);
        }

        [Test]
        public void OriginalExceptionAttributes_ShouldRecordStacklessOriginalException()
        {
            var exception = new ArgumentNullException("obj");
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var attributes = BacktraceUnityLogCapture.CreateOriginalExceptionAttributes(
                exception,
                "TestContext",
                true,
                threadId);

            Assert.AreEqual(
                "false",
                attributes["backtrace.unity.original_exception.stack_present"]);
            Assert.AreEqual(
                BacktraceUnityLogCapture.StacklessReasonOriginalException,
                attributes["backtrace.unity.original_exception.stackless_reason"]);
        }

        [Test]
        public void ExceptionMessagePrefixes_ShouldUseRuntimeExceptionMessage()
        {
            var exception = new ArgumentNullException("obj");
            var prefixes = BacktraceUnityLogCapture.CreateExceptionMessagePrefixes(exception);

            Assert.Contains("ArgumentNullException: " + exception.Message, prefixes);
            Assert.Contains(typeof(ArgumentNullException).FullName + ": " + exception.Message, prefixes);
            Assert.False(prefixes.Contains("ArgumentNullException"));
            Assert.False(prefixes.Contains(typeof(ArgumentNullException).FullName));
        }

        [Test]
        public void ExceptionMessagePrefixes_ShouldUseTypeOnlyWhenMessageIsEmpty()
        {
            var exception = new EmptyMessageException();
            var prefixes = BacktraceUnityLogCapture.CreateExceptionMessagePrefixes(exception);

            Assert.Contains(nameof(EmptyMessageException), prefixes);
            Assert.Contains(typeof(EmptyMessageException).FullName, prefixes);
        }

        private sealed class EmptyMessageException : Exception
        {
            public override string Message
            {
                get { return string.Empty; }
            }
        }

        [Test]
        public void AssignSourceCodeToReport_ShouldPreserveSourceCodeWhenStackIsEmpty()
        {
            var exception = BacktraceUnhandledException.CreateFromUnityLogCallback(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty,
                LogType.Exception,
                allowEnvironmentStackFallback: false);
            var report = BacktraceReport.CreateWithoutEnvironmentStackFallback(
                exception,
                new Dictionary<string, string>());
            report.AssignSourceCodeToReport("recent Unity log context");

            var data = report.ToBacktraceData(null, -1);
            var json = data.ToJson();

            Assert.That(json, Does.Contain("\"sourceCode\""));
            Assert.That(json, Does.Contain("recent Unity log context"));
        }

        [Test]
        public void CustomAnnotation_ShouldSerialize()
        {
            var exception = BacktraceUnhandledException.CreateFromUnityLogCallback(
                "ArgumentNullException: Value cannot be null.\nParameter name: obj",
                string.Empty,
                LogType.Exception,
                allowEnvironmentStackFallback: false);
            var report = BacktraceReport.CreateWithoutEnvironmentStackFallback(
                exception,
                new Dictionary<string, string>());
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
