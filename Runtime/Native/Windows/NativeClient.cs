using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Backtrace.Unity.Runtime.Native.Windows
{
    internal class NativeClient : INativeClient
    {
        [DllImport("BacktraceCrashpadWindows", EntryPoint = "Initialize")]
        private static extern bool Initialize(string submissionUrl, string databasePath, string handlerPath, string[] attachments, int attachmentSize);

        [DllImport("BacktraceCrashpadWindows", EntryPoint = "AddAttribute")]
        private static extern bool AddAttribute(string key, string value);

        [DllImport("BacktraceCrashpadWindows", EntryPoint = "DumpWithoutCrash")]
        private static extern void NativeReport(string message, bool setMainThreadAsFaultingThread);

        // Last Backtrace client update time 
        volatile internal float _lastUpdateTime;

        /// <summary>
        /// Determine if the ANR background thread should be disabled or not 
        /// for some period of time.
        /// This option will be used by the native client implementation
        /// once application goes to background/foreground
        /// </summary>
        volatile internal bool _preventAnr = false;

        /// <summary>
        /// Determine if ANR thread should exit
        /// </summary>
        volatile internal bool _stopAnr = false;

        private Thread _anrThread;

        private readonly BacktraceConfiguration _configuration;
        private bool _captureNativeCrashes = false;
        public NativeClient(string gameObjectName, BacktraceConfiguration configuration, IDictionary<string, string> clientAttributes, IEnumerable<string> attachments)
        {
            _configuration = configuration;
            HandleNativeCrashes(clientAttributes, attachments);
            HandleAnr();
        }

        private void HandleNativeCrashes(IDictionary<string, string> clientAttributes, IEnumerable<string> attachments)
        {
            if (!_configuration.CaptureNativeCrashes)
            {
                return;
            }
            var databasePath = _configuration.CrashpadDatabasePath;
            if (string.IsNullOrEmpty(databasePath) || !Directory.Exists(_configuration.GetFullDatabasePath()))
            {
                Debug.LogWarning("Backtrace native integration status: database path doesn't exist");
                return;
            }

            var crashpadHandlerPath = GetDefaultPathToCrashpadHandler();
            if (!File.Exists(crashpadHandlerPath))
            {
                Debug.LogWarning("Backtrace native integration status: Cannot find path to Crashpad handler.");
                return;
            }

            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();

            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }

            // reassign to captureNativeCrashes
            // to avoid doing anything on crashpad binary, when crashpad isn't available
            _captureNativeCrashes = Initialize(
                minidumpUrl,
                databasePath,
                crashpadHandlerPath,
                attachments.ToArray(),
                attachments.Count());

            if (!_captureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Crashpad client");
                return;
            }

            foreach (var attribute in clientAttributes)
            {
                AddAttribute(attribute.Key, attribute.Value);
            }
            // add exception type to crashes handled by crashpad - all exception handled by crashpad 
            // by default we setting this option here, to set error.type when unexpected crash happen (so attribute will present)
            // otherwise in other methods - ANR detection, OOM handler, we're overriding it and setting it back to "crash"

            // warning 
            // don't add attributes that can change over the time to initialization method attributes. Crashpad will prevent from 
            // overriding them on game runtime. ANRs/OOMs methods can override error.type attribute, so we shouldn't pass error.type 
            // attribute via attributes parameters.
            AddAttribute("error.type", "Crash");
        }

        private string GetDefaultPathToCrashpadHandler()
        {
            const string crashpadHandlerName = "crashpad_handler.dll";
            const string pluginDir = "Plugins";
            string architecture = IntPtr.Size == 8 ? "x86_64" : "x86";

            string pluginPath = Path.Combine(pluginDir, architecture);
            string pluginHandlerPath = Path.Combine(pluginPath, crashpadHandlerName);

            // generate full path to .dll file in plugins dir.
            return Path.Combine(Application.dataPath, pluginHandlerPath);

        }
        public void Disable()
        {
            if (_anrThread != null)
            {
                _stopAnr = true;
            }
            return;
        }

        public void GetAttributes(IDictionary<string, string> attributes)
        {
            return;
        }

        public void HandleAnr(string gameObjectName = "", string callbackName = "")
        {
            if (!_captureNativeCrashes || !_configuration.HandleANR)
            {
                return;
            }

            bool reported = false;
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _anrThread = new Thread(() =>
            {
                float lastUpdatedCache = 0;
                while (_anrThread.IsAlive && _stopAnr == false)
                {
                    if (!_preventAnr)
                    {
                        if (lastUpdatedCache == 0)
                        {
                            lastUpdatedCache = _lastUpdateTime;
                        }
                        else if (lastUpdatedCache == _lastUpdateTime)
                        {
                            if (!reported)
                            {

                                reported = true;
                                if (AndroidJNI.AttachCurrentThread() == 0)
                                {
                                    // set temporary attribute to "Hang"
                                    AddAttribute("error.type", "Hang");

                                    NativeReport("ANRException: Blocked thread detected.", true);
                                    // update error.type attribute in case when crash happen 
                                    SetAttribute("error.type", "Crash");
                                }
                            }
                        }
                        else
                        {
                            reported = false;
                        }

                        lastUpdatedCache = _lastUpdateTime;
                    }
                    else if (lastUpdatedCache != 0)
                    {
                        // make sure when ANR happened just after going to foreground
                        // we won't false positive ANR report
                        lastUpdatedCache = 0;
                    }
                    Thread.Sleep(5000);
                }
            });
            _anrThread.IsBackground = true;
            _anrThread.Start();
            return;
        }

        public bool OnOOM()
        {
            return false;
        }

        public void PauseAnrThread(bool state)
        {
            _preventAnr = state;
        }

        public void SetAttribute(string key, string value)
        {
            if (!_captureNativeCrashes || string.IsNullOrEmpty(key))
            {
                return;
            }
            // avoid null reference in crashpad source code
            if (value == null)
            {
                value = string.Empty;
            }

            AddAttribute(key, value);
        }

        public void UpdateClientTime(float time)
        {
            _lastUpdateTime = time;
        }
    }
}
