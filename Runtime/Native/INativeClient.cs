using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backtrace.Unity.Runtime.Native
{
    internal interface INativeClient
    {
#if UNITY_ANDROID || UNITY_IOS
        void HandleAnr(string gameObjectName, string callbackName);
#endif
        Dictionary<string, string> GetAttributes();
    }
}
