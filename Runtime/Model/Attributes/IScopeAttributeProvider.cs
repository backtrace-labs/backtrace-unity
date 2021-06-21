using System.Collections.Generic;

namespace Backtrace.Unity.Model.Attributes
{
    public interface IScopeAttributeProvider
    {
        void GetAttributes(IDictionary<string, string> attributes);
    }
}
