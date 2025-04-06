using System;
using System.Security.Cryptography;
using System.Text;

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

        /// <summary>
        /// Converts a random string into a guid representation.
        /// </summary>
        public static Guid FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Guid.Empty;
            }
            // to make sure we're supporting old version of Unity that can use .NET 3.5 
            // we're using an older API to generate a GUID.
            MD5 md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}
