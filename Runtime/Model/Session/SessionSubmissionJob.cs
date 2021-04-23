namespace Backtrace.Unity.Model.Session
{
    internal sealed class SessionSubmissionJob
    {
        public float NextInvokeTime { get; set; }
        public UniqueEvent[] UniqueEvents { get; set; }
        public SessionEvent[] SessionEvents { get; set; }
        public uint NumberOfRetries { get; set; }
    }
}
