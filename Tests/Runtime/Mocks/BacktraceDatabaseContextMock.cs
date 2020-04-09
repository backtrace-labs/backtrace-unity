using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Services;

public class BacktraceDatabaseContextMock : BacktraceDatabaseContext
{
    private BacktraceDatabaseSettings _settings;
    public BacktraceDatabaseContextMock(BacktraceDatabaseSettings settings) : base(settings)
    {
        _settings = settings;
    }

    protected override BacktraceDatabaseRecord ConvertToRecord(BacktraceData backtraceData, string hash)
    {
        //create new record and return it to AVOID storing data on hard drive
        return new BacktraceDatabaseRecord(backtraceData, _settings.DatabasePath)
        {
            Hash = hash
        };
    }
}
