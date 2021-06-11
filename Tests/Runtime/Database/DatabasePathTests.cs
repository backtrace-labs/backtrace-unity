using Backtrace.Unity.Common;
using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public class DatabasePathTests
    {

        [Test]
        public void TestDbPath_EmptyPathToDatabase_PathShouldBeEmpty()
        {
            Assert.IsEmpty(ClientPathHelper.GetFullPath(string.Empty));
        }

        [Test]
        public void TestDbPath_ShouldReplaceInterpolationWithDataPath_PathShouldntBeEmpty()
        {

            var expectedDatabasePath = Path.Combine(Application.dataPath, "foo", "bar");
            var testedDatabasePath = "${Application.dataPath}/foo/bar";

            var actualDatabasePath = ClientPathHelper.GetFullPath(testedDatabasePath);

            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldReplaceInterpolationWithPersistentDataPathDataPath_PathShouldntBeEmpty()
        {

            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var testedDatabasePath = "${Application.persistentDataPath}/foo/bar";

            var actualDatabasePath = ClientPathHelper.GetFullPath(testedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldtTryToParseInterpolatedString_PathShouldntBeEmpty()
        {

            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var actualDatabasePath = ClientPathHelper.GetFullPath(expectedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldTryToEscapeRootedDir_PathShouldntBeEmpty()
        {
            var testedPath = "./test";
            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, testedPath);
            var actualDatabasePath = ClientPathHelper.GetFullPath(testedPath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldCorrectlyGenerateFullpath_PathShouldntBeEmpty()
        {
            var expectedDatabasePath =
#if UNITY_EDITOR_OSX || UNITY_IOS || UNITY_STANDALONE_OSX
                "/Users/user/Library/Application Support/Backtrace/database/path";
#else
            "C:/users/user/Backtrace/database/path";
#endif

            var actualDatabasePath = ClientPathHelper.GetFullPath(expectedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldParseCorrectlyInterpolatedStringWithUpperCaseChar_PathShouldntBeEmpty()
        {
            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var testedDatabasePath = "${Application.PersistentDataPath}/foo/bar";

            var actualDatabasePath = ClientPathHelper.GetFullPath(testedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldParseCorrectlyInterpolatedStringWithLowerCaseChar_PathShouldntBeEmpty()
        {
            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var testedDatabasePath = "${application.persistentDataPath}/foo/bar";

            var actualDatabasePath = ClientPathHelper.GetFullPath(testedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }


        [Test]
        public void TestDbPath_ShouldHandleIncorrectInterpolationClosingValue_ShouldReturnInvalidPathToDatabase()
        {
            var testedInvalidPath = "${application.persistentDataPath/foo/bar";
            // beacuse ${ will point to root dir of project, we will try to extend this path
            // with application.dataPath
            var expectedInvalidPath = Path.Combine(Application.persistentDataPath, testedInvalidPath);
            var actualDatabasePath = ClientPathHelper.GetFullPath(testedInvalidPath);
            Assert.AreEqual(Path.GetFullPath(expectedInvalidPath), actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldHandleIncorrectInterpolationStartingValue_ShouldReturnInvalidPathToDatabase()
        {
            var testedInvalidPath = "$application.persistentDataPath/foo/bar";
            // beacuse ${ will point to root dir of project, we will try to extend this path
            // with application.dataPath
            var expectedInvalidPath = Path.Combine(Application.persistentDataPath, testedInvalidPath);
            var actualDatabasePath = ClientPathHelper.GetFullPath(testedInvalidPath);
            Assert.AreEqual(Path.GetFullPath(expectedInvalidPath), actualDatabasePath);
        }


        [Test]
        public void TestDbPath_ShouldHandleIncorrectInterpolationValue_ShouldReturnInvalidPathToDatabase()
        {
            var testedInvalidPath = "application.persistentDataPath}/foo/bar";
            // beacuse ${ will point to root dir of project, we will try to extend this path
            // with application.dataPath
            var expectedInvalidPath = Path.Combine(Application.persistentDataPath, testedInvalidPath);
            var actualDatabasePath = ClientPathHelper.GetFullPath(testedInvalidPath);
            Assert.AreEqual(Path.GetFullPath(expectedInvalidPath), actualDatabasePath);
        }
    }
}
