using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    internal interface INativeClient
    {
#if UNITY_ANDROID || UNITY_IOS
        void HandleAnr(string gameObjectName, string callbackName);
#endif
        void GetAttributes(Dictionary<string, string> data);

        void SetAttribute(string key, string value);
    }
}
