using System.Collections.Generic;

namespace Backtrace.Unity.Model.Attributes
{
    public interface IDynamicAttributeProvider
    {
        void GetAttributes(IDictionary<string, string> attributes);
    }
}
