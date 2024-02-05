using Backtrace.Unity.Extensions;
using Backtrace.Unity.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backtrace.Unity.Model.Waiter
{
    public interface IWaiter
    {
        public IEnumerator Wait();
    }
}