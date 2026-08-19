using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HRM.Security
{
   
    /// Triển khai dịch vụ bảo mật lai (Hybrid Security) cho HealthcareDB1.
    /// - Mã hóa/Giải mã: AES-256 (IV 16 byte ở đầu ciphertext, PKCS7).
    /// - Chỉ mục tra cứu chính xác: HMAC-SHA256 (Hex 64 ký tự).
    /// - Chỉ mục tra cứu mờ: MinHash LSH Tri-gram (5 Hash Functions, seeds: 13,27,31,47,59).

    public class RealSecurityService : IHybridSecurityService
    {
        // Khóa bí mật 256-bit (32 bytes) cho AES và HMAC
        // .Take(32): đảm bảo đúng 32 bytes — AES-256 yêu cầu key length 16/24/32
        private static readonly byte[] AES_KEY  = Encoding.UTF8.GetBytes("HRM_MASTER_KEY_32BYTES_2026_LEADER_SEC!").Take(32).ToArray();
        private static readonly byte[] HMAC_KEY = Encoding.UTF8.GetBytes("HRM_HMAC_INDEX_KEY_32BYTES_2026_LEADER!").Take(32).ToArray();

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

        private static readonly System.Threading.ThreadLocal<Aes> _aesLocal = new System.Threading.ThreadLocal<Aes>(() => {
            var aes = Aes.Create();
            aes.Key = AES_KEY;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        });

        public string DecryptData(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            if (!cipherText.StartsWith("BKT_") && !IsBase64String(cipherText))
            {
                return cipherText;
            }

            byte[] fullCipher = null;
            try
            {
                fullCipher = Convert.FromBase64String(cipherText);
            }
            catch
            {
                return cipherText;
            }

            // 1. Kiểm tra xem đây có phải là Dữ liệu mẫu (Seed Data) dạng chuỗi thuần hay không
            // Tránh việc ném Exception trong AES decryption (gây nghẽn CPU khi chạy debug)
            try
            {
                // Thử decode UTF-8
                string rawStr = Encoding.UTF8.GetString(fullCipher).Replace("\0", "").Trim();
                if (!string.IsNullOrEmpty(rawStr) && !rawStr.Contains('\uFFFD') && rawStr.All(c => !char.IsControl(c) || c == ' ' || c == '\t'))
                {
                    return rawStr;
                }
                
                // Thử decode UTF-16 (Dữ liệu N'String' trong SQL Server)
                string utf16Str = Encoding.Unicode.GetString(fullCipher).Replace("\0", "").Trim();
                if (!string.IsNullOrEmpty(utf16Str) && !utf16Str.Contains('\uFFFD') && utf16Str.All(c => !char.IsControl(c) || c == ' ' || c == '\t'))
                {
                    return utf16Str;
                }
            }
            catch { }

            // 2. Nếu không phải là Seed Data hợp lệ, thì đây đích thị là dữ liệu đã được mã hóa AES (Cipher text)
            try
            {
                if (fullCipher.Length < 16) return cipherText;

                byte[] iv = new byte[16];
                byte[] cipher = new byte[fullCipher.Length - 16];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

                var aes = _aesLocal.Value;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(decryptedBytes).Replace("\0", "").Trim();
            }
            catch
            {
                return cipherText; // Fallback
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

    
        /// LSH + TRI-GRAM: Tạo chỉ mục tìm kiếm mờ (Fuzzy Index) theo thuật toán Leader.
        /// Tri-gram (n=3) + 5 MinHash functions (seeds: 13,27,31,47,59).
        /// Output: "BKT_V2_h0_h1_h2_h3_h4" — 5 MinHash values dùng làm bucket keys.

        public string GenerateFuzzyIndex(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;

            // Bước 1: Chuẩn hóa — viết thường và xóa khoảng trắng
            string normalized = rawText.ToLower().Replace(" ", "");

            // CẢI TIẾN 1: Đổi từ Bi-gram (n=2) sang Tri-gram (n=3)
            // Nếu chuỗi ngắn hơn 3 ký tự (ví dụ: tên "An" -> "an"), giữ nguyên chuỗi
            var nGrams = new HashSet<string>();
            if (normalized.Length < 3)
            {
                nGrams.Add(normalized);
            }
            else
            {
                for (int i = 0; i < normalized.Length - 2; i++)
                {
                    nGrams.Add(normalized.Substring(i, 3));
                }
            }

            // CẢI TIẾN 2: Tăng số lượng hàm băm MinHash từ 16 lên 5 (seeds mới)
            // Sử dụng XOR thay vì công thức tuyến tính để giảm collision
            int[] seeds = { 13, 27, 31, 47, 59 };
            var minHashes = new int[seeds.Length];

            for (int i = 0; i < seeds.Length; i++)
            {
                int minHash = int.MaxValue;

                foreach (var gram in nGrams)
                {
                    int hash = HRM.Common.SecurityIndexHelper.GetDeterministicHashCode(gram) ^ seeds[i];

                    if (hash < minHash)
                    {
                        minHash = hash;
                    }
                }

                minHashes[i] = minHash;
            }

            return "BKT_V2_" + string.Join("_", minHashes);
        }

        #endregion

        #region Private Helpers

        private static bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length % 4 != 0) return false;
            return Convert.TryFromBase64String(s, new Span<byte>(new byte[s.Length]), out _);
        }

        #endregion
    }
}
