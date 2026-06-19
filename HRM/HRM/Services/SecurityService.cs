using System;
using HRM.Common;
using HRM.Helpers.Security;

namespace HRM.Services
{
    public class SecurityService : ISecurityService
    {
        public string GenerateSearchIndex(string rawData, string columnProfile)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return string.Empty;

            // Generate SHA-256 hash with salt
            byte[] hashBytes = SecurityIndexHelper.ComputeHashWithSalt(rawData);
            return Convert.ToBase64String(hashBytes);
        }

        public string EncryptData(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return string.Empty;

            // Encrypt using AES-256
            byte[]? encryptedBytes = EncryptionHelper.EncryptString(rawData);
            return encryptedBytes != null ? Convert.ToBase64String(encryptedBytes) : string.Empty;
        }

        public string DecryptData(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData))
                return string.Empty;

            // Convert Base64 string back to byte[] and decrypt
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
            return EncryptionHelper.DecryptString(encryptedBytes) ?? string.Empty;
        }
    }
}
