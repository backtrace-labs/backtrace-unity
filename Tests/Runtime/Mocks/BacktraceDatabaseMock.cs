using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceDatabaseMock : BacktraceDatabase
    {
        /// <summary>
        /// Make sure we won't remove any file/directory from unit-test code.
        /// </summary>
        protected override void RemoveOrphaned()
        {
            Debug.Log("Removing old reports");
        }

        protected override void LoadReports()
        {
            Debug.Log("Loading reports");
        }

        /// <summary>
        /// Make sure we don't store any data on hard drive.
        /// </summary>
        protected override bool InitializeDatabasePaths()
        {
            DatabasePath = Configuration.DatabasePath;
            Debug.Log("Setting up Backtrace database directories");
            return true;
        }
    }
}