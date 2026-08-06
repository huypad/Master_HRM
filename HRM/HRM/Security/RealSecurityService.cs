using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HRM.Common;
using HRM.Helpers.Security;

namespace HRM.Security
{
   
    /// Triển khai dịch vụ bảo mật lai (Hybrid Security) cho HealthcareDB1.
    /// - Mã hóa/Giải mã: AES-256 (IV 16 byte ở đầu ciphertext, PKCS7).
    /// - Chỉ mục tra cứu chính xác: HMAC-SHA256 (Hex 64 ký tự).
    /// - Chỉ mục tra cứu mờ: MinHash LSH (16 Hash Functions, 4 Bands, 4 Rows).

    public class RealSecurityService : IHybridSecurityService
    {
        // Khóa bí mật 256-bit (32 bytes) cho AES và HMAC
        private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("HRM_MASTER_KEY_32BYTES_2026_LEADER_SEC!");
        private static readonly byte[] HMAC_KEY = Encoding.UTF8.GetBytes("HRM_HMAC_INDEX_KEY_32BYTES_2026_LEADER!");

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

        public string DecryptData(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            // Nếu dữ liệu đã là PlainText
            if (!cipherText.StartsWith("BKT_") && !IsBase64String(cipherText))
            {
                return cipherText;
            }

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                
                // Nếu độ dài nhỏ hơn 16 bytes IV, decode dạng UTF-8/ASCII Base64
                if (fullCipher.Length < 16)
                {
                    string utf8Str = Encoding.UTF8.GetString(fullCipher).Replace("\0", "").Trim();
                    if (!string.IsNullOrEmpty(utf8Str) && utf8Str.All(c => !char.IsControl(c) || c == ' ' || c == '\t'))
                    {
                        return utf8Str;
                    }
                    return cipherText;
                }

                // Tách IV (16 bytes đầu) và CipherText (phần còn lại)
                byte[] iv = new byte[16];
                byte[] cipher = new byte[fullCipher.Length - 16];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

                using var aes = Aes.Create();
                aes.Key = AES_KEY;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(decryptedBytes).Replace("\0", "").Trim();
            }
            catch
            {
                // Nếu chuỗi là Base64 của UTF-8/ASCII chưa mã hóa AES
                try
                {
                    byte[] rawBytes = Convert.FromBase64String(cipherText);
                    string rawStr = Encoding.UTF8.GetString(rawBytes).Replace("\0", "").Trim();
                    if (!string.IsNullOrEmpty(rawStr) && rawStr.All(c => !char.IsControl(c) || c == ' ' || c == '\t'))
                    {
                        return rawStr;
                    }
                }
                catch { }

                return cipherText;
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

    
        /// Tạo chỉ mục tìm kiếm mờ MinHash LSH (16 hash functions, split thành 4 bands x 4 rows).
        /// Trả về chuỗi đại diện cho LSH buckets.

        public string GenerateFuzzyIndex(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;

            // 1. Chuẩn hóa & tạo Bi-gram n-grams
            string normalized = SecurityIndexHelper.NormalizeForSearch(plainText);
            var ngrams = SecurityIndexHelper.BuildNgrams(normalized, 2);

            if (ngrams.Count == 0) return string.Empty;

            // 2. Tính MinHash Signature (16 giá trị int min)
            int[] minHashSig = new int[16];
            for (int i = 0; i < 16; i++)
            {
                int minVal = int.MaxValue;
                int a = (i + 1) * 3 + 7;
                int b = (i + 1) * 5 + 11;

                foreach (var gram in ngrams)
                {
                    int h = Math.Abs((gram.GetHashCode() * a + b) % 2147483647);
                    if (h < minVal) minVal = h;
                }
                minHashSig[i] = minVal;
            }

            // 3. Chia 16 hash thành 4 Bands, mỗi Band 4 rows -> LSH Bucket Keys
            var buckets = new string[4];
            for (int band = 0; band < 4; band++)
            {
                int h1 = minHashSig[band * 4];
                int h2 = minHashSig[band * 4 + 1];
                int h3 = minHashSig[band * 4 + 2];
                int h4 = minHashSig[band * 4 + 3];

                string bandStr = $"{h1}_{h2}_{h3}_{h4}";
                using var md5 = MD5.Create();
                byte[] bHash = md5.ComputeHash(Encoding.UTF8.GetBytes(bandStr));
                buckets[band] = Convert.ToHexString(bHash).Substring(0, 8); // 8 char Bucket ID
            }

            return string.Join(";", buckets);
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
