using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Backtrace.Unity.Model.Attributes
{
    internal sealed class MachineAttributeProvider : IScopeAttributeProvider
    {
        private readonly MachineIdStorage _machineIdStorage = new MachineIdStorage();
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            attributes["guid"] = _machineIdStorage.GenerateMachineId();
            IncludeGraphicCardInformation(attributes);
            IncludeOsInformation(attributes);
        }

        private void IncludeOsInformation(IDictionary<string, string> attributes)
        {
            //The processor architecture.
            string cpuArchitecture = SystemHelper.CpuArchitecture();
            if (!string.IsNullOrEmpty(cpuArchitecture))
            {
                attributes["uname.machine"] = cpuArchitecture;
            }
            //Operating system name = such as "windows"
            attributes["uname.sysname"] = SystemHelper.Name();

            //The version of the operating system
            attributes["uname.version"] = Environment.OSVersion.Version.ToString();
            attributes["uname.fullname"] = SystemInfo.operatingSystem;
            attributes["uname.family"] = SystemInfo.operatingSystemFamily.ToString();
            attributes["cpu.count"] = SystemInfo.processorCount.ToString(CultureInfo.InvariantCulture);
            attributes["cpu.frequency"] = SystemInfo.processorFrequency.ToString(CultureInfo.InvariantCulture);
            attributes["cpu.brand"] = SystemInfo.processorType;

            attributes["audio.supported"] = SystemInfo.supportsAudio.ToString(CultureInfo.InvariantCulture);

            //Time when system was booted
            int boottime = Environment.TickCount;
            if (boottime <= 0)
            {
                boottime = int.MaxValue;
            }
            attributes["cpu.boottime"] = boottime.ToString(CultureInfo.InvariantCulture);

            //The hostname of the crashing system.
            attributes["hostname"] = Environment.MachineName;
#if !UNITY_ANDROID
            if (SystemInfo.systemMemorySize != 0)
            {
                //number of kilobytes that application is using.
                attributes["vm.rss.size"] = (SystemInfo.systemMemorySize * 1048576L).ToString(CultureInfo.InvariantCulture);
            }
#endif
        }
        private void IncludeGraphicCardInformation(IDictionary<string, string> attributes)
        {

            //This is the PCI device ID of the user's graphics card. Together with SystemInfo.graphicsDeviceVendorID, 
            //this number uniquely identifies a particular graphics card model. 
            //The number is the same across operating systems and driver versions.
            //Note that device IDs are only implemented on PC(Windows / Mac / Linux) platforms and on Android when running
            //Vulkan; on other platforms you'll have to do name-based detection if needed.
            attributes["graphic.id"] = SystemInfo.graphicsDeviceID.ToString(CultureInfo.InvariantCulture);
            attributes["graphic.name"] = SystemInfo.graphicsDeviceName;
            attributes["graphic.type"] = SystemInfo.graphicsDeviceType.ToString();
            attributes["graphic.vendor"] = SystemInfo.graphicsDeviceVendor;
            attributes["graphic.vendor.id"] = SystemInfo.graphicsDeviceVendorID.ToString(CultureInfo.InvariantCulture);

            attributes["graphic.driver.version"] = SystemInfo.graphicsDeviceVersion;

            attributes["graphic.memory"] = SystemInfo.graphicsMemorySize.ToString(CultureInfo.InvariantCulture);
            attributes["graphic.multithreaded"] = SystemInfo.graphicsMultiThreaded.ToString(CultureInfo.InvariantCulture);

            attributes["graphic.shader"] = SystemInfo.graphicsShaderLevel.ToString(CultureInfo.InvariantCulture);
            attributes["graphic.topUv"] = SystemInfo.graphicsUVStartsAtTop.ToString(CultureInfo.InvariantCulture);
        }
    }
}
