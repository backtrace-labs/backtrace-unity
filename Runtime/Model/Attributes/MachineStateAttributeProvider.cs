using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Backtrace.Unity.Model.Attributes
{
    internal sealed class MachineStateAttributeProvider : IDynamicAttributeProvider
    {
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            //collect battery data
            var batteryLevel = SystemInfo.batteryLevel == -1
                    ? -1
                    : SystemInfo.batteryLevel * 100;
            attributes["battery.level"] = batteryLevel.ToString(CultureInfo.InvariantCulture);
            attributes["battery.status"] = SystemInfo.batteryStatus.ToString();
        }
    }
}
