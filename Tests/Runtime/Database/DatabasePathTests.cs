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
            Assert.IsEmpty(DatabasePathHelper.GetFullDatabasePath(string.Empty));
        }

        [Test]
        public void TestDbPath_ShouldReplaceInterpolationWithDataPath_PathShouldntBeEmpty()
        {

            var expectedDatabasePath = Path.Combine(Application.dataPath, "foo", "bar");
            var testedDatabasePath = "${Application.dataPath}/foo/bar";

            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedDatabasePath);

            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldReplaceInterpolationWithPersistentDataPathDataPath_PathShouldntBeEmpty()
        {

            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var testedDatabasePath = "${Application.persistentDataPath}/foo/bar";

            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldtTryToParseInterpolatedString_PathShouldntBeEmpty()
        {

            var expectedDatabasePath = Path.Combine(Application.dataPath, "foo", "bar");
            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(expectedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldTryToEscapeRootedDir_PathShouldntBeEmpty()
        {
            var testedPath = "./test";
            var expectedDatabasePath = Path.Combine(Application.dataPath, testedPath);
            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedPath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldCorrectlyGenerateFullpath_PathShouldntBeEmpty()
        {
            var expectedDatabasePath = "C:/users/user/Backtrace/database/path";
            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(expectedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldParseCorrectlyInterpolatedStringWithUpperCaseChar_PathShouldntBeEmpty()
        {
            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var testedDatabasePath = "${Application.PersistentDataPath}/foo/bar";

            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldParseCorrectlyInterpolatedStringWithLowerCaseChar_PathShouldntBeEmpty()
        {
            var expectedDatabasePath = Path.Combine(Application.persistentDataPath, "foo", "bar");
            var testedDatabasePath = "${application.persistentDataPath}/foo/bar";

            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedDatabasePath);
            Assert.AreEqual(new DirectoryInfo(expectedDatabasePath).FullName, actualDatabasePath);
        }


        [Test]
        public void TestDbPath_ShouldHandleIncorrectInterpolationClosingValue_ShouldReturnInvalidPathToDatabase()
        {
            var testedInvalidPath = "${application.persistentDataPath/foo/bar";
            // beacuse ${ will point to root dir of project, we will try to extend this path
            // with application.dataPath
            var expectedInvalidPath = Path.Combine(Application.dataPath, testedInvalidPath);
            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedInvalidPath);
            Assert.AreEqual(Path.GetFullPath(expectedInvalidPath), actualDatabasePath);
        }

        [Test]
        public void TestDbPath_ShouldHandleIncorrectInterpolationStartingValue_ShouldReturnInvalidPathToDatabase()
        {
            var testedInvalidPath = "$application.persistentDataPath/foo/bar";
            // beacuse ${ will point to root dir of project, we will try to extend this path
            // with application.dataPath
            var expectedInvalidPath = Path.Combine(Application.dataPath, testedInvalidPath);
            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedInvalidPath);
            Assert.AreEqual(Path.GetFullPath(expectedInvalidPath), actualDatabasePath);
        }


        [Test]
        public void TestDbPath_ShouldHandleIncorrectInterpolationValue_ShouldReturnInvalidPathToDatabase()
        {
            var testedInvalidPath = "application.persistentDataPath}/foo/bar";
            // beacuse ${ will point to root dir of project, we will try to extend this path
            // with application.dataPath
            var expectedInvalidPath = Path.Combine(Application.dataPath, testedInvalidPath);
            var actualDatabasePath = DatabasePathHelper.GetFullDatabasePath(testedInvalidPath);
            Assert.AreEqual(Path.GetFullPath(expectedInvalidPath), actualDatabasePath);
        }
    }
}
