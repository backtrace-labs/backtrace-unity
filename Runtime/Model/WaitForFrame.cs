using Backtrace.Unity.Model.Waiter;
using System.Collections;

namespace Backtrace.Unity.Model
{
    public class WaitForFrame
    {
        private static IWaiter _waiter = Application.isBatchMode
             ? new BatchModeWaiter()
             : new EndOfFrameWaiter();

        public static IEnumerator Wait()
        {
            return _waiter.Wait();
        }
    }
}