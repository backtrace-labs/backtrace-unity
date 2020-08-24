using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    internal interface INativeClient
    {
#if UNITY_ANDROID || UNITY_IOS
        void HandleAnr(string gameObjectName, string callbackName);
#endif
        Dictionary<string, string> GetAttributes();

        void SetAttribute(string key, string value);
    }
}
