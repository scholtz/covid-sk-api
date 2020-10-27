using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    public static class Hash
    {
        public static string GetSHA256Hash(this byte[] data)
        {
            byte[] hashOut;
            using (SHA256 hasher = SHA256.Create())
            {
                hashOut = hasher.ComputeHash(data);
            }
            return BitConverter.ToString(hashOut).Replace("-", "").ToLower();
        }
    }
}
