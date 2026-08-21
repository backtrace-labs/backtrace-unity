#if UNITY_ANDROID || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using Backtrace.Unity.Model;
using Backtrace.Unity.Runtime.Native.Android;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class AndroidNativeClientOomTests
    {
        [Test]
        public void OnOomAttemptsTimestampAfterFirstAttributeWriteFails()
        {
            var attempts = new List<KeyValuePair<string, string>>();
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            try
            {
                var client = new NativeClient(
                    configuration,
                    (key, value) =>
                    {
                        attempts.Add(new KeyValuePair<string, string>(key, value));
                        if (attempts.Count == 1)
                        {
                            throw new InvalidOperationException("first native attribute failed");
                        }
                    });

                LogAssert.Expect(
                    LogType.Warning,
                    "BT_UNITY_ANDROID_NATIVE_ATTRIBUTE_FAILURE: Failure type: System.InvalidOperationException");

                bool result = false;
                Assert.DoesNotThrow(() => { result = client.OnOOM(); });

                Assert.IsTrue(result);
                Assert.AreEqual(2, attempts.Count);
                Assert.AreEqual("memory.warning", attempts[0].Key);
                Assert.AreEqual("true", attempts[0].Value);
                Assert.AreEqual("memory.warning.date", attempts[1].Key);

                int timestamp;
                Assert.IsTrue(
                    int.TryParse(
                        attempts[1].Value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out timestamp),
                    "The second attribute must contain an invariant Unix timestamp.");
                Assert.Greater(timestamp, 0);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(configuration);
            }
        }
    }
}
#endif
