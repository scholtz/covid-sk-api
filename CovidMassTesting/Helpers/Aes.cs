using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// https://gist.github.com/magicsih/be06c2f60288b54d9f52856feb96ce8c
    /// </summary>
    public class Aes : IDisposable
    {
        private readonly RijndaelManaged rijndael = new RijndaelManaged();
        private readonly System.Text.UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
        private bool disposed;

        private const int CHUNK_SIZE = 256;

        private void InitializeRijndael()
        {
            rijndael.Mode = CipherMode.CBC;
            rijndael.Padding = PaddingMode.PKCS7;
        }

        public Aes()
        {
            InitializeRijndael();

            rijndael.KeySize = CHUNK_SIZE;
            rijndael.BlockSize = CHUNK_SIZE;

            rijndael.GenerateKey();
            rijndael.GenerateIV();
        }

        public Aes(String base64key, String base64iv)
        {
            if (string.IsNullOrEmpty(base64key))
            {
                throw new ArgumentException($"'{nameof(base64key)}' cannot be null or empty", nameof(base64key));
            }

            if (string.IsNullOrEmpty(base64iv))
            {
                throw new ArgumentException($"'{nameof(base64iv)}' cannot be null or empty", nameof(base64iv));
            }

            InitializeRijndael();

            rijndael.Key = Convert.FromBase64String(base64key);
            rijndael.IV = Convert.FromBase64String(base64iv);
        }

        public Aes(byte[] key, byte[] iv)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            InitializeRijndael();

            rijndael.Key = key;
            rijndael.IV = iv;
        }

        public string Decrypt(byte[] cipher)
        {
            if (cipher is null)
            {
                throw new ArgumentNullException(nameof(cipher));
            }

            using ICryptoTransform transform = rijndael.CreateDecryptor();
            byte[] decryptedValue = transform.TransformFinalBlock(cipher, 0, cipher.Length);
            return unicodeEncoding.GetString(decryptedValue);
        }

        public string DecryptFromBase64String(string base64cipher)
        {
            if (string.IsNullOrEmpty(base64cipher))
            {
                throw new ArgumentException($"'{nameof(base64cipher)}' cannot be null or empty", nameof(base64cipher));
            }

            return Decrypt(Convert.FromBase64String(base64cipher));
        }

        public byte[] EncryptToByte(string plain)
        {
            if (string.IsNullOrEmpty(plain))
            {
                throw new ArgumentException($"'{nameof(plain)}' cannot be null or empty", nameof(plain));
            }

            using ICryptoTransform encryptor = rijndael.CreateEncryptor();
            byte[] cipher = unicodeEncoding.GetBytes(plain);
            byte[] encryptedValue = encryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return encryptedValue;
        }

        public string EncryptToBase64String(string plain)
        {
            if (string.IsNullOrEmpty(plain))
            {
                throw new ArgumentException($"'{nameof(plain)}' cannot be null or empty", nameof(plain));
            }

            return Convert.ToBase64String(EncryptToByte(plain));
        }

        public string GetKey()
        {
            return Convert.ToBase64String(rijndael.Key);
        }

        public string GetIV()
        {
            return Convert.ToBase64String(rijndael.IV);
        }

        public override string ToString()
        {
            return "KEY:" + GetKey() + Environment.NewLine + "IV:" + GetIV();
        }
        #region IDispose
        /// <summary>
        /// For IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            rijndael?.Dispose();

            disposed = true;
        }
        #endregion
    }
}
