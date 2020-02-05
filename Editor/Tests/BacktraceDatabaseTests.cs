using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BacktraceDatabaseTests: BacktraceBaseTest
    {
        private BacktraceDatabase database;

        [SetUp]
        public void Setup()
        {
            BeforeSetup();
            database = GameObject.AddComponent<BacktraceDatabaseMock>();
            database.Configuration = null;
            AfterSetup(false);
        }

        [UnityTest]
        public IEnumerator TestDbCreation_EmptyBacktraceConfiguration_ValidDbCreation()
        {
            Assert.IsTrue(!database.Enable);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestDbCreation_ValidConfiguration_EnabledDb()
        {
            var configuration = GetBasicConfiguration();
            configuration.DatabasePath = Application.dataPath;
            configuration.CreateDatabase = false;
            configuration.AutoSendMode = false;
            configuration.Enabled = true;

            database.Configuration = configuration;
            database.Reload();
            Assert.IsTrue(database.Enable);
            yield return null;
        }

    }
}
