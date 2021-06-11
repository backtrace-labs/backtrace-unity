using System.Collections.Generic;

namespace Backtrace.Unity.Model.Attributes
{
    internal interface IScopeAttributeProvider
    {
        void GetAttributes(IDictionary<string, string> attributes);
    }
}
