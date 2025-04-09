using Backtrace.Unity.Common;
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
#if UNITY_ANDROID && !UNITY_EDITOR

            using (var build = new AndroidJavaClass("android.os.Build"))
            {
                attributes["device.manufacturer"] = build.GetStatic<string>("MANUFACTURER").ToString();
                attributes["device.brand"] = build.GetStatic<string>("BRAND").ToString();
                attributes["device.product"] = build.GetStatic<string>("PRODUCT").ToString();
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    attributes["uname.version"] = version.GetStatic<string>("RELEASE").ToString();

                    var deviceSdkVersion = version.GetStatic<int>("SDK_INT");
                    attributes["device.sdk"] = deviceSdkVersion.ToString();
                    if(deviceSdkVersion >= 21) 
                    {
                        string[] supportedAbis = build.GetStatic<string[]>("SUPPORTED_ABIS");
                        
                        if (supportedAbis != null && supportedAbis.Length > 0)
                        {
                            attributes["device.abi"] =  supportedAbis[0];
                        }
                        
                    }
                }
            }
            attributes["uname.fullname"] = Environment.OSVersion.Version.ToString();
#else
            if (SystemInfo.systemMemorySize != 0)
            {
                //number of kilobytes that application is using.
                attributes["vm.rss.size"] = (SystemInfo.systemMemorySize * 1048576L).ToString(CultureInfo.InvariantCulture);
            }
#endif


#if UNITY_IOS && !UNITY_EDITOR
            attributes["uname.version"] = UnityEngine.iOS.Device.systemVersion;
            attributes["uname.fullname"] = Environment.OSVersion.Version.ToString();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            attributes["uname.fullname"] = Environment.OSVersion.Version.ToString();
#endif
        }

        private void IncludeGraphicCardInformation(IDictionary<string, string> attributes)
        {
            // if a graphic card is not available
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return;
            }
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
