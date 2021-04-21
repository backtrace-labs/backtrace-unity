using System.Collections.Generic;

namespace Backtrace.Unity.Model.Attributes
{
    internal interface IDynamicAttributeProvider
    {
        void GetAttributes(IDictionary<string, string> attributes);
    }
}
