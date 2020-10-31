using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

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
            using (SHA256 hasher = SHA256.Create())
            {
                hashOut = hasher.ComputeHash(data);
            }
            return BitConverter
                .ToString(hashOut)
                .Replace("-", "", true, CultureInfo.InvariantCulture)
                .ToLowerInvariant();
        }
    }
}
