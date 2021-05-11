namespace Backtrace.Unity.Model.Metrics
{
    internal sealed class MetricsSubmissionJob
    {
        public double NextInvokeTime { get; set; }
        public UniqueEvent[] UniqueEvents { get; set; }
        public SummedEvent[] SummedEvents { get; set; }
        public uint NumberOfAttemps { get; set; }
    }
}
