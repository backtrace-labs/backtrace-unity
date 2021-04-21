using System.Collections.Generic;

namespace Backtrace.Unity.Interfaces
{
    public interface IBacktraceSession
    {
        string SubmissionUrl { get; set; }
        void Send();
        bool AddUniqueEvent(string attributeName, IDictionary<string, string> attributes);
        bool AddSessionEvent(string sessionEvent, IDictionary<string, string> attributes);
        void Tick(long time);
    }
}
