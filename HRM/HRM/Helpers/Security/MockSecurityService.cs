using System.Security.Cryptography;
using System.Text;

namespace HRM.Helpers.Security
{
    public class MockSecurityService : ISecurityService
    {
        private const string Salt = "AppFixedSalt_2026";
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");

        public string GenerateSearchIndex(string rawData, string columnProfile)
        {
            rawData ??= string.Empty;

            var normalized = columnProfile.Equals("HoTenGram", StringComparison.OrdinalIgnoreCase)
                ? rawData
                : rawData.Trim().ToLowerInvariant();

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + normalized));
            return Convert.ToBase64String(bytes);
        }

        public string EncryptData(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(rawData);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string DecryptData(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData))
                return string.Empty;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedData);

                if (encryptedBytes.Length <= 16)
                    return string.Empty;

                using var aes = Aes.Create();
                aes.Key = Key;

                var iv = new byte[16];
                var cipherBytes = new byte[encryptedBytes.Length - 16];

                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);
                Buffer.BlockCopy(encryptedBytes, 16, cipherBytes, 0, cipherBytes.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
               
                return encryptedData;
            }
        }
    }
}
