#if UNITY_STANDALONE_WIN
using Backtrace.Unity.Model;
using Backtrace.Unity.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using NativeClient = Backtrace.Unity.Runtime.Native.Windows.NativeClient;

namespace Backtrace.Unity.Tests.Runtime.Native.Windows
{
    public sealed class ScopedNativeAttributesTests
    {
        // PlayerPrefs key used by the Windows NativeClient to store the scoped list
        private const string ScopedKeyList = "backtrace-scoped-attributes";
        private const string ScopedValuePattern = "bt-{0}";

        [TearDown]
        public void Setup()
        {
            CleanLegacyAttributes();
            NativeClient.CleanScopedAttributes();
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void FreshStartup_ShouldntIncludeAnyAttributeFromPlayerPrefs_AttributesAreEmpty()
        {
            var attributes = NativeClient.GetScopedAttributes();

            Assert.IsEmpty(attributes);
        }

        [Test]
        public void LegacyAttributesSupport_ShouldIncludeLegacyAttributesWhenScopedAttributesAreNotAvailable_AllLegacyAttributesArePresent()
        {
            string testVersion = "0.1.0";
            PlayerPrefs.SetString(NativeClient.VersionKey, testVersion);
            string machineUuid = "foo-bar";
            PlayerPrefs.SetString(NativeClient.MachineUuidKey, machineUuid);
            string sessionKey = "session-foo-bar-baz";
            PlayerPrefs.SetString(NativeClient.SessionKey, sessionKey);

            var attributes = NativeClient.GetScopedAttributes();

            Assert.AreEqual(attributes.Count, 3);
            Assert.AreEqual(attributes["application.version"], testVersion);
            Assert.AreEqual(attributes["guid"], machineUuid);
            Assert.AreEqual(attributes[BacktraceMetrics.ApplicationSessionKey], sessionKey);
        }

        [Test]
        public void NativeCrashUploadAttributes_ShouldSetValuesInPlayerPrefs_ValuesAreAvailableInPlayerPrefs()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.SendUnhandledGameCrashesOnGameStartup = true;
            const string testAttributeString = "foo-key";
            const string testAttributeValue = "foo-bar-value";

            new NativeClient(configuration, null, new Dictionary<string, string>() { { testAttributeString, testAttributeValue } }, new List<string>());
            var scopedAttributes = NativeClient.GetScopedAttributes();

            Assert.AreEqual(scopedAttributes[testAttributeString], testAttributeValue);
        }

        [Test]
        public void NativeCrashUploadAttributesSetting_ShouldReadPlayerPrefsWithLegacyAttributes_AllAttributesArePresent()
        {
            string testVersion = "0.1.0";
            PlayerPrefs.SetString(NativeClient.VersionKey, testVersion);
            string machineUuid = "foo-bar";
            PlayerPrefs.SetString(NativeClient.MachineUuidKey, machineUuid);
            string sessionKey = "session-foo-bar-baz";
            PlayerPrefs.SetString(NativeClient.SessionKey, sessionKey);

            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.SendUnhandledGameCrashesOnGameStartup = true;
            const string testAttributeString = "foo-key";
            const string testAttributeValue = "foo-bar-value";

            new NativeClient(configuration, null, new Dictionary<string, string>() { { testAttributeString, testAttributeValue } }, new List<string>());
            var scopedAttributes = NativeClient.GetScopedAttributes();

            Assert.AreEqual(scopedAttributes["application.version"], testVersion);
            Assert.AreEqual(scopedAttributes["guid"], machineUuid);
            Assert.AreEqual(scopedAttributes[BacktraceMetrics.ApplicationSessionKey], sessionKey);
            Assert.AreEqual(scopedAttributes[testAttributeString], testAttributeValue);
        }

        [Test]
        public void NativeCrashUploadAttributes_ShouldSetScopedAttributeViaNativeClientApi_AttributePresentsInScopedAttributes()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.SendUnhandledGameCrashesOnGameStartup = true;
            const string testAttributeKey = "foo-key-bar-baz";
            const string testAttributeValue = "123123";

            var client = new NativeClient(configuration, null, new Dictionary<string, string>(), new List<string>());
            client.SetAttribute(testAttributeKey, testAttributeValue);
            var scopedAttributes = NativeClient.GetScopedAttributes();

            Assert.AreEqual(scopedAttributes[testAttributeKey], testAttributeValue);
        }

        [Test]
        public void NativeCrashAttributesCleanMethod_ShouldCleanAllScopedAttribtues_ScopedAttributesAreNotPresent()
        {
            string testVersion = "0.1.0";
            PlayerPrefs.SetString(NativeClient.VersionKey, testVersion);
            string machineUuid = "foo-bar";
            PlayerPrefs.SetString(NativeClient.MachineUuidKey, machineUuid);
            string sessionKey = "session-foo-bar-baz";
            PlayerPrefs.SetString(NativeClient.SessionKey, sessionKey);
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.SendUnhandledGameCrashesOnGameStartup = true;
            const string testAttributeString = "foo-key";
            const string testAttributeValue = "foo-bar-value";

            new NativeClient(configuration, null, new Dictionary<string, string>() { { testAttributeString, testAttributeValue } }, new List<string>());
            var attributesBeforeCleanup = NativeClient.GetScopedAttributes();
            NativeClient.CleanScopedAttributes();
            var attributesAfterCleanup = NativeClient.GetScopedAttributes();

            Assert.IsEmpty(attributesAfterCleanup);
            Assert.IsNotEmpty(attributesBeforeCleanup);
        }

        [Test]
        public void ScopedAttributes_ShouldNotDuplicateKeys_OnRepeatedAdds()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.SendUnhandledGameCrashesOnGameStartup = true;

            const string k = "dup-key";
            const string v = "v1";

            var client = new NativeClient(configuration, null, new Dictionary<string, string>(), new List<string>());

            client.SetAttribute(k, v);
            client.SetAttribute(k, v);
            client.SetAttribute(k, v);

            var scoped = NativeClient.GetScopedAttributes();
            Assert.IsTrue(scoped.ContainsKey(k));
            Assert.AreEqual(v, scoped[k]);

            var json = PlayerPrefs.GetString(ScopedKeyList);
            var occurrences = json.Split('"');
            int count = 0;
            foreach (var s in occurrences)
            {
                if (s == k) count++;
            }

            Assert.AreEqual(1, count, "Key should be stored once in the scoped key list.");
        }

        [Test]
        public void ScopedAttributes_ShouldSkipWrites_WhenValueUnchanged()
        {
            var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
            configuration.SendUnhandledGameCrashesOnGameStartup = true;

            const string k = "stable-key";
            const string v = "same-value";

            var client = new NativeClient(configuration, null, new Dictionary<string, string>(), new List<string>());

            client.SetAttribute(k, v);
            var json1 = PlayerPrefs.GetString(ScopedKeyList);
            var val1 = PlayerPrefs.GetString(string.Format(ScopedValuePattern, k));

            client.SetAttribute(k, v);
            var json2 = PlayerPrefs.GetString(ScopedKeyList);
            var val2 = PlayerPrefs.GetString(string.Format(ScopedValuePattern, k));

            Assert.AreEqual(json1, json2, "Scoped key list JSON should not change when value is unchanged.");
            Assert.AreEqual(val1, val2, "Stored value should remain the same.");
        }

        private void CleanLegacyAttributes()
        {
            PlayerPrefs.DeleteKey(NativeClient.VersionKey);
            PlayerPrefs.DeleteKey(NativeClient.MachineUuidKey);
            PlayerPrefs.DeleteKey(NativeClient.SessionKey);
        }
    }
}

#endif