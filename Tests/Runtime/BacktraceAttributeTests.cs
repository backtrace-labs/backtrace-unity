using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceAttributeTests
    {
        [TestCase(null, null)]
        [TestCase("errorMessage", null)]
        [TestCase(null, "keyValue")]
        [TestCase("errorMessage", "keyValue")]
        public void TestAttributesGeneration_NullableValues_ValidAttributeObject(string errorMessage, string keyValue)
        {
            var report = string.IsNullOrEmpty(errorMessage) ? null : new BacktraceReport(errorMessage);
            var attributes = string.IsNullOrEmpty(keyValue) ? null : new Dictionary<string, string>() { { keyValue, keyValue } };
            var backtraceAttributes = new BacktraceAttributes(report, attributes);
            Assert.IsNotNull(backtraceAttributes);
        }
        [UnityTest]
        public IEnumerator TestAttributesGeneration_CreateCorrectMessageAttributes_WithDiffrentReportConfiguration()
        {
            var clientAttributes = new Dictionary<string, string>()
            {
                ["foo"] = "foo",
                ["bar"] = ""
            };

            var reportAttributes = new Dictionary<string, string>()
            {
                ["reportFoo"] = "foo",
                ["reportBar"] = "bar"
            };

            var report = new BacktraceReport("message", reportAttributes);
            var attributes = new BacktraceAttributes(report, clientAttributes);
            foreach (var keyValuePair in clientAttributes)
            {
                Assert.AreEqual(attributes.Attributes[keyValuePair.Key].ToString(), clientAttributes[keyValuePair.Key]);
            }

            foreach (var keyValuePair in reportAttributes)
            {
                Assert.AreEqual(attributes.Attributes[keyValuePair.Key].ToString(), reportAttributes[keyValuePair.Key]);
            }

            Assert.AreEqual(report.Message, attributes.Attributes["error.message"]);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestAttributesGeneration_CreateCorrectErrorAttributes_WithDiffrentReportConfiguration()
        {
            var clientAttributes = new Dictionary<string, string>()
            {
                ["foo"] = "foo",
                ["bar"] = ""
            };

            var reportAttributes = new Dictionary<string, string>()
            {
                ["reportFoo"] = "foo",
                ["reportBar"] = "bar"
            };

            var report = new BacktraceReport(new FileNotFoundException(), reportAttributes);
            var attributes = new BacktraceAttributes(report, clientAttributes);
            foreach (var keyValuePair in clientAttributes)
            {
                Assert.AreEqual(attributes.Attributes[keyValuePair.Key].ToString(), clientAttributes[keyValuePair.Key]);
            }

            foreach (var keyValuePair in reportAttributes)
            {
                Assert.AreEqual(attributes.Attributes[keyValuePair.Key].ToString(), reportAttributes[keyValuePair.Key]);
            }

            Assert.AreEqual(report.Message, attributes.Attributes["error.message"]);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestCorrectDictionaryGeneration_ReplaceAttributes_TheSameDictionaryAttributes()
        {
            var reportAttributeKey = "report_attr";
            var reportAttributeValue = string.Format("{0}-value", reportAttributeKey);
            var reportAttributes = new Dictionary<string, string>()
            {
                { reportAttributeKey, reportAttributeValue}
            };

            var clientAttributes = new Dictionary<string, string>() {
                {reportAttributeKey, string.Format("{0}-client", reportAttributeValue)
            } };

            var report = new BacktraceReport("message", reportAttributes);
            var attributes = new BacktraceAttributes(report, clientAttributes);
            Assert.AreEqual(attributes.Attributes[reportAttributeKey], reportAttributes[reportAttributeKey]);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestLibraryAttributes_ValidApplicationAttributes_ShouldIncludeAllApplicationInformationInAttributes()
        {
            var attributes = new BacktraceAttributes(null, null);

            Assert.AreEqual(attributes.Attributes["application"], Application.productName);
            Assert.AreEqual(attributes.Attributes["application.version"], Application.version);
            Assert.AreEqual(attributes.Attributes["application.url"], Application.absoluteURL);
            Assert.AreEqual(attributes.Attributes["application.company.name"], Application.companyName);
            Assert.AreEqual(attributes.Attributes["application.data_path"], Application.dataPath);
            Assert.AreEqual(attributes.Attributes["application.id"], Application.identifier);
            Assert.AreEqual(attributes.Attributes["application.installer.name"], Application.installerName);
            Assert.AreEqual(attributes.Attributes["application.internet_reachability"], Application.internetReachability.ToString());
            Assert.AreEqual(attributes.Attributes["application.editor"], Application.isEditor);
            Assert.AreEqual(attributes.Attributes["application.focused"], Application.isFocused);
            Assert.AreEqual(attributes.Attributes["application.mobile"], Application.isMobilePlatform);
            Assert.AreEqual(attributes.Attributes["application.playing"], Application.isPlaying);
            Assert.AreEqual(attributes.Attributes["application.background"], Application.runInBackground);
            Assert.AreEqual(attributes.Attributes["application.sandboxType"], Application.sandboxType.ToString());
            Assert.AreEqual(attributes.Attributes["application.system.language"], Application.systemLanguage.ToString());
            Assert.AreEqual(attributes.Attributes["application.unity.version"], Application.unityVersion);
            Assert.AreEqual(attributes.Attributes["application.temporary_cache"], Application.temporaryCachePath);
            Assert.AreEqual(attributes.Attributes["application.debug"], Debug.isDebugBuild);

            yield return null;
        }
    }
}
