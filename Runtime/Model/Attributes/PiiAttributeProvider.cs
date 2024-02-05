using System.Collections.Generic;
using UnityEngine;

namespace Backtrace.Unity.Model.Attributes
{
    /// <summary>
    /// Generate device information that has user information dynamically
    /// to let user override them
    /// </summary>
    internal sealed class PiiAttributeProvider : IScopeAttributeProvider
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
                attributes["device.machine"] = SystemInfo.deviceModel;
                attributes["device.type"] = SystemInfo.deviceType.ToString();
            }
        }
    }
}
