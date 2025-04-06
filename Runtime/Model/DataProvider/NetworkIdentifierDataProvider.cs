using Backtrace.Unity.Extensions;
using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace Backtrace.Unity.Model.DataProvider
{
    internal class NetworkIdentifierDataProvider : IMachineIdentifierDataProvider
    {
        public string Get()
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