using System.Collections;
using UnityEngine;
namespace Backtrace.Unity.Model.Waiter
{
    public class EndOfFrameWaiter : IWaiter
    {
        public IEnumerator Wait()
        {
            Debug.Log("Using EndOfFrameWaiter");
            return new WaitForEndOfFrame();
        }
    }
}