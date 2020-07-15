using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceDatabaseTests : BacktraceBaseTest
    {
        private BacktraceDatabase database;

        [OneTimeSetUp]
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
            configuration.DatabasePath = Application.temporaryCachePath;
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
