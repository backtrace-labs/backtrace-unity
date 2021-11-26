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
        [TearDown]
        public void Setup()
        {
            CleanLegacyAttributes();
            NativeClient.CleanScopedAttributes();
        }

        public void FreshStartup_ShouldntIncludeAnyAttributeFromPlayerPrefs_AttributesAreEmpty()
        {
            var attributes = NativeClient.GetScopedAttributes();

            Assert.IsEmpty(attributes);
        }

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

        private void CleanLegacyAttributes()
        {
            PlayerPrefs.DeleteKey(NativeClient.VersionKey);
            PlayerPrefs.DeleteKey(NativeClient.MachineUuidKey);
            PlayerPrefs.DeleteKey(NativeClient.SessionKey);
        }
    }
}

#endif