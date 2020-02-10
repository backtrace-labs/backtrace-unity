using Backtrace.Unity;
using Backtrace.Unity.Interfaces;
using UnityEngine;

public class BacktraceDatabaseMock : BacktraceDatabase
{
    /// <summary>
    /// Make sure we won't remove any file/directory from unit-test code.
    /// </summary>
    protected override void RemoveOrphaned()
    {
        Debug.Log("Removing old reports");
    }

    internal override void LoadReports()
    {
        Debug.Log("Loading reports");
    }

    /// <summary>
    /// Make sure we don't store any data on hard drive.
    /// </summary>
    protected override void CreateDatabaseDirectory()
    {
        Debug.Log("Creating database directory");
    }

    protected override IBacktraceDatabaseContext BacktraceDatabaseContext
    {
        get
        {
            return base.BacktraceDatabaseContext;
        }

        set
        {
            // mock should only create one type of backtrace database context.
            base.BacktraceDatabaseContext = new BacktraceDatabaseContextMock(DatabaseSettings);
        }
    }
}