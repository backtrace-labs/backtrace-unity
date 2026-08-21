using System;
using System.Collections.Generic;
using Backtrace.Unity.Runtime.Native;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class NativeAttributeLifecycleTests
    {
        private const string AndroidFailureCode = "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE";

        [Test]
        public void NativeAttributeFailureIsContainedAndLogsOnlyFailureType()
        {
            const string sensitiveKey = "secret-key";
            const string sensitiveValue = "secret-value";
            const string sensitivePath = "/private/data/backtrace";
            const string sensitiveUrl = "https://submit.example.test/token";
            var warnings = new List<string>();

            bool result = true;
            Assert.DoesNotThrow(() => result = NativeAttributeLifecycle.TrySetAttribute(
                () => { throw new InvalidOperationException(
                    sensitiveKey + " " + sensitiveValue + " " + sensitivePath + " " + sensitiveUrl); },
                AndroidFailureCode,
                warnings.Add));

            Assert.IsFalse(result);
            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
            StringAssert.DoesNotContain(sensitiveKey, warnings[0]);
            StringAssert.DoesNotContain(sensitiveValue, warnings[0]);
            StringAssert.DoesNotContain(sensitivePath, warnings[0]);
            StringAssert.DoesNotContain(sensitiveUrl, warnings[0]);
        }

        [Test]
        public void FailedAttributeDoesNotPreventLaterAttributeOperations()
        {
            int attempts = 0;
            var warnings = new List<string>();

            bool firstResult = NativeAttributeLifecycle.TrySetAttribute(
                () =>
                {
                    attempts++;
                    throw new InvalidOperationException("first attribute failed");
                },
                AndroidFailureCode,
                warnings.Add);

            Assert.IsFalse(firstResult);
            Assert.AreEqual(1, warnings.Count);
            warnings.Clear();

            bool secondResult = NativeAttributeLifecycle.TrySetAttribute(
                () => attempts++,
                AndroidFailureCode,
                warnings.Add);

            Assert.IsTrue(secondResult);
            Assert.AreEqual(2, attempts);
            Assert.IsEmpty(warnings, "A successful native attribute write must not log a warning.");
        }

        [Test]
        public void LoggingFailureDoesNotEscape()
        {
            Assert.DoesNotThrow(() => NativeAttributeLifecycle.TrySetAttribute(
                () => { throw new InvalidOperationException("native attribute failed"); },
                AndroidFailureCode,
                warning => { throw new ApplicationException("logger failed"); }));
        }

        [Test]
        public void ContainedAndroidFailureDoesNotProduceDuplicateGenericWarning()
        {
            var warnings = new List<string>();

            bool result = NativeAttributeLifecycle.TrySetAttribute(
                () => NativeAttributeLifecycle.TrySetAttribute(
                    () => { throw new InvalidOperationException("native attribute failed"); },
                    AndroidFailureCode,
                    warnings.Add),
                NativeAttributeLifecycle.NativeAttributeFailureCode,
                warnings.Add);

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(
                new[] { "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE: Failure type: System.InvalidOperationException" },
                warnings);
        }
    }
}
