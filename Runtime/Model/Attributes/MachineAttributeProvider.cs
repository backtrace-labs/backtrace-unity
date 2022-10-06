using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using Backtrace.Unity.Runtime.Native.Android;

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
            attributes["uname.version"] = GetVersionString();
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

        // Helper functions for getting the version number.
        private string GetVersionString()
        {
#if UNITY_ANDROID
            var APILevelToVersion = new Dictionary<int, string>(){
                { 0, "N.a." },
                { 1, "1.0" },
                { 2, "1.1" },
                { 3, "1.5" },
                { 4, "1.6" },
                { 5, "2.0" },
                { 6, "2.0.1" },
                { 7, "2.1" },
                { 8, "2.2" },
                { 9, "2.3" },
                { 10, "2.3.3" },
                { 11, "3.0" },
                { 12, "3.1" },
                { 13, "3.2" },
                { 14, "4.0" },
                { 15, "4.0.3" },
                { 16, "4.1" },
                { 17, "4.2" },
                { 18, "4.3" },
                { 19, "4.4" },
                { 20, "4.4" },
                { 21, "5.0" },
                { 22, "5.1" },
                { 23, "6.0" },
                { 24, "7.0" },
                { 25, "7.1.1" },
                { 26, "8.0" },
                { 27, "8" },
                { 28, "9" },
                { 29, "10" },
                { 30, "11" },
                { 31, "12" },
                { 32, "12L" },
                { 33, "13" },
                { 10000, "Next" },
            };

            return APILevelToVersion.GetValueOrDefault(NativeClient.GetAndroidSDKLevel(), "unknown");
#elif UNITY_IOS || UNITY_
            // For exaple: "iPhone OS 8.4" on iOS 8.4
            var match = Regex.Match(SystemInfo.operatingSystem, @"\d+(?:\.\d+)+");

            return match.Success ? match.Value : "unknown";
#else
            // Default case, such as for Windows/Linux/PS5/etc
            return Environment.OSVersion.Version.ToString();
#endif
        }
    }
}
