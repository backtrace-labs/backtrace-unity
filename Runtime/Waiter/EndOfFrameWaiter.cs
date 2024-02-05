using Backtrace.Unity.Extensions;
using Backtrace.Unity.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backtrace.Unity.Model.Waiter
{
    public class EndOfFrameWaiter : IWaiter
    {
        public IEnumerator Wait()
        {
            Debug.Log("Using EndOfFrameWaiter");
            yield return new WaitForEndOfFrame();
        }
    }
}