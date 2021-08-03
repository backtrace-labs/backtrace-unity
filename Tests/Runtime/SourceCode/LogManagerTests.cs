using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public class LogManagerTests
    {

        [Test]
        public void TestLogManagerInitialization_LimitEqualToZero_ShouldBeDisabled()
        {
            var logManager = new BacktraceLogManager(0);
            Assert.IsTrue(logManager.Disabled);
        }

        [Test]
        public void TestLogManagerInitialization_LimitNotEqualToZero_ShouldBeEnabled()
        {
            var logManager = new BacktraceLogManager(1);
            Assert.IsFalse(logManager.Disabled);
        }

        [Test]
        public void TestDisabledManager_AddLogToDisabledManager_ShouldntEnqueueMessage()
        {
            var logManager = new BacktraceLogManager(0);
            logManager.Enqueue("fake message", string.Empty, LogType.Log);
            Assert.AreEqual(0, logManager.Size);
        }

        [Test]
        public void TestMessageQueue_AddLogToEnabledManager_ShouldEnqueueMessage()
        {
            uint expectedNumberOfMessages = 1;
            var logManager = new BacktraceLogManager(expectedNumberOfMessages);
            logManager.Enqueue("fake message", string.Empty, LogType.Log);
            Assert.AreEqual(expectedNumberOfMessages, logManager.Size);
        }

        [Test]
        public void TestMessageQueue_AddMultipleLosgToEnabledManager_ShouldEnqueueLimitMessage()
        {
            uint expectedNumberOfMessages = 1;
            var logManager = new BacktraceLogManager(expectedNumberOfMessages);

            logManager.Enqueue("deleted message", string.Empty, LogType.Log);
            var enqueuedMessage = "enqueued message";
            logManager.Enqueue(enqueuedMessage, string.Empty, LogType.Log);

            Assert.AreEqual(expectedNumberOfMessages, logManager.Size);
            // validate if log ends with enqueueMessage to validate if message is there,
            // without checking other message content (date, log type etc..)
            Assert.IsTrue(logManager.LogQueue.First().EndsWith(enqueuedMessage));
        }


        [TestCase(5, "ar-DZ")]
        [TestCase(10, "ar-SA")]
        [TestCase(25, "ko-KR")]
        public void TestLogManagerLimit_AddMessagesThatMatchLimitCriteria_AllMessagesShouldBeInLogManager(int numberOfLogs, string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            var message = "fake message";
            var stackTrace = string.Empty;
            var type = LogType.Log;
            var backtraceUnityLogManager = new BacktraceLogManager((uint)numberOfLogs);

            for (int i = 0; i < numberOfLogs; i++)
            {
                backtraceUnityLogManager.Enqueue(message, stackTrace, type);
            }
            Assert.AreEqual(backtraceUnityLogManager.Size, numberOfLogs);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(25)]
        public void TestLogManagerLimit_AddMessageReportsThatMatchLimitCriteria_AllMessagesShouldBeInLogManager(int numberOfLogs)
        {
            var report = new BacktraceReport("message");
            var backtraceUnityLogManager = new BacktraceLogManager((uint)numberOfLogs);

            for (int i = 0; i < numberOfLogs; i++)
            {
                backtraceUnityLogManager.Enqueue(report);
            }
            Assert.AreEqual(backtraceUnityLogManager.Size, numberOfLogs);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(25)]
        public void TestLogManagerLimit_AddExceptionReportsThatMatchLimitCriteria_AllMessagesShouldBeInLogManager(int numberOfLogs)
        {
            var report = new BacktraceReport(new Exception(string.Empty));
            var backtraceUnityLogManager = new BacktraceLogManager((uint)numberOfLogs);

            for (int i = 0; i < numberOfLogs; i++)
            {
                backtraceUnityLogManager.Enqueue(report);
            }
            Assert.AreEqual(backtraceUnityLogManager.Size, numberOfLogs);
        }
        [Test]
        public void TestLogManagerSourceCodeGeneration_ShouldReturnStringSourceCode_CorrectSourceCodeGenerationFromLog()
        {
            var message = "Message";
            var stackTrace = "stack trace";
            var type = LogType.Log;
            var backtraceUnityLogManager = new BacktraceLogManager(1);
            backtraceUnityLogManager.Enqueue(message, stackTrace, type);
            var sourceCodeText = backtraceUnityLogManager.ToSourceCode();
            Assert.IsNotEmpty(sourceCodeText);
            Assert.IsTrue(sourceCodeText.Contains(message));
            Assert.IsFalse(sourceCodeText.Contains(stackTrace));
        }
        [Test]
        public void TestLogManagerSourceCodeGeneration_ShouldReturnStringSourceCode_CorrectSourceCodeGenerationFromEmptyLog()
        {
            var type = LogType.Log;
            var backtraceUnityLogManager = new BacktraceLogManager(1);
            backtraceUnityLogManager.Enqueue(string.Empty, string.Empty, type);
            var sourceCodeText = backtraceUnityLogManager.ToSourceCode();
            Assert.IsNotEmpty(sourceCodeText);
        }
        [Test]
        public void TestLogManagerSourceCodeGeneration_ShouldReturnStringSourceCode_CorrectSourceCodeGenerationWithStackTraceForException()
        {
            var message = "Message";
            var stackTrace = "stack trace";
            var type = LogType.Exception;
            var backtraceUnityLogManager = new BacktraceLogManager(1);
            backtraceUnityLogManager.Enqueue(message, stackTrace, type);
            var sourceCodeText = backtraceUnityLogManager.ToSourceCode();
            Assert.IsNotEmpty(sourceCodeText);
            Assert.IsTrue(sourceCodeText.Contains(message));
            Assert.IsTrue(sourceCodeText.Contains(stackTrace));
        }

        [Test]
        public void TestLogManagerSourceCodeGeneration_ShouldReturnStringSourceCodeForEmptyValues_CorrectSourceCodeGenerationWithStackTraceForException()
        {
            var message = string.Empty;
            var stackTrace = string.Empty;
            var type = LogType.Log;
            var backtraceUnityLogManager = new BacktraceLogManager(1);
            backtraceUnityLogManager.Enqueue(message, stackTrace, type);
            string sourceCodeText = string.Empty;
            Assert.DoesNotThrow(() =>
            {
                sourceCodeText = backtraceUnityLogManager.ToSourceCode();
            });

            Assert.IsNotEmpty(sourceCodeText);
        }
    }
}