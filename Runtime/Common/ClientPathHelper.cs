using System;
using System.IO;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Common
{
    internal static class ClientPathHelper
    {
        /// <summary>
        /// Parse Backtrace client path string to full path
        /// </summary>
        /// <param name="path">Backtrace path with interpolated string and other Backtrace's improvements</param>
        /// <returns>Full path to Backtrace file/directory</returns>
        internal static string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.ParseInterpolatedString().GenerateFullPath();
        }

        private static string ParseInterpolatedString(this string path)
        {
            // check if string has any interpolated substring 
            var interpolationStart = path.IndexOf("${");
            if (interpolationStart == -1)
            {
                return path;
            }
            var interpolationEnd = path.IndexOf('}', interpolationStart);
            if (interpolationEnd == -1)
            {
                return path;
            }
            var interpolationValue = path.Substring(interpolationStart, interpolationEnd - interpolationStart + 1);
            if (string.IsNullOrEmpty(interpolationValue))
            {
                return path;
            }

            switch (interpolationValue.ToLower())
            {
                case "${application.persistentdatapath}":
                    return path.Replace(interpolationValue, Application.persistentDataPath);
                case "${application.datapath}":
                    return path.Replace(interpolationValue, Application.dataPath);
                default:
                    return path;
            }

        }

        private static string GenerateFullPath(this string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Application.persistentDataPath, path);
            }
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return string.Empty;
            }

        }
        
        internal static bool IsFileInDatabaseDirectory(string databasePath, string filePath)
        {
            // If databasePath does not have a trailing slash, it is already a directory.
            if (!databasePath.EndsWith("/"))
            {
                return new DirectoryInfo(databasePath).FullName == new DirectoryInfo(Path.GetDirectoryName(filePath)).FullName;
            }
            // Handles case when users put a trailing slash in their database path
            return Path.GetDirectoryName(databasePath) == Path.GetDirectoryName(filePath);
        }
    }
}
