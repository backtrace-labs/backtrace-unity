﻿#if UNITY_STANDALONE_WIN
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Model.Breadcrumbs.Storage;
using Backtrace.Unity.Runtime.Native.Base;
using Backtrace.Unity.Services;
using Backtrace.Unity.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Runtime.Native.Windows
{
    internal sealed class NativeClient : NativeClientBase, INativeClient
    {
        [Serializable]
        private class ScopedAttributesContainer
        {
            public List<string> Keys = new List<string>();
        }
        /// <summary>
        /// Scoped attributes key - Backtrace Windows native client uses this key
        /// to store a list of attributes stored by user/library
        /// </summary>
        private const string ScopedAttributeListKey = "backtrace-scoped-attributes";

        /// <summary>
        /// Scoped attributes pattern - we prefer to avoid collision in attributes and avoid using
        /// popular names. Instead we're adding a prefix "bt-" to make sure we won't override user preferences
        /// </summary>
        private const string ScopedAttributesPattern = "bt-{0}";

        /// <summary>
        /// Application version storage key
        /// </summary>
        internal const string VersionKey = "backtrace-app-version";

        /// <summary>
        /// Application UUID storage key
        /// </summary>
        internal const string MachineUuidKey = "backtrace-uuid";

        /// <summary>
        /// Application session id storage key
        /// </summary>
        internal const string SessionKey = "backtrace-session-id";


        [DllImport("BacktraceCrashpadWindows", EntryPoint = "Initialize")]
        private static extern bool Initialize(string submissionUrl, string databasePath, string handlerPath, string[] attachments, int attachmentSize);

        [DllImport("BacktraceCrashpadWindows", EntryPoint = "AddAttribute")]
        private static extern bool AddNativeAttribute(string key, string value);

        [DllImport("BacktraceCrashpadWindows", EntryPoint = "DumpWithoutCrash")]
        private static extern void NativeReport(string message, bool setMainThreadAsFaultingThread);

        public NativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs, IDictionary<string, string> clientAttributes, IEnumerable<string> attachments) : base(configuration, breadcrumbs)
        {
            CleanScopedAttributes();
            HandleNativeCrashes(clientAttributes, attachments);
            AddScopedAttributes(clientAttributes);
            if (!configuration.ReportFilterType.HasFlag(ReportFilterType.Hang))
            {
                HandleAnr();
            }
        }

        private void HandleNativeCrashes(IDictionary<string, string> clientAttributes, IEnumerable<string> attachments)
        {
            var integrationDisabled = !_configuration.CaptureNativeCrashes || !_configuration.Enabled;
            if (integrationDisabled)
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

            CaptureNativeCrashes = Initialize(
                minidumpUrl,
                databasePath,
                crashpadHandlerPath,
                attachments.ToArray(),
                attachments.Count());

            if (!CaptureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize Crashpad client");
                return;
            }

            foreach (var attribute in clientAttributes)
            {
                AddNativeAttribute(attribute.Key, attribute.Value);
            }

            // add exception type to crashes handled by crashpad - all exception handled by crashpad 
            // by default we setting this option here, to set error.type when unexpected crash happen (so attribute will present)
            // otherwise in other methods - ANR detection, OOM handler, we're overriding it and setting it back to "crash"

            // warning 
            // don't add attributes that can change over the time to initialization method attributes. Crashpad will prevent from 
            // overriding them on game runtime. ANRs/OOMs methods can override error.type attribute, so we shouldn't pass error.type 
            // attribute via attributes parameters.
            AddNativeAttribute(ErrorTypeAttribute, CrashType);
        }
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            return;
        }

        public void HandleAnr()
        {
            var anrDisabled =
#if UNITY_STANDALONE_WIN
                !CaptureNativeCrashes || !_configuration.HandleANR;
#else
                true;
#endif
            if (anrDisabled)
            {
                return;
            }

            bool reported = false;
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            AnrThread = new Thread(() =>
            {
                float lastUpdatedCache = 0;
                while (AnrThread.IsAlive && StopAnr == false)
                {
                    if (!PreventAnr)
                    {
                        if (lastUpdatedCache == 0)
                        {
                            lastUpdatedCache = LastUpdateTime;
                        }
                        else if (lastUpdatedCache == LastUpdateTime)
                        {
                            if (!reported)
                            {
                                OnAnrDetection();
                                reported = true;
                                // set temporary attribute to "Hang"
                                AddNativeAttribute(ErrorTypeAttribute, HangType);

                                NativeReport(AnrMessage, true);
                                // update error.type attribute in case when crash happen 
                                AddNativeAttribute(ErrorTypeAttribute, HangType);
                            }
                        }
                        else
                        {
                            reported = false;
                        }

                        lastUpdatedCache = LastUpdateTime;
                    }
                    else if (lastUpdatedCache != 0)
                    {
                        // make sure when ANR happened just after going to foreground
                        // we won't false positive ANR report
                        lastUpdatedCache = 0;
                    }
                    Thread.Sleep(AnrWatchdogTimeout);
                }
            });
            AnrThread.IsBackground = true;
            AnrThread.Start();
            return;
        }

        public bool OnOOM()
        {
            return false;
        }

        public void SetAttribute(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            // avoid null reference in crashpad source code
            if (value == null)
            {
                value = string.Empty;
            }
            AddAttributes(key, value);
        }

        /// <summary>
        /// Read directory structure in the native crash directory and send new crashes to Backtrace
        /// </summary>
        public static IEnumerator SendUnhandledGameCrashesOnGameStartup(ICollection<string> clientAttachments, string breadcrumbPath, string databasePath, IBacktraceApi backtraceApi)
        {
            // Path to the native crash directory
            string nativeCrashesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    string.Format("Temp/{0}/{1}/crashes", Application.companyName, Application.productName));

            if (string.IsNullOrEmpty(nativeCrashesDir) || !Directory.Exists(nativeCrashesDir))
            {
                yield break;
            }

            var attachments = clientAttachments == null
                ? new List<string>()
                : new List<string>(clientAttachments);

            // make sure - when user close game in the middle of sending data, the library won't have a chance to clean up temporary breadcurmb
            // file. Becuase of that we prefer to always check if we need to clean something that left in the previous application session

            string breadcrumbsCopyName = string.Format("{0}-1", BacktraceStorageLogManager.BreadcrumbLogFilePrefix);
            string breadcrumbCopyPath = Path.Combine(databasePath, breadcrumbsCopyName);
            if (File.Exists(breadcrumbCopyPath))
            {
                File.Delete(breadcrumbCopyPath);
            }


            // determine if handler should create a copy of a breadcrumb file 
            // on the application startup. This check also prevents a situation when
            // algorithm will try to copy a breacrumb file when a breadcrumbs file doesn't exist
            // Client prefers to make a copy of a breadcrumb file in the database directory. Otherwise, if database
            // for any reason in new session is not available, algorithm shouldn't make a copy. 
            bool requireBreadcrumbsCopy = string.IsNullOrEmpty(breadcrumbPath) || string.IsNullOrEmpty(databasePath) ? false : true;
            bool copiedFile = false;

            var crashDirs = Directory.GetDirectories(nativeCrashesDir);

            IDictionary<string, string> attributes = GetScopedAttributes();
            // be sure that error.type attribute provided by default by our library
            // is always present in native attributes.
            attributes[ErrorTypeAttribute] = CrashType;

            foreach (var crashDir in crashDirs)
            {
                var crashDirFullPath = Path.Combine(nativeCrashesDir, crashDir);
                var crashFiles = Directory.GetFiles(crashDirFullPath);

                var alreadyUploaded = crashFiles.Any(n => n.EndsWith("backtrace.json"));
                if (alreadyUploaded)
                {
                    continue;
                }
                var minidumpPath = crashFiles.FirstOrDefault(n => n.EndsWith("crash.dmp"));
                if (string.IsNullOrEmpty(minidumpPath))
                {
                    continue;
                }
                if (requireBreadcrumbsCopy)
                {
                    try
                    {
                        File.Copy(breadcrumbPath, breadcrumbCopyPath);
                        attachments.Add(breadcrumbCopyPath);
                        copiedFile = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(string.Format("Cannot make a copy of the breadcrumb file in the database directory. Reason: {0}", e.Message));
                    }
                    finally
                    {
                        requireBreadcrumbsCopy = false;
                    }
                }
                var dumpAttachment = crashFiles.Concat(attachments).Where(n => n != minidumpPath).ToList();
                yield return backtraceApi.SendMinidump(minidumpPath, dumpAttachment, attributes, (BacktraceResult result) =>
                {
                    if (result != null && result.Status == BacktraceResultStatus.Ok)
                    {
                        File.Create(Path.Combine(crashDirFullPath, "backtrace.json"));
                    }
                });
            }
            if (copiedFile)
            {
                try
                {
                    File.Delete(breadcrumbCopyPath);
                }
                catch (Exception e)
                {
                    // The file will be cleaned on the library startup via database integration
                    // if native client for any reason won't be able to remove it.
                    Debug.LogWarning(string.Format("Cannot remove temporary breadcrumb file. Reason: {0}", e.Message));
                }
            }
        }
        /// <summary>
        /// Generate path to Crashpad handler binary
        /// </summary>
        /// <returns>Path to crashpad handler binary</returns>
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
        /// <summary>
        /// Clean scoped attributes
        /// </summary>
        internal static void CleanScopedAttributes()
        {
            // cleaning scoped attributes should be skipped when 
            // Configuration.SendUnhandledGameCrashesOnGameStartup  is set to false
            // the reason behind this decision is to make sure user change in the configuration
            // won't leave any useless data
            var attributesJson = PlayerPrefs.GetString(ScopedAttributeListKey);
            if (!HasScopedAttributesEmpty(attributesJson))
            {
                return;
            }
            var attributes = JsonUtility.FromJson<ScopedAttributesContainer>(attributesJson);
            foreach (var attributeKey in attributes.Keys)
            {
                PlayerPrefs.DeleteKey(string.Format(ScopedAttributesPattern, attributeKey));
            }
            PlayerPrefs.DeleteKey(ScopedAttributeListKey);
        }

        internal static IDictionary<string, string> GetScopedAttributes()
        {
            var attributesJson = PlayerPrefs.GetString(ScopedAttributeListKey);
            if (!HasScopedAttributesEmpty(attributesJson))
            {
                return new Dictionary<string, string>();
            }
            var result = new Dictionary<string, string>();
            var attributes = JsonUtility.FromJson<ScopedAttributesContainer>(attributesJson);
            foreach (var attributeKey in attributes.Keys)
            {
                var value = PlayerPrefs.GetString(string.Format(ScopedAttributesPattern, attributeKey), string.Empty);
                result[attributeKey] = value;
            }

            // extend scoped attributes with legacy attributes stored by Backtrace-Unity library in previous versions
            IDictionary<string, string> legacyAttributes = new Dictionary<string, string>()
                    {
                        { MachineUuidKey, "guid" },
                        { VersionKey, "application.version" },
                        { SessionKey, BacktraceMetrics.ApplicationSessionKey }
                    };

            foreach (var legacyAttribute in legacyAttributes)
            {
                string legacyAttributeValue = PlayerPrefs.GetString(legacyAttribute.Key, string.Empty);
                if (!string.IsNullOrEmpty(legacyAttributeValue))
                {
                    PlayerPrefs.DeleteKey(legacyAttribute.Key);
                    result[legacyAttribute.Value] = legacyAttributeValue;
                }
            }
            return result;
        }

        /// <summary>
        /// Adds attributes to scoped registry and to native clietn
        /// </summary>
        /// <param name="key">attribute key</param>
        /// <param name="value">attribute value</param>
        private void AddAttributes(string key, string value)
        {
            if (CaptureNativeCrashes)
            {
                AddNativeAttribute(key, value);
            }
            AddScopedAttribute(key, value);
        }

        /// <summary>
        /// Adds dictionary of attributes to player prefs for windows crashes captured by unity crash handler
        /// </summary>
        /// <param name="atributes">Attributes</param>
        internal void AddScopedAttributes(IDictionary<string, string> attributes)
        {
            if (!_configuration.SendUnhandledGameCrashesOnGameStartup)
            {
                return;
            }
            var attributesContainer = new ScopedAttributesContainer();
            foreach (var attribute in attributes)
            {
                attributesContainer.Keys.Add(attribute.Key);
                PlayerPrefs.SetString(string.Format(ScopedAttributesPattern, attribute.Key), attribute.Value);
            }
            PlayerPrefs.SetString(ScopedAttributeListKey, JsonUtility.ToJson(attributesContainer));
        }

        /// <summary>
        /// Adds attribute to player prefs for windows crashes captured by Unity crash handler
        /// </summary>
        /// <param name="key">Attribute key</param>
        /// <param name="value">Attribute value</param>
        private void AddScopedAttribute(string key, string value)
        {
            if (!_configuration.SendUnhandledGameCrashesOnGameStartup)
            {
                return;
            }
            var attributesJson = PlayerPrefs.GetString(ScopedAttributeListKey);
            var attributes = HasScopedAttributesEmpty(attributesJson)
                ? JsonUtility.FromJson<ScopedAttributesContainer>(attributesJson)
                : new ScopedAttributesContainer();

            attributes.Keys.Add(key);
            PlayerPrefs.SetString(ScopedAttributeListKey, JsonUtility.ToJson(attributes));
            PlayerPrefs.SetString(string.Format(ScopedAttributesPattern, key), value);
        }

        private static bool HasScopedAttributesEmpty(string attributesJson)
        {
            return !(string.IsNullOrEmpty(attributesJson) || attributesJson == "{}");
        }
    }
}

#endif