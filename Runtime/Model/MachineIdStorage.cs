using Backtrace.Unity.Extensions;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Backtrace Machine Id storage
    /// </summary>
    internal class MachineIdStorage
    {
        /// <summary>
        /// Player prefs machine identifier key
        /// </summary>
        internal const string MachineIdentifierKey = "backtrace-machine-id";

        /// <summary>
        /// Generate unique machine id. 
        /// </summary>
        /// <returns>Unique machine id Guid in a string format</returns>
        internal string GenerateMachineId()
        {
            var storageMachineId = FetchMachineIdFromStorage();
            if (!string.IsNullOrEmpty(storageMachineId))
            {
                return storageMachineId;
            }

#if !UNITY_WEBGL && !UNITY_SWITCH
            var unityIdentifier = UseUnityIdentifier();
            if (!GuidHelper.IsNullOrEmpty(unityIdentifier))
            {
                StoreMachineId(unityIdentifier);
                return unityIdentifier;
            }
            var networkIdentifier = UseNetworkingIdentifier();
            if (!GuidHelper.IsNullOrEmpty(networkIdentifier))
            {
                StoreMachineId(networkIdentifier);
                return networkIdentifier;
            }
#endif
            var backtraceRandomIdentifier = Guid.NewGuid().ToString();
            StoreMachineId(backtraceRandomIdentifier);
            return backtraceRandomIdentifier;
        }


        /// <summary>
        /// Fetch a machine id in the internal storage
        /// </summary>
        /// <returns>machine identifier in the GUID string format</returns>
        private string FetchMachineIdFromStorage()
        {
            return PlayerPrefs.GetString(MachineIdentifierKey);
        }

        /// <summary>
        /// Set a machine id in the internal storage
        /// </summary>
        /// <param name="machineId">machine identifier</param>
        private void StoreMachineId(string machineId)
        {
            PlayerPrefs.SetString(MachineIdentifierKey, machineId);
        }

        /// <summary>
        /// Use Unity device identifier to generate machine identifier
        /// </summary>
        /// <returns>Unity machine identifier if the device identifier is supported. Otherwise null</returns>
        protected virtual string UseUnityIdentifier()
        {
            if (SystemInfo.deviceUniqueIdentifier == SystemInfo.unsupportedIdentifier)
            {
                return null;
            }
            return SystemInfo.deviceUniqueIdentifier;
        }

        /// <summary>
        /// Use Networking interface to generate machine identifier - MAC number from the networking interface.
        /// </summary>
        /// <returns>Machine id - MAC in a GUID format. If the networking interface is not available then it returns null.</returns>
        protected virtual string UseNetworkingIdentifier()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up);

            foreach (var @interface in interfaces)
            {
                var physicalAddress = @interface.GetPhysicalAddress();
                if (physicalAddress == null)
                {
                    continue;
                }
                var macAddress = physicalAddress.ToString();
                if (string.IsNullOrEmpty(macAddress))
                {
                    continue;
                }
                string hex = macAddress.Replace(":", string.Empty);
                var value = Convert.ToInt64(hex, 16);
                return GuidHelper.FromLong(value).ToString();
            }

            return null;
        }
    }
}
