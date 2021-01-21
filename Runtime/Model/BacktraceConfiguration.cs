﻿using Backtrace.Unity.Common;
using Backtrace.Unity.Types;
using System;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    [Serializable]
    [CreateAssetMenu(fileName = "Backtrace Configuration", menuName = "Backtrace/Configuration", order = 0)]
    public class BacktraceConfiguration : ScriptableObject
    {
        /// <summary>
        /// Backtrace server url
        /// </summary>
        [Header("Backtrace client configuration")]
        [Tooltip("This field is required to submit exceptions from your Unity project to your Backtrace instance.\n \nMore information about how to retrieve this value for your instance is our docs at What is a submission URL and What is a submission token?\n\nNOTE: the backtrace-unity plugin will expect full URL with token to your Backtrace instance.")]
        public string ServerUrl;

        /// <summary>
        /// Backtrace server API token
        /// </summary>
        public string Token;

        /// <summary>
        /// Maximum number reports per minute
        /// </summary>
        [Tooltip("Reports per minute: Limits the number of reports the client will send per minutes. If set to 0, there is no limit. If set to a higher value and the value is reached, the client will not send any reports until the next minute. Default: 50")]
        public int ReportPerMin = 50;

        /// <summary>
        /// Determine if client should catch unhandled exceptions
        /// </summary>
        [Tooltip("Toggle this on or off to set the library to handle unhandled exceptions that are not captured by try-catch blocks.")]
        public bool HandleUnhandledExceptions = true;

        /// <summary>
        /// Determine if client should ignore ssl validation
        /// </summary>
        [Tooltip("Unity by default will validate ssl certificates. By using this option you can avoid ssl certificates validation. However, if you don't need to ignore ssl validation, please set this option to false.")]
        public bool IgnoreSslValidation = false;

        /// <summary>
        /// Destroy Backtrace instances on new scene load.
        /// </summary>
        [Tooltip("Backtrace-client by default will be available on each scene. Once you initialize Backtrace integration, you can fetch Backtrace game object from every scene. In case if you don't want to have Backtrace-unity integration available by default in each scene, please set this value to true.")]
        public bool DestroyOnLoad = false;

        /// <summary>
        /// Sampling configuration - fractional sampling allows to drop some % of unhandled exception.
        /// </summary>
        [Tooltip("Log random sampling rate - Enables a random sampling mechanism for unhandled exceptions - by default sampling is equal to 0.01 - which means only 1% of randomply sampling reports will be send to Backtrace. \n" +
            "* 1 - means 100% of unhandled exception reports will be reported by library,\n" +
            "* 0.1 - means 10% of unhandled exception reports will be reported by library,\n" +
            "* 0 - means library is going to drop all unhandled exception.")]
        [Range(0, 1)]
        public double Sampling = 0.01d;

        /// <summary>
        /// Backtrace report filter type
        /// </summary>
        [Tooltip("Report filter allows to filter specific type of reports. Possible options:\n" +
            "* Disable - Disable report filtering - send every type of report.\n" +
            "* Message - Prevent message reports.\n" +
            "* Exception - Prevent exception reports.\n" +
            "* Unhandled exception- Prevent unhandled exception reports.\n" +
            "* Hang - Prevent sending reports when game hang.")]

        public ReportFilterType ReportFilterType = ReportFilterType.None;
        /// <summary>
        /// Game object depth in Backtrace report
        /// </summary>
        [Tooltip("Allows developer to filter number of game object childrens in Backtrace report.")]
        public int GameObjectDepth = -1;

        /// <summary>
        /// Number of logs collected by Backtrace-Unity
        /// </summary>
        [Tooltip("Number of logs collected by Backtrace-Unity")]
        public uint NumberOfLogs = 10;

        /// <summary>
        /// Flag that allows to include performance statistics in Backtrace report
        /// </summary>
        [Tooltip("Enable performance statistics")]
        public bool PerformanceStatistics = false;
        /// <summary>
        /// Try to find game native crashes and send them on Game startup
        /// </summary>
        [Tooltip("Try to find game native crashes and send them on Game startup")]
        public bool SendUnhandledGameCrashesOnGameStartup = true;

#if UNITY_ANDROID || UNITY_IOS
#if UNITY_ANDROID
        /// <summary>
        /// Capture native NDK Crashes.
        /// </summary>
        [Tooltip("Capture native NDK Crashes (ANDROID API 21+)")]
#elif UNITY_IOS
        /// <summary>
        /// Capture native iOS Crashes.
        /// </summary>
        [Tooltip("Capture native Crashes")]
#endif

        public bool CaptureNativeCrashes = true;
#endif

#if UNITY_ANDROID  || UNITY_IOS
        /// <summary>
        /// Handle ANR events - Application not responding
        /// </summary>
        [Tooltip("Handle ANR events - Application not responding")]
        public bool HandleANR = true;

#if UNITY_2019_2_OR_NEWER
        /// <summary>
        /// Symbols upload token
        /// </summary>
        [Tooltip("Symbols upload token required to upload symbols to Backtrace")]
        public string SymbolsUploadToken = string.Empty;
#endif
#endif

        /// <summary>
        /// Backtrace client deduplication strategy. 
        /// </summary>
        [Tooltip("Client-side deduplication allows the backtrace-unity library to group multiple error reports into a single one based on various factors. Factors include:\n\n" +
            "* Disable - Client-side deduplication rules are disabled.\n" +
            "* Everything - Use all the options as a factor in client-side deduplication.\n" +
            "* Faulting callstack - Use the faulting callstack as a factor in client-side deduplication.\n" +
            "* Exception type - Use the exception type as a factor in client-side deduplication.\n" +
            "* Exception message - Use the exception message as a factor in client-side deduplication.")]

        public DeduplicationStrategy DeduplicationStrategy = DeduplicationStrategy.None;


        /// <summary>
        /// Use normalized exception message instead environment stack trace, when exception doesn't have stack trace
        /// </summary>
        [Tooltip("If exception does not have a stack trace, use a normalized exception message to generate fingerprint.")]
        public bool UseNormalizedExceptionMessage = false;

        /// <summary>
        /// Determine minidump type support - minidump generation is supported on Windows.
        /// </summary>
        [Tooltip("Type of minidump that will be attached to Backtrace report in the report generated on Windows machine.")]
        public MiniDumpType MinidumpType = MiniDumpType.None;

        /// <summary>
        /// Generate game screen shot when exception happen
        /// </summary>
        [Tooltip("Generate and attach screenshot of frame as exception occurs")]
        public bool GenerateScreenshotOnException = false;

        /// <summary>
        /// Directory path where reports and minidumps are stored
        /// </summary>
        [Tooltip("This is the path to directory where the Backtrace database will store reports on your game. NOTE: Backtrace database will remove all existing files on database start.")]
        public string DatabasePath;

        /// <summary>
        /// Determine if database is enable
        /// </summary>
        [Header("Backtrace database configuration")]
        [Tooltip("When this setting is toggled, the backtrace-unity plugin will configure an offline database that will store reports if they can't be submitted do to being offline or not finding a network. When toggled on, there are a number of Database settings to configure.")]
        public bool Enabled;

        /// <summary>
        /// Add Unity log file to Backtrace report
        /// </summary>
        [Tooltip("Add Unity player log file to Backtrace report")]
        public bool AddUnityLogToReport = false;

        /// <summary>
        /// Resend report when http client throw exception
        /// </summary>
        [Tooltip("When toggled on, the database will send automatically reports to Backtrace server based on the Retry Settings below. When toggled off, the developer will need to use the Flush method to attempt to send and clear. Recommend that this is toggled on.")]
        public bool AutoSendMode = true;

        /// <summary>
        /// Determine if BacktraceDatabase should try to create database directory on application start
        /// </summary>
        [Tooltip("If toggled, the library will create the offline database directory if the provided path doesn't exists.")]
        public bool CreateDatabase = false;

        /// <summary>
        /// Maximum number of stored reports in Database. If value is equal to zero, then limit not exists
        /// </summary>
        [Tooltip("This is one of two limits you can impose for controlling the growth of the offline store. This setting is the maximum number of stored reports in database. If value is equal to zero, then limit not exists, When the limit is reached, the database will remove the oldest entries.")]
        public int MaxRecordCount = 8;

        /// <summary>
        /// Database size in MB
        /// </summary>
        [Tooltip("This is the second limit you can impose for controlling the growth of the offline store. This setting is the maximum database size in MB. If value is equal to zero, then size is unlimited, When the limit is reached, the database will remove the oldest entries.")]
        public long MaxDatabaseSize;
        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        [Tooltip("If the database is unable to send its record, this setting specifies how many seconds the library should wait between retries.")]
        public int RetryInterval = 60;

        /// <summary>
        /// Maximum number of retries
        [Tooltip("If the database is unable to send its record, this setting specifies the maximum number of retries before the system gives up")]
        public int RetryLimit = 3;

        /// <summary>
        /// Retry order
        /// </summary>
        [Tooltip("This specifies in which order records are sent to the Backtrace server.")]
        public RetryOrder RetryOrder;

        public string GetFullDatabasePath()
        {
            return DatabasePathHelper.GetFullDatabasePath(DatabasePath);
        }
        public string CrashpadDatabasePath
        {
            get
            {
                if (!Enabled)
                {
                    return string.Empty;
                }
                return Path.Combine(GetFullDatabasePath(), "crashpad");
            }
        }

        public string GetValidServerUrl()
        {
            return UpdateServerUrl(ServerUrl);
        }

        public static string UpdateServerUrl(string value)
        {
            //in case if user pass invalid string, copy value contain uri without method modifications
            var copy = value;

            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!value.StartsWith("http"))
            {
                value = string.Format("https://{0}", value);
            }
            string uriScheme = value.StartsWith("https://")
                ? Uri.UriSchemeHttps
                : Uri.UriSchemeHttp;

            if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
            {
                return copy;
            }
            return new UriBuilder(value) { Scheme = uriScheme }.Uri.ToString();
        }

        public static bool ValidateServerUrl(string value)
        {
            return Uri.IsWellFormedUriString(UpdateServerUrl(value), UriKind.Absolute);
        }

        public bool IsValid()
        {
            return ValidateServerUrl(ServerUrl);
        }


        public BacktraceCredentials ToCredentials()
        {
            return new BacktraceCredentials(ServerUrl);
        }
    }
}