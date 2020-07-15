using System;
using System.IO;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Common
{
    internal static class DatabasePathHelper
    {
        /// <summary>
        /// Parse Backtrace database path string to full path
        /// </summary>
        /// <param name="databasePath">Backtrace database path</param>
        /// <returns>Full path to Backtrace database</returns>
        internal static string GetFullDatabasePath(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                return string.Empty;
            }

            return databasePath.ParseInterpolatedString().GetFullPath();
        }

        private static string ParseInterpolatedString(this string databasePath)
        {
            // check if string has any interpolated substring 
            var interpolationStart = databasePath.IndexOf("${");
            if (interpolationStart == -1)
            {
                return databasePath;
            }
            var interpolationEnd = databasePath.IndexOf('}', interpolationStart);
            if(interpolationEnd == -1)
            {
                return databasePath;
            }
            var interpolationValue = databasePath.Substring(interpolationStart, interpolationEnd - interpolationStart +1);
            if(string.IsNullOrEmpty(interpolationValue))
            {
                return databasePath;
            }

            switch (interpolationValue.ToLower())
            {
                case "${application.persistentdatapath}":
                    return databasePath.Replace(interpolationValue, Application.persistentDataPath);
                case "${application.datapath}":
                    return databasePath.Replace(interpolationValue, Application.dataPath);
                default:
                    return databasePath;
            }

        }

        private static string GetFullPath(this string databasePath)
        {
            if (!Path.IsPathRooted(databasePath))
            {
                databasePath = Path.Combine(Application.persistentDataPath, databasePath);
            }
            return Path.GetFullPath(databasePath);

        }
    }
}
