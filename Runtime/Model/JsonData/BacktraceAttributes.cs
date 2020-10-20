using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using Backtrace.Unity.Json;
using System;
using System.Collections.Generic;
#if !UNITY_WEBGL
using System.Linq;
using System.Net.NetworkInformation;
#endif
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
        public readonly Dictionary<string, string> Attributes;

        private static string _machineId;
        private static string MachineId
        {
            get
            {
                if (string.IsNullOrEmpty(_machineId))
                {
                    _machineId = GenerateMachineId();
                }
                return _machineId;
            }
        }


        internal const string APPLICATION_ATTRIBUTE_NAME = "application";
        /// <summary>
        /// Create instance of Backtrace Attribute
        /// </summary>
        /// <param name="report">Received report</param>
        /// <param name="clientAttributes">Client's attributes (report and client)</param>
        public BacktraceAttributes(BacktraceReport report, Dictionary<string, string> clientAttributes, bool onlyBuiltInAttributes = false)
        {
            if (clientAttributes == null)
            {
                clientAttributes = new Dictionary<string, string>();
            }
            Attributes = clientAttributes;

            if (report != null)
            {
                ConvertAttributes(report, clientAttributes);
                SetExceptionAttributes(report);
            }
            //Environment attributes override user attributes     
            SetLibraryAttributes(report);
            SetMachineAttributes(onlyBuiltInAttributes);
            SetProcessAttributes(onlyBuiltInAttributes);
            SetSceneInformation(onlyBuiltInAttributes);
        }
        private BacktraceAttributes() { }

        public BacktraceJObject ToJson()
        {
            return new BacktraceJObject(Attributes);
        }

        private void SetScriptingBackend()
        {
#if NET_STANDARD_2_0
            Attributes["scripting.backend"] = ".NET Standard 2.0";
#elif NET_4_6
            Attributes["scripting.backend"] = ".NET Framework 4.5";
#else
            Attributes["scripting.backend"] = ".NET Framework 3.5 equivalent";
#endif
        }
        /// <summary>
        /// Set library attributes
        /// </summary>
        private void SetLibraryAttributes(BacktraceReport report)
        {
            if (report != null)
            {
                if (!string.IsNullOrEmpty(report.Factor))
                {
                    Attributes["_mod_factor"] = report.Factor;
                }
                if (!string.IsNullOrEmpty(report.Fingerprint))
                {
                    Attributes["_mod_fingerprint"] = report.Fingerprint;
                }
            }

            Attributes["guid"] = MachineId;
            SetScriptingBackend();

            //Base name of application generating the report
            Attributes[APPLICATION_ATTRIBUTE_NAME] = Application.productName;
            Attributes["application.version"] = Application.version;
            Attributes["application.url"] = Application.absoluteURL;
            Attributes["application.company.name"] = Application.companyName;
            Attributes["application.data_path"] = Application.dataPath;
            Attributes["application.id"] = Application.identifier;
            Attributes["application.installer.name"] = Application.installerName;
            Attributes["application.internet_reachability"] = Application.internetReachability.ToString();
            Attributes["application.editor"] = Application.isEditor.ToString();
            Attributes["application.focused"] = Application.isFocused.ToString();
            Attributes["application.mobile"] = Application.isMobilePlatform.ToString();
            Attributes["application.playing"] = Application.isPlaying.ToString();
            Attributes["application.background"] = Application.runInBackground.ToString();
            Attributes["application.sandboxType"] = Application.sandboxType.ToString();
            Attributes["application.system.language"] = Application.systemLanguage.ToString();
            Attributes["application.unity.version"] = Application.unityVersion;
            Attributes["application.debug"] = Debug.isDebugBuild.ToString();
#if !UNITY_SWITCH
            Attributes["application.temporary_cache"] = Application.temporaryCachePath;
#endif


        }

        /// <summary>
        /// Generate unique machine identifier. Value should be with guid key in Attributes dictionary. 
        /// Machine id is equal to mac address of first network interface. If network interface in unvailable, random long will be generated.
        /// </summary>
        /// <returns>Machine uuid</returns>
        private static string GenerateMachineId()
        {
#if !UNITY_WEBGL && !UNITY_SWITCH
            // DeviceUniqueIdentifier will return "Switch" on Nintendo Switch
            // try to generate random guid instead
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
#else
            return Guid.NewGuid().ToString();
#endif
        }

        /// <summary>
        /// Convert custom user attributes
        /// </summary>
        /// <param name="report">Received report</param>
        /// <param name="clientAttributes">Client's attributes (report and client)</param>
        /// <returns>Dictionary of custom user attributes </returns>
        private void ConvertAttributes(BacktraceReport report, Dictionary<string, string> clientAttributes)
        {
            var reportAttributes = BacktraceReport.ConcatAttributes(report, clientAttributes);
            foreach (var attribute in reportAttributes)
            {
                Attributes[attribute.Key] = attribute.Value;
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

            // detect exception type
            var errorType = "error.type";
            if (!report.ExceptionTypeReport)
            {
                Attributes[errorType] = "Message";
                return;
            }
            if (report.Exception is BacktraceUnhandledException)
            {
                if ((report.Exception as BacktraceUnhandledException).Classifier == "ANRException")
                {
                    Attributes[errorType] = "Hang";
                }
                else
                {
                    Attributes[errorType] = "Unhandled exception";
                }
            } else
            {
                Attributes[errorType] = "Exception";
            }
        }

        internal void SetSceneInformation(bool onlyBuiltInAttributes = false)
        {
            //The number of Scenes which have been added to the Build Settings. The Editor will contain Scenes that were opened before entering playmode.
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                Attributes["scene.count.build"] = SceneManager.sceneCountInBuildSettings.ToString();
            }
            Attributes["scene.count"] = SceneManager.sceneCount.ToString();
            if (onlyBuiltInAttributes)
            {
                return;
            }
            var activeScene = SceneManager.GetActiveScene();
            Attributes["scene.active"] = activeScene.name;
            Attributes["scene.buildIndex"] = activeScene.buildIndex.ToString();
#if UNITY_2018_4_OR_NEWER
            Attributes["scene.handle"] = activeScene.handle.ToString();
#endif
            Attributes["scene.isDirty"] = activeScene.isDirty.ToString();
            Attributes["scene.isLoaded"] = activeScene.isLoaded.ToString();
            Attributes["scene.name"] = activeScene.name;
            Attributes["scene.path"] = activeScene.path;
        }

        /// <summary>
        /// Set attributes from current process
        /// </summary>
        private void SetProcessAttributes(bool onlyBuiltInAttributes = false)
        {
            if (onlyBuiltInAttributes)
            {
                return;
            }
            Attributes["gc.heap.used"] = GC.GetTotalMemory(false).ToString();
            Attributes["process.age"] = Math.Round(Time.realtimeSinceStartup).ToString();
        }

        private void SetGraphicCardInformation()
        {
            //This is the PCI device ID of the user's graphics card. Together with SystemInfo.graphicsDeviceVendorID, 
            //this number uniquely identifies a particular graphics card model. 
            //The number is the same across operating systems and driver versions.
            //Note that device IDs are only implemented on PC(Windows / Mac / Linux) platforms and on Android when running
            //Vulkan; on other platforms you'll have to do name-based detection if needed.
            Attributes["graphic.id"] = SystemInfo.graphicsDeviceID.ToString();
            Attributes["graphic.name"] = SystemInfo.graphicsDeviceName;
            Attributes["graphic.type"] = SystemInfo.graphicsDeviceType.ToString();
            Attributes["graphic.vendor"] = SystemInfo.graphicsDeviceVendor;
            Attributes["graphic.vendor.id"] = SystemInfo.graphicsDeviceVendorID.ToString();

            Attributes["graphic.driver.version"] = SystemInfo.graphicsDeviceVersion;

            Attributes["graphic.memory"] = SystemInfo.graphicsMemorySize.ToString();
            Attributes["graphic.multithreaded"] = SystemInfo.graphicsMultiThreaded.ToString();

            Attributes["graphic.shader"] = SystemInfo.graphicsShaderLevel.ToString();
            Attributes["graphic.topUv"] = SystemInfo.graphicsUVStartsAtTop.ToString();


        }
        /// <summary>
        /// Set attributes about current machine
        /// </summary>
        private void SetMachineAttributes(bool onlyBuiltInAttributes = false)
        {
            if (onlyBuiltInAttributes)
            {
                //collect battery data
                var batteryLevel = SystemInfo.batteryLevel == -1
                        ? -1
                        : SystemInfo.batteryLevel * 100;
                Attributes["battery.level"] = batteryLevel.ToString();
                Attributes["battery.status"] = SystemInfo.batteryStatus.ToString();
            }

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
            Attributes["cpu.count"] = SystemInfo.processorCount.ToString();
            Attributes["cpu.frequency"] = SystemInfo.processorFrequency.ToString();
            Attributes["cpu.brand"] = SystemInfo.processorType;

            Attributes["audio.supported"] = SystemInfo.supportsAudio.ToString();

            //Time when system was booted
            int boottime = Environment.TickCount;
            if (boottime <= 0)
            {
                boottime = int.MaxValue;
            }
            Attributes["cpu.boottime"] = boottime.ToString();

            //The hostname of the crashing system.
            Attributes["hostname"] = Environment.MachineName;
#if !UNITY_ANDROID
            if (SystemInfo.systemMemorySize != 0)
            {
                //number of kilobytes that application is using.
                Attributes["vm.rss.size"] = (SystemInfo.systemMemorySize * 1048576L).ToString();
            }
#endif
        }
    }
}
