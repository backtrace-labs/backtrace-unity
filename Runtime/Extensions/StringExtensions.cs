using System.Linq;
using System.Security.Cryptography;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Extensions
{
    internal static class StringHelper
    {
        /// <summary>
        /// Remove all characters from string that aren't letter.
        /// </summary>
        internal static string OnlyLetters(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            return new string(source.Where(n => char.IsLetter(n)).ToArray());
        }

        /// <summary>
        /// Create sha256 from string builder value
        /// </summary>
        /// <param name="value">string value</param>
        /// <returns>sha256 string</returns>
        internal static string GetSha(this StringBuilder source)
        {
            if (source == null)
            {
                return string.Empty;
            }
            return GetSha(source.ToString());
        }

        /// <summary>
        /// Create sha256 from string value
        /// </summary>
        /// <param name="value">string value</param>
        /// <returns>sha256 string</returns>
        internal static string GetSha(this string source)
        {
            // generate empty sha which represents fingerprint in Backtrace 
            // for empty string
            if (string.IsNullOrEmpty(source))
            {
                return "0000000000000000000000000000000000000000000000000000000000000000";
            }
            using (var sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(Encoding.ASCII.GetBytes(source));
                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
