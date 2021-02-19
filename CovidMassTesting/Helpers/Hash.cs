using System;
using System.Globalization;
using System.Security.Cryptography;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// Hash helper
    /// </summary>
    public static class Hash
    {
        /// <summary>
        /// Allows to make hash of any string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetSHA256Hash(this byte[] data)
        {
            byte[] hashOut;
            using (var hasher = SHA256.Create())
            {
                hashOut = hasher.ComputeHash(data);
            }
#pragma warning disable CA1308 // Normalize strings to uppercase
            return BitConverter
                .ToString(hashOut)
                .Replace("-", "", true, CultureInfo.InvariantCulture)
                .ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        }
        /// <summary>
        /// Allows to make hash of any string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetSHA256Hash(this string data)
        {
            return GetSHA256Hash(System.Text.Encoding.UTF8.GetBytes(data));
        }
    }
}
