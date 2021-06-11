using Backtrace.Unity.Common;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.InMemory;
using Backtrace.Unity.Model.Breadcrumbs.Storage;
using Backtrace.Unity.Tests.Runtime.Breadcrumbs.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime.Breadcrumbs
{
    public class BreadcrumbsFileOperationTests
    {
        private readonly string _startOfDocumentString = Encoding.UTF8.GetString(BacktraceStorageLogManager.StartOfDocument);
        private readonly string _endOfDocumentString = Encoding.UTF8.GetString(BacktraceStorageLogManager.EndOfDocument);
        private readonly string _newRow = Encoding.UTF8.GetString(BacktraceStorageLogManager.NewRow);
        private const BacktraceBreadcrumbType ManualBreadcrumbsType = BacktraceBreadcrumbType.Manual;

        [Test]
        public void TestFileCreation_ShouldRecreateFileIfFileExists_SuccessfullyRecreatedFile()
        {
            var breadcrumbFile = new InMemoryBreadcrumbFile();
            var breadcrumbsStorageManager = new BacktraceStorageLogManager(Application.temporaryCachePath)
            {
                BreadcrumbFile = breadcrumbFile
            };
            string emptyDocumentText = _startOfDocumentString + _endOfDocumentString;

            var enableResult = breadcrumbsStorageManager.Enable();

            Assert.IsTrue(enableResult);
            Assert.AreEqual(Encoding.UTF8.GetBytes(emptyDocumentText), breadcrumbFile.MemoryStream.ToArray());
        }

        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Assert)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Exception)]
        public void TestFileCreation_AddNewBreadcrumbToFile_SuccessfullyAddedBreadcrumb(LogType testedLevel)
        {
            var currentTime = DateTimeHelper.TimestampMs();
            const string breadcrumbMessage = "foo";
            var breadcrumbFile = new InMemoryBreadcrumbFile();
            var breadcrumbsStorageManager = new BacktraceStorageLogManager(Application.temporaryCachePath)
            {
                BreadcrumbFile = breadcrumbFile
            };
            var breadcrumbsManager = new BacktraceBreadcrumbs(breadcrumbsStorageManager);
            var unityEngineLogLevel = breadcrumbsManager.ConvertLogTypeToLogLevel(testedLevel);
            var logTypeThatUnsupportCurrentTestCase =
              (Enum.GetValues(typeof(UnityEngineLogLevel)) as IEnumerable<UnityEngineLogLevel>)
              .First(n => n == unityEngineLogLevel);

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, logTypeThatUnsupportCurrentTestCase);
            var added = breadcrumbsManager.Log(breadcrumbMessage, testedLevel);

            Assert.IsTrue(added);
            var data = ConvertToBreadcrumbs(breadcrumbFile);
            Assert.AreEqual(1, data.Count());
            var breadcrumb = data.First();
            Assert.AreEqual(ManualBreadcrumbsType, (BacktraceBreadcrumbType)breadcrumb.Type);
            Assert.AreEqual(unityEngineLogLevel, breadcrumb.Level);
            Assert.AreEqual(breadcrumbMessage, breadcrumb.Message);
            // round timestamp because timestamp value in the final json will reduce decimal part.
            Assert.That(Math.Round(currentTime, 0), Is.LessThanOrEqualTo(Math.Round(breadcrumb.Timestamp, 0)));
        }

        [Test]
        public void TestFileLimit_ShouldCleanupTheSpace_SpaceWasCleaned()
        {
            const string breadcrumbMessage = "foo";
            const int minimalSize = 10 * 1000;
            var breadcrumbFile = new InMemoryBreadcrumbFile();
            var breadcrumbsStorageManager = new BacktraceStorageLogManager(Application.temporaryCachePath)
            {
                BreadcrumbFile = breadcrumbFile,
                BreadcrumbsSize = minimalSize
            };
            var breadcrumbsManager = new BacktraceBreadcrumbs(breadcrumbsStorageManager);
            var unityEngineLogLevel = UnityEngineLogLevel.Debug;
            var breadcrumbStart = breadcrumbsManager.BreadcrumbId();

            breadcrumbsManager.EnableBreadcrumbs(ManualBreadcrumbsType, unityEngineLogLevel);
            int numberOfAddedBreadcrumbs = 1;
            breadcrumbsManager.Log(breadcrumbMessage, LogType.Assert);
            var breadcrumbSize = breadcrumbFile.Size - 2;
            while (breadcrumbFile.Size + breadcrumbSize < minimalSize != false)
            {
                breadcrumbsManager.Log(breadcrumbMessage, LogType.Assert);
                numberOfAddedBreadcrumbs++;
            }
            var sizeBeforeCleanup = breadcrumbFile.Size;
            var numberOfBreadcurmbsBeforeCleanUp = numberOfAddedBreadcrumbs;
            breadcrumbsManager.Log(breadcrumbMessage, LogType.Assert);
            numberOfAddedBreadcrumbs++;

            Assert.That(breadcrumbFile.Size, Is.LessThan(sizeBeforeCleanup));
            var data = ConvertToBreadcrumbs(breadcrumbFile);
            Assert.IsNotEmpty(data);
            Assert.AreEqual(breadcrumbStart + numberOfAddedBreadcrumbs, breadcrumbsStorageManager.BreadcrumbId());
            Assert.That(breadcrumbsStorageManager.Length(), Is.LessThan(numberOfBreadcurmbsBeforeCleanUp));

        }

        [Test]
        public void TestBreadcrumbs_BasicBreadcrumbsTestForAllEvents_ShouldStoreEvents()
        {
            const int expectedNumberOfBreadcrumbs = 3;
            string[] messages = new string[expectedNumberOfBreadcrumbs] {
                "CustomUserBreadcrumb1",
                "PlayerStarted",
                "unhandled exception custom message from breadcrumbs test case"
            };

            var breadcrumb1Attributes = new Dictionary<string, string>() { { "name", "CustomUserBreadcrumb1Value" } };

            var breadcrumbFile = new InMemoryBreadcrumbFile();
            var breadcrumbsStorageManager = new BacktraceStorageLogManager(Application.temporaryCachePath)
            {
                BreadcrumbFile = breadcrumbFile
            };
            var breadcrumbsManager = new BacktraceBreadcrumbs(breadcrumbsStorageManager);
            var expectedBreadcrumbId = breadcrumbsManager.BreadcrumbId();
            var unityEngineLogLevel = UnityEngineLogLevel.Debug | UnityEngineLogLevel.Warning | UnityEngineLogLevel.Info | UnityEngineLogLevel.Error | UnityEngineLogLevel.Fatal;

            breadcrumbsManager.EnableBreadcrumbs(BacktraceBreadcrumbType.Manual | BacktraceBreadcrumbType.System, unityEngineLogLevel);
            breadcrumbsManager.Warning(messages[0], breadcrumb1Attributes);
            breadcrumbsManager.Info(messages[1]);
            breadcrumbsManager.Exception(messages[2]);

            Assert.AreEqual(expectedNumberOfBreadcrumbs, breadcrumbsStorageManager.Length());
            Assert.AreEqual(expectedNumberOfBreadcrumbs + expectedBreadcrumbId, breadcrumbsStorageManager.BreadcrumbId());
            var breadcrumbs = ConvertToBreadcrumbs(breadcrumbFile);
            for (int i = 0; i < expectedNumberOfBreadcrumbs; i++)
            {
                Assert.AreEqual(messages[i], breadcrumbs.ElementAt(i).Message);
            }
        }

        private IEnumerable<InMemoryBreadcrumb> ConvertToBreadcrumbs(InMemoryBreadcrumbFile file)
        {
            return ConvertToBreadcrumbs(Encoding.UTF8.GetString(file.MemoryStream.ToArray()));
        }
        private IEnumerable<InMemoryBreadcrumb> ConvertToBreadcrumbs(string json)
        {
            if (!json.StartsWith(_startOfDocumentString) || !json.EndsWith(_endOfDocumentString))
            {
                throw new ArgumentException("Invalid JSON file");
            }
            var dataJson = json
                .Substring(json.IndexOf(_startOfDocumentString) + _startOfDocumentString.Length, json.Length - _startOfDocumentString.Length - _endOfDocumentString.Length)
                .Split(new string[1] { _newRow }, StringSplitOptions.None);

            var result = new List<InMemoryBreadcrumb>();
            foreach (var data in dataJson)
            {
                result.Add(JsonUtility.FromJson<InMemoryBreadcrumb>(data));
            }
            return result;
        }
    }
}
