using System.Security.Cryptography;
using System.Text;

namespace HRM.Helpers.Security
{
    public static class EncryptionHelper
    {
        // Nên đưa key vào appsettings.json hoặc biến môi trường.
        // Key AES-256 phải đủ 32 bytes.
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");

        public static byte[]? EncryptString(string? plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return null;

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var cipherBytes = encryptor.TransformFinalBlock(
                plainBytes,
                0,
                plainBytes.Length
            );

            // Lưu IV + CipherText chung một mảng byte
            var result = new byte[aes.IV.Length + cipherBytes.Length];

            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return result;
        }

        public static string? DecryptString(object? dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;

            var encryptedBytes = (byte[])dbValue;

            if (encryptedBytes.Length <= 16)
                return null;

            using var aes = Aes.Create();
            aes.Key = Key;

            var iv = new byte[16];
            var cipherBytes = new byte[encryptedBytes.Length - 16];

            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(encryptedBytes, 16, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            var plainBytes = decryptor.TransformFinalBlock(
                cipherBytes,
                0,
                cipherBytes.Length
            );

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}