using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HRM.Security
{
   
    /// Triển khai dịch vụ bảo mật lai (Hybrid Security) cho HealthcareDB1.
    /// - Chỉ mục tra cứu mờ: MinHash LSH với cấu hình n-gram và seed hiện hành.

    public class RealSecurityService : IHybridSecurityService
    {

        private readonly byte[] AES_KEY;
        private readonly byte[] HMAC_KEY;

        public RealSecurityService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            string aesKeyStr = configuration["Security:AesKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình 'Security:AesKey' (User Secrets/Env Var).");
            string hmacKeyStr = configuration["Security:HmacKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình 'Security:HmacKey' (User Secrets/Env Var).");

            AES_KEY = ReadExactly32ByteKey(aesKeyStr, "Security:AesKey");
            HMAC_KEY = ReadExactly32ByteKey(hmacKeyStr, "Security:HmacKey");

            _aesLocal = new System.Threading.ThreadLocal<Aes>(() => {
                var aes = Aes.Create();
                aes.Key = AES_KEY;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                return aes;
            });
        }

        #region IHybridSecurityService Implementation


        /// Mã hóa dữ liệu bằng AES-256 (IV 16 byte ngẫu nhiên được nối vào đầu cipher).
        /// Output: Chuỗi Base64 đại diện cho [IV (16B) + CipherText (NB)].

        public string EncryptData(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = AES_KEY;
            aes.GenerateIV(); // Tạo IV ngẫu nhiên 16 bytes
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Nối IV (16 bytes) + CipherBytes
            byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }


        /// Giải mã dữ liệu AES-256 từ chuỗi Base64 chứa [IV (16B) + CipherText (NB)].

        private readonly System.Threading.ThreadLocal<Aes> _aesLocal;

        public string DecryptData(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            byte[] fullCipher;
            try
            {
                fullCipher = Convert.FromBase64String(cipherText);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Ciphertext Bệnh nhân không đúng định dạng Base64.", ex);
            }

            if (fullCipher.Length <= 16 || (fullCipher.Length - 16) % 16 != 0)
            {
                throw new InvalidOperationException(
                    "Ciphertext Bệnh nhân không đúng định dạng AES-CBC.");
            }

            try
            {
                byte[] iv = new byte[16];
                byte[] cipher = new byte[fullCipher.Length - 16];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

                var aes = _aesLocal.Value
                    ?? throw new InvalidOperationException("Không thể khởi tạo AES cho HealthcareDB1.");
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(decryptedBytes).Replace("\0", "").Trim();
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Giải mã dữ liệu Bệnh nhân thất bại. Kiểm tra Security:AesKey và dữ liệu trong Patient_Secure.", ex);
            }
        }

    
        /// Tạo chỉ mục tìm kiếm chính xác HMAC-SHA256 (64 ký tự Hex) cho CCCD/Phone/BankAccount.
   
        public string GenerateExactIndex(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;

            string normalized = plainText.Trim();
            using var hmac = new HMACSHA256(HMAC_KEY);
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            
            return Convert.ToHexString(hashBytes); // 64 ky tu Hex in hoa
        }

        // Cấu hình benchmark đã chốt: Tri-gram + 3 MinHash seeds.
        public const int FUZZY_NGRAM_SIZE = 3;

        public static readonly int[] FUZZY_HASH_SEEDS = { 13, 27, 31 };

        private static int GetSeededHash(string gram, int seed)
        {
            string input = $"{seed}:{gram}";
            byte[] bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt32(bytes, 0);
        }

        public string GenerateFuzzyIndex(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;

            // Chuẩn hóa — viết thường và xóa khoảng trắng
            string normalized = rawText.ToLower().Replace(" ", "");
            if (string.IsNullOrEmpty(normalized)) return string.Empty;

            int n = FUZZY_NGRAM_SIZE;
            var nGrams = new HashSet<string>();
            if (normalized.Length < n)
            {
                nGrams.Add(normalized);
            }
            else
            {
                for (int i = 0; i <= normalized.Length - n; i++)
                {
                    nGrams.Add(normalized.Substring(i, n));
                }
            }

            if (nGrams.Count == 0) return string.Empty;

            var minHashes = new int[FUZZY_HASH_SEEDS.Length];
            for (int i = 0; i < FUZZY_HASH_SEEDS.Length; i++)
            {
                int minHash = int.MaxValue;
                foreach (var gram in nGrams)
                {
                    int hash = GetSeededHash(gram, FUZZY_HASH_SEEDS[i]);
                    if (hash < minHash) minHash = hash;
                }
                minHashes[i] = minHash;
            }

            return "BKT_V2_" + string.Join("_", minHashes);
        }

        #endregion

        #region Private Helpers

        private static byte[] ReadExactly32ByteKey(string keyValue, string configurationName)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(keyValue);
            if (keyBytes.Length != 32)
            {
                throw new InvalidOperationException(
                    $"{configurationName} phải có đúng 32 byte UTF-8 cho AES-256/HMAC-SHA256.");
            }

            return keyBytes;
        }

        #endregion
    }
}
