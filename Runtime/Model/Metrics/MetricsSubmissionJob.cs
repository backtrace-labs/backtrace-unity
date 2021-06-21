using System.Collections.Generic;

namespace Backtrace.Unity.Model.Metrics
{
    internal sealed class MetricsSubmissionJob<T>
    {
        public double NextInvokeTime { get; set; }
        public ICollection<T> Events { get; set; }
        public uint NumberOfAttempts { get; set; }
    }
}
