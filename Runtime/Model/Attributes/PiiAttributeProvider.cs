using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Attributes
{
    /// <summary>
    /// Generate device information that has user information dynamically
    /// to let user override them
    /// </summary>
    internal sealed class PiiAttributeProvider : IDynamicAttributeProvider
    {
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            if (SystemInfo.deviceModel != SystemInfo.unsupportedIdentifier)
            {
                attributes["device.model"] = SystemInfo.deviceModel;
                // This is typically the "name" of the device as it appears on the networks.
                attributes["device.name"] = SystemInfo.deviceName;
                attributes["device.type"] = SystemInfo.deviceType.ToString();
            }
        }
    }
}
