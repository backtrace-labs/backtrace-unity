using System.Collections;
using UnityEngine;

namespace Backtrace.Unity.Model.Waiter
{
    public interface IWaiter
    {
        YieldInstruction Wait();
    }
}