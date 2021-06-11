using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Backtrace.Unity.Model.Attributes
{
    internal sealed class ProcessAttributeProvider : IDynamicAttributeProvider
    {
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            attributes["gc.heap.used"] = GC.GetTotalMemory(false).ToString(CultureInfo.InvariantCulture);
            attributes["process.age"] = Math.Round(Time.realtimeSinceStartup).ToString(CultureInfo.InvariantCulture);
        }
    }
}
