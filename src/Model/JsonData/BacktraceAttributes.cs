using Backtrace.Newtonsoft.Linq;
using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Class instance to get a built-in attributes from current application
    /// </summary>
    public class BacktraceAttributes
    {
        /// <summary>
        /// Get built-in primitive attributes
        /// </summary>
        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        /// <summary>
        /// Get built-in complex attributes
        /// </summary>
        public Dictionary<string, object> ComplexAttributes = new Dictionary<string, object>();

        /// <summary>
        /// Create instance of Backtrace Attribute
        /// </summary>
        /// <param name="report">Received report</param>
        /// <param name="clientAttributes">Client's attributes (report and client)</param>
        public BacktraceAttributes(BacktraceReport report, Dictionary<string, object> clientAttributes)
        {
            if (report != null)
            {
                ConvertAttributes(report, clientAttributes);
                SetLibraryAttributes(report);
                SetExceptionAttributes(report);
            }
            //Environment attributes override user attributes            
            SetMachineAttributes();
            SetProcessAttributes();
        }
        private BacktraceAttributes() { }

        public static BacktraceAttributes Deserialize(JToken jToken)
        {
            var attributes = new Dictionary<string, object>();
            foreach (BacktraceJProperty keys in jToken)
            {
                attributes.Add(keys.Name, keys.Value.Value<string>());
            }
            return new BacktraceAttributes()
            {
                Attributes = attributes
            };
        }

        public BacktraceJObject ToJson()
        {
            var attr = new BacktraceJObject();
            foreach (var attribute in Attributes)
            {
                if (attribute.Value != null && attribute.Value.GetType() == typeof(bool))
                {
                    attr[attribute.Key] = (bool)attribute.Value;
                }
                else if (attribute.Value != null && TypeHelper.IsNumeric(attribute.Value.GetType()))
                {
                    attr[attribute.Key] = Convert.ToInt64(attribute.Value);
                }
                else
                {
                    attr[attribute.Key] = attribute.Value.ToString();
                }
            }
            return attr;
        }
        /// <summary>
        /// Set library attributes
        /// </summary>
        private void SetLibraryAttributes(BacktraceReport report)
        {
            if (!string.IsNullOrEmpty(report.Fingerprint))
            {
                Attributes["_mod_fingerprint"] = report.Fingerprint;
            }

            if (!string.IsNullOrEmpty(report.Factor))
            {
                Attributes["_mod_factor"] = report.Factor;
            }
            //A unique identifier of a machine
            Attributes["guid"] = GenerateMachineId();
            //Base name of application generating the report
            Attributes["application"] = Application.productName;
            Attributes["application.url"] = Application.absoluteURL;
            Attributes["application.company.name"] = Application.companyName;
            Attributes["application.data_path"] = Application.dataPath;
            Attributes["application.id"] = Application.identifier;
            Attributes["application.installer.name"] = Application.installerName;
            Attributes["application.internet_reachability"] = Application.internetReachability.ToString();
            Attributes["application.editor"] = Application.isEditor;
            Attributes["application.focused"] = Application.isFocused;
            Attributes["application.mobile"] = Application.isMobilePlatform;
            Attributes["application.playing"] = Application.isPlaying;
            Attributes["application.background"] = Application.runInBackground;
            Attributes["application.sandboxType"] = Application.sandboxType.ToString();
            Attributes["application.system.language"] = Application.systemLanguage.ToString();
            Attributes["application.unity.version"] = Application.unityVersion;
            Attributes["application.temporary_cache"] = Application.temporaryCachePath;
            Attributes["application.debug"] = Debug.isDebugBuild;
        }

        /// <summary>
        /// Generate unique machine identifier. Value should be with guid key in Attributes dictionary. 
        /// Machine id is equal to mac address of first network interface. If network interface in unvailable, random long will be generated.
        /// </summary>
        /// <returns>Machine uuid</returns>
        private string GenerateMachineId()
        {
            if (SystemInfo.deviceUniqueIdentifier != SystemInfo.unsupportedIdentifier)
            {
                return SystemInfo.deviceUniqueIdentifier;
            }
            var networkInterface =
                 NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up);

            PhysicalAddress physicalAddr = null;
            string macAddress = null;
            if (networkInterface == null
                || (physicalAddr = networkInterface.GetPhysicalAddress()) == null
                || string.IsNullOrEmpty(macAddress = physicalAddr.ToString()))
            {
                return Guid.NewGuid().ToString();
            }

            string hex = macAddress.Replace(":", string.Empty);
            var value = Convert.ToInt64(hex, 16);
            return GuidExtensions.FromLong(value).ToString();
        }

        /// <summary>
        /// Convert custom user attributes
        /// </summary>
        /// <param name="report">Received report</param>
        /// <param name="clientAttributes">Client's attributes (report and client)</param>
        /// <returns>Dictionary of custom user attributes </returns>
        private void ConvertAttributes(BacktraceReport report, Dictionary<string, object> clientAttributes)
        {
            var attributes = BacktraceReport.ConcatAttributes(report, clientAttributes);
            foreach (var attribute in attributes)
            {
                var type = attribute.Value.GetType();
                if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
                {
                    Attributes.Add(attribute.Key, attribute.Value);
                }
                else
                {
                    ComplexAttributes.Add(attribute.Key, attribute.Value);
                }
            }
            //add exception information to Complex attributes.
            if (report.ExceptionTypeReport)
            {
                ComplexAttributes.Add("Exception Properties", report.Exception);
            }
        }

        /// <summary>
        /// Set attributes from exception
        /// </summary>
        internal void SetExceptionAttributes(BacktraceReport report)
        {
            //there is no information to analyse
            if (report == null)
            {
                return;
            }
            Attributes["error.message"] = report.ExceptionTypeReport
                ? report.Exception.Message
                : report.Message;
        }

        internal void SetSceneInformation()
        {
            //The number of Scenes which have been added to the Build Settings. The Editor will contain Scenes that were open before entering playmode.
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                Attributes["scene.count.build"] = SceneManager.sceneCountInBuildSettings;
            }
            Attributes["scene.count"] = SceneManager.sceneCount;
            var activeScene = SceneManager.GetActiveScene();
            Attributes["scene.active"] = activeScene.name;
            Attributes["scene.active.loaded"] = activeScene.isLoaded;
        }

        /// <summary>
        /// Set attributes from current process
        /// </summary>
        private void SetProcessAttributes()
        {
            Attributes["gc.heap.used"] = GC.GetTotalMemory(false);
            Attributes["process.age"] = Math.Round(Time.realtimeSinceStartup);
        }

        private void SetGraphicCardInformation()
        {
            //This is the PCI device ID of the user's graphics card. Together with SystemInfo.graphicsDeviceVendorID, 
            //this number uniquely identifies a particular graphics card model. 
            //The number is the same across operating systems and driver versions.
            //Note that device IDs are only implemented on PC(Windows / Mac / Linux) platforms and on Android when running
            //Vulkan; on other platforms you'll have to do name-based detection if needed.
            Attributes["graphic.id"] = SystemInfo.graphicsDeviceID;
            Attributes["graphic.name"] = SystemInfo.graphicsDeviceName;
            Attributes["graphic.type"] = SystemInfo.graphicsDeviceType.ToString();
            Attributes["graphic.vendor"] = SystemInfo.graphicsDeviceVendor;
            Attributes["graphic.vendor.id"] = SystemInfo.graphicsDeviceVendorID;

            Attributes["graphic.driver.version"] = SystemInfo.graphicsDeviceVersion;
            Attributes["graphic.driver.version"] = SystemInfo.graphicsDeviceVersion;

            Attributes["graphic.memory"] = SystemInfo.graphicsMemorySize;
            Attributes["graphic.multithreaded"] = SystemInfo.graphicsMultiThreaded;

            Attributes["graphic.shader"] = SystemInfo.graphicsShaderLevel;
            Attributes["graphic.topUv"] = SystemInfo.graphicsUVStartsAtTop;


        }
        /// <summary>
        /// Set attributes about current machine
        /// </summary>
        private void SetMachineAttributes()
        {
            //collect battery data
            var batteryLevel = SystemInfo.batteryLevel == -1
                    ? -1
                    : SystemInfo.batteryLevel * 100;
            Attributes["battery.level"] = batteryLevel;
            Attributes["battery.status"] = SystemInfo.batteryStatus.ToString();

            if (SystemInfo.deviceModel != SystemInfo.unsupportedIdentifier)
            {
                Attributes["device.model"] = SystemInfo.deviceModel;
                // This is typically the "name" of the device as it appears on the networks.
                Attributes["device.name"] = SystemInfo.deviceName;
                Attributes["device.type"] = SystemInfo.deviceType.ToString();
            }
            SetGraphicCardInformation();

            //The processor architecture.
            string cpuArchitecture = SystemHelper.CpuArchitecture();
            if (!string.IsNullOrEmpty(cpuArchitecture))
            {
                Attributes["uname.machine"] = cpuArchitecture;
            }
            //Operating system name = such as "windows"
            Attributes["uname.sysname"] = SystemHelper.Name(cpuArchitecture);

            //The version of the operating system
            Attributes["uname.version"] = Environment.OSVersion.Version.ToString();
            Attributes["uname.fullname"] = SystemInfo.operatingSystem;
            Attributes["uname.family"] = SystemInfo.operatingSystemFamily.ToString();

            Attributes["cpu.count"] = SystemInfo.processorCount;
            Attributes["cpu.frequency"] = SystemInfo.processorFrequency;
            Attributes["cpu.brand"] = SystemInfo.processorType;

            Attributes["audio.supported"] = SystemInfo.supportsAudio;

            //Time when system was booted
            int boottime = Environment.TickCount;
            if (boottime <= 0)
            {
                boottime = int.MaxValue;
            }
            Attributes["cpu.boottime"] = boottime;

            //The hostname of the crashing system.
            Attributes["hostname"] = Environment.MachineName;
            if (SystemInfo.systemMemorySize != 0)
            {
                Attributes["vm.rss.size"] = SystemInfo.systemMemorySize * 1048576L;
            }
        }
    }
}
