using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Database;
using Backtrace.Unity.Services;

namespace Backtrace.Unity.Tests.Runtime
{
    internal class BacktraceDatabaseContextMock : BacktraceDatabaseContext
    {
        private readonly BacktraceDatabaseSettings _settings;
        public BacktraceDatabaseContextMock(BacktraceDatabaseSettings settings) : base(settings)
        {
            _settings = settings;
        }
    }
}