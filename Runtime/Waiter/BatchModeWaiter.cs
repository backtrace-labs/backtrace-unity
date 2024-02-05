using System.Collections;
using UnityEngine;
namespace Backtrace.Unity.Model.Waiter
{
    public class BatchModeWaiter : IWaiter
    {
        public IEnumerator Wait()
        {
            Debug.Log("Using BatchModeWaiter");
            return null;
        }
    }
}