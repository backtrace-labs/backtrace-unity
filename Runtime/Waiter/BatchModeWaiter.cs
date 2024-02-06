using UnityEngine;

namespace Backtrace.Unity.Model.Waiter
{
    public class BatchModeWaiter : IWaiter
    {
        public YieldInstruction Wait()
        {
            Debug.Log("Using BatchModeWaiter");
            return null;
        }
    }
}