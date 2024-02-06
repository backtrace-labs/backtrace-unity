using UnityEngine;

namespace Backtrace.Unity.Model.Waiter
{
    public class EndOfFrameWaiter : IWaiter
    {
        public YieldInstruction Wait()
        {
            return new WaitForEndOfFrame();
        }
    }
}