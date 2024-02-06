using Backtrace.Unity.Model.Waiter;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    public class WaitForFrame
    {
        private static IWaiter _waiter = CreateWaiterStrategy();

        public static YieldInstruction Wait()
        {
            return _waiter.Wait();
        }

        private static IWaiter CreateWaiterStrategy()
        {
            if (Application.isBatchMode)
            {
                return new BatchModeWaiter();
            }

            return new EndOfFrameWaiter();
        }
    }
}