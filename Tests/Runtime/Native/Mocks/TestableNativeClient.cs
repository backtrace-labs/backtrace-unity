#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native;
using Backtrace.Unity.Runtime.Native.Base;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime.Native.Mocks
{
    internal sealed class TestableNativeClient : NativeClientBase, INativeClient
    {
        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();

        public TestableNativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs) : base(configuration, breadcrumbs)
        { }

        public void GetAttributes(IDictionary<string, string> attributes)
        {
            foreach (var attribute in _attributes)
            {
                attributes[attribute.Key] = attribute.Value;
            }
            return;
        }

        public void HandleAnr()
        {
            return;
        }

        public void SimulateAnr()
        {
            OnAnrDetection();
        }

        public bool OnOOM()
        {
            return true;
        }

        public void SetAttribute(string key, string value)
        {
            _attributes[key] = value;
            return;
        }
    }
}
#endif