using System;
using System.Security.Cryptography;
using System.Text;
using HRM.Common;
using HRM.Helpers.Security;

namespace HRM.Security
{
    
    /// Triển khai dịch vụ bảo mật V2 (AES-256, HMAC-SHA256 Exact Index và BitGram 16-bit Bucket Index).
    
    public class RealSecurityService : IHybridSecurityService
    {
        // Secret Key dùng chung cho các thuật toán băm HMAC-SHA256
        private static readonly byte[] HmacKey = Encoding.UTF8.GetBytes("HRM_HMAC_BenchmarkKey_2026_V2");

        
        /// Mã hóa dữ liệu bằng thuật toán AES-256 (Tái sử dụng EncryptionHelper).
       
        /// <param name="rawText">Dữ liệu cần mã hóa.</param>
        /// <returns>Chuỗi mã hóa dạng Base64.</returns>
        public string EncryptData(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            var bytes = EncryptionHelper.EncryptString(rawText);
            return bytes != null ? Convert.ToBase64String(bytes) : string.Empty;
        }

        
        /// Giải mã dữ liệu mã hóa AES-256.
       
        /// <param name="cipherText">Chuỗi mã hóa dạng Base64.</param>
        /// <returns>Dữ liệu giải mã gốc (Plaintext).</returns>
        public string DecryptData(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                return EncryptionHelper.DecryptString(bytes) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        
        /// Tạo Exact Index cho CCCD, SĐT hoặc Số Tài Khoản bằng HMAC-SHA256.
        
        /// <param name="rawText">Từ khóa tra cứu chính xác.</param>
        /// <returns>Chuỗi HMAC-SHA256 dạng Base64 (44 ký tự).</returns>
        public string GenerateExactIndex(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            var normalized = SecurityIndexHelper.NormalizeForSearch(rawText);
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            using var hmac = new HMACSHA256(HmacKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToBase64String(hash);
        }

       
        /// Tạo BitGram Bucket ID ("BG_XXXX") cho một trigram Họ Tên bằng HMAC-SHA256 (lấy 16-bit hash đầu).
        
        /// <param name="rawText">Trigram đã normalize (3 ký tự).</param>
        /// <returns>Chuỗi Bucket ID dạng "BG_XXXX".</returns>
        public string GenerateFuzzyIndex(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            var normalized = SecurityIndexHelper.NormalizeForSearch(rawText);
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            using var hmac = new HMACSHA256(HmacKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));

            var bucket = (hash[0] << 8) | hash[1];
            return $"BG_{bucket:X4}";
        }
    }
}
