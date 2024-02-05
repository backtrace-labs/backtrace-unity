using System.Collections;

namespace Backtrace.Unity.Model.Waiter
{
    public interface IWaiter
    {
        public IEnumerator Wait();
    }
}