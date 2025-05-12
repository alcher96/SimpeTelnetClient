using System;
using System.Security.Cryptography;
using System.Text;

namespace TelnetClient
{
    public static class CryptoHelper
    {
        private static readonly byte[] entropy = Encoding.UTF8.GetBytes("TelnetClientEntropy");

        public static string Encrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes = ProtectedData.Protect(dataBytes, entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                return null;
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}