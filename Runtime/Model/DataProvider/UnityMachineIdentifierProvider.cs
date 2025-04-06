using Backtrace.Unity.Extensions;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;

namespace Backtrace.Unity.Model.DataProvider
{
    internal class UnityMachineIdentifierProvider : IMachineIdentifierDataProvider
    {
        private readonly string _deviceUniqueIdentifier;
        internal UnityMachineIdentifierProvider() : this(SystemInfo.deviceUniqueIdentifier) { }

        internal UnityMachineIdentifierProvider(string machineIdentifier)
        {
            _deviceUniqueIdentifier = machineIdentifier;
        }
        public string Get()
        {
            if (!IsValidIdentifier())
            {
                return null;
            }

            if (Guid.TryParse(_deviceUniqueIdentifier, out Guid unityUuidGuid))
            {
                return unityUuidGuid.ToString();
            }
            return GuidHelper.FromString(_deviceUniqueIdentifier).ToString();
        }

        private bool IsValidIdentifier()
        {
            return _deviceUniqueIdentifier != SystemInfo.unsupportedIdentifier && !string.IsNullOrEmpty(_deviceUniqueIdentifier);
        }
    }
}
