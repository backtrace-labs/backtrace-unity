using Backtrace.Unity.Extensions;
using Backtrace.Unity.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backtrace.Unity.Model.Waiter
{
    public class BatchModeWaiter : IWaiter
    {
        public IEnumerator Wait()
        {
            Debug.Log("Using BatchModeWaiter");
            yield return null;
        }
    }
}