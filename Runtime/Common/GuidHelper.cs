﻿using System;

namespace Backtrace.Unity.Extensions
{
    /// <summary>
    /// Extension for Guid class
    /// </summary>
    public static class GuidHelper
    {
        /// <summary>
        /// Convert long to Guid
        /// </summary>
        /// <returns>new Guid based on long</returns>
        public static Guid FromLong(long source)
        {
            byte[] guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(source), guidData, 8);
            return new Guid(guidData);
        }

        public static bool IsNullOrEmpty(string guid)
        {
            const string emptyGuid = "00000000-0000-0000-0000-000000000000";
            return string.IsNullOrEmpty(guid) || guid == emptyGuid;
        }
    }
}
