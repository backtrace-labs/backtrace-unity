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
            var added = breadcrumbsManager.AddBreadcrumbs(breadcrumbMessage, testedLevel);

            Assert.IsTrue(added);
            var data = ConvertToBreadcrumbs(breadcrumbFile);
            Assert.AreEqual(1, data.Count());
            var breadcrumb = data.First();
            Assert.AreEqual(ManualBreadcrumbsType, (BacktraceBreadcrumbType)breadcrumb.Type);
            Assert.AreEqual(unityEngineLogLevel, breadcrumb.Level);
            Assert.AreEqual(breadcrumbMessage, breadcrumb.Message);

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
