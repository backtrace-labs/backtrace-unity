using Backtrace.Unity.Types;
using System;
using System.IO;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    [Serializable]
    public class BacktraceConfiguration : ScriptableObject
    {
        /// <summary>
        /// Backtrace server url
        /// </summary>
        public string ServerUrl;

        /// <summary>
        /// Backtrace server API token
        /// </summary>
        public string Token;

        /// <summary>
        /// Maximum number reports per minute
        /// </summary>
        public int ReportPerMin;

        /// <summary>
        /// Determine if client should catch unhandled exceptions
        /// </summary>
        public bool HandleUnhandledExceptions = true;

        /// <summary>
        /// Directory path where reports and minidumps are stored
        /// </summary>
        public string DatabasePath;

        /// <summary>
        /// Determine if database is enable
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Resend report when http client throw exception
        /// </summary>
        public bool AutoSendMode = true;

        /// <summary>
        /// Determine if BacktraceDatabase should try to create database directory on application start
        /// </summary>
        public bool CreateDatabase = false;

        /// <summary>
        /// Maximum number of stored reports in Database. If value is equal to zero, then limit not exists
        /// </summary>
        public int MaxRecordCount;

        /// <summary>
        /// Database size in MB
        /// </summary>
        public long MaxDatabaseSize;
        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        public int RetryInterval = 60;

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        public int RetryLimit = 3;

        /// <summary>
        /// Retry order
        /// </summary>
        public RetryOrder RetryOrder;

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
            if (!value.Contains(".sp.backtrace.io"))
            {
                value += ".sp.backtrace.io";
                Debug.Log("After change server URL: " + value);
            }
            if (!value.StartsWith("http"))
            {
                value = $"https://{value}";
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
            return ValidateServerUrl(ServerUrl) && ValidateToken(Token);
        }

        public static bool ValidateToken(string value)
        {
            return !(string.IsNullOrEmpty(value) || value.Length != 64);
        }

        public BacktraceCredentials ToCredentials()
        {
            return new BacktraceCredentials(ServerUrl, Token);
        }

        public static bool ValidateDatabasePath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string databasePathCopy = value;
            if (!Path.IsPathRooted(databasePathCopy))
            {
                databasePathCopy = Path.GetFullPath(Path.Combine(Application.dataPath, databasePathCopy));
            }
            return Directory.Exists(databasePathCopy);
        }
    }
}