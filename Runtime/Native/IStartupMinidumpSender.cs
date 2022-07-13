using Backtrace.Unity.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    internal interface IStartupMinidumpSender
    {
        IEnumerator SendMinidumpOnStartup(ICollection<string> clientAttachments, IBacktraceApi backtraceApi);
    }
}
