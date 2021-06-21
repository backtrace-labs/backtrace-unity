using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Services;
using NUnit.Framework;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Native.Windows
{
    public sealed class NativeCrashUploaderAttributesTests
    {
        [TearDown]
        public void Setup()
        {
            PlayerPrefs.DeleteKey(NativeCrashUploader.VersionKey);
            PlayerPrefs.DeleteKey(NativeCrashUploader.MachineUuidKey);
            PlayerPrefs.DeleteKey(NativeCrashUploader.SessionKey);
        }

        [Test]
        public void NativeCrashUploadAttributesSetting_ShouldSetValuesInPlayerPrefs_ValuesAreAvailableInPlayerPrefs()
        {
            var backtraceApi = new BacktraceApiMock();
            var attributeProvider = new AttributeProvider();

            new NativeCrashUploader(attributeProvider, backtraceApi);

            Assert.AreEqual(attributeProvider.ApplicationGuid, PlayerPrefs.GetString(NativeCrashUploader.MachineUuidKey));
            var sessionId = PlayerPrefs.GetString(NativeCrashUploader.SessionKey);
            Assert.AreEqual(attributeProvider.ApplicationSessionKey, string.IsNullOrEmpty(sessionId) ? null : sessionId);
            Assert.AreEqual(attributeProvider.ApplicationVersion, PlayerPrefs.GetString(NativeCrashUploader.VersionKey));
        }

        [Test]
        public void NativeCrashUploadAttributesReading_ShouldReadCorrecUuid_UuidIsValid()
        {
            var backtraceApi = new BacktraceApiMock();
            var attributeProvider = new AttributeProvider();

            var nativeCrashUploader = new NativeCrashUploader(attributeProvider, backtraceApi);

            Assert.AreEqual(attributeProvider.ApplicationGuid, nativeCrashUploader.MachineUuid);
        }

        [Test]
        public void NativeCrashUploadAttributesReading_ShouldReadCorrecSessionIdFromPreviousSession_SessionIdIsValid()
        {
            var backtraceApi = new BacktraceApiMock();
            var attributeProvider = new AttributeProvider();
            var backtraceMetrics = new BacktraceMetrics(attributeProvider, 100, "https://unique-event-url.com", "https://summed-event-url.com");
            attributeProvider.AddScopedAttributeProvider(backtraceMetrics);

            // simulate first session
            new NativeCrashUploader(attributeProvider, backtraceApi);
            // second session
            var nativeCrashUploader = new NativeCrashUploader(attributeProvider, backtraceApi);

            Assert.AreEqual(attributeProvider.ApplicationSessionKey, nativeCrashUploader.SessionId);
        }

        [Test]
        public void NativeCrashUploadAttributesReading_ShouldReadCorrecVersion_VersionIsValid()
        {
            var backtraceApi = new BacktraceApiMock();
            var attributeProvider = new AttributeProvider();

            var nativeCrashUploader = new NativeCrashUploader(attributeProvider, backtraceApi);

            Assert.AreEqual(attributeProvider.ApplicationVersion, nativeCrashUploader.ApplicationVersion);
        }

        [Test]
        public void NativeCrashUploadAttributesReading_ShouldReadSessionIdFromPreviousSession_SessionIdIsValid()
        {
            const string fakeSessionId = "foo-bar";
            var backtraceApi = new BacktraceApiMock();
            var attributeProvider = new AttributeProvider();
            PlayerPrefs.SetString(NativeCrashUploader.SessionKey, fakeSessionId);

            var nativeCrashUploader = new NativeCrashUploader(attributeProvider, backtraceApi);

            Assert.AreEqual(fakeSessionId, nativeCrashUploader.SessionId);
        }
    }
}
