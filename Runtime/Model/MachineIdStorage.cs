using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model.DataProvider;
using System;

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

        private readonly ISessionStorageDataProvider _sessionStorageDataProvider;
        private readonly IMachineIdentifierProvider[] _machineIdentifierDataProviders;

        internal MachineIdStorage() : this(
            new IMachineIdentifierProvider[] { new UnityMachineIdentifierProvider(), new NetworkIdentifierProvider() },
            new SessionStorageDataProvider())
        { }
        internal MachineIdStorage(IMachineIdentifierProvider[] machineIdentifierDataProviders, ISessionStorageDataProvider sessionStorageDataProvider)
        {
            _machineIdentifierDataProviders = machineIdentifierDataProviders;
            _sessionStorageDataProvider = sessionStorageDataProvider;
        }

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
            foreach (var machineIdentifierProvider in _machineIdentifierDataProviders)
            {
                var identifier = machineIdentifierProvider.Get();
                if (!GuidHelper.IsNullOrEmpty(identifier))
                {
                    StoreMachineId(identifier);
                    return identifier;
                }
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
            var storedMachineId = _sessionStorageDataProvider.GetString(MachineIdentifierKey);
            // in the previous version of the SDK, the stored machine id could be invalid
            // to fix the problem, we want to verify if the id is valid and if isn't, fix it.
            if (string.IsNullOrEmpty(storedMachineId) || Guid.TryParse(storedMachineId, out Guid _))
            {
                return storedMachineId;
            }

            var machineId = GuidHelper.FromString(storedMachineId).ToString();
            StoreMachineId(machineId);
            return machineId;
        }

        /// <summary>
        /// Set a machine id in the internal storage
        /// </summary>
        /// <param name="machineId">machine identifier</param>
        private void StoreMachineId(string machineId)
        {
            _sessionStorageDataProvider.SetString(MachineIdentifierKey, machineId);
        }
    }
}
