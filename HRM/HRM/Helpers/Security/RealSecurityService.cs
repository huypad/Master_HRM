using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HRM.Helpers.Security
{
    public interface IRealSecurityService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherTextBase64);
        string ComputeHmac(string input);
        string NormalizeName(string input);
        List<string> BuildBigrams(string name);
        string ComputeBitgramBucket(string bigram);
    }

    public class RealSecurityService : IRealSecurityService
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes AES Key
        private readonly string _hmacKey;

        public RealSecurityService(IConfiguration configuration)
        {
            _hmacKey = configuration["Healthcare:HmacKey"] ?? "HealthcareBenchmarkHmacKey2026!!";
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherTextBase64)
        {
            if (string.IsNullOrWhiteSpace(cipherTextBase64))
                return string.Empty;

            try
            {
                var encryptedBytes = Convert.FromBase64String(cipherTextBase64);
                if (encryptedBytes.Length <= 16)
                    return cipherTextBase64;

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
                // Fallback nếu chuỗi truyền vào không phải base64 AES
                return cipherTextBase64;
            }
        }

        public string ComputeHmac(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant()));
            return Convert.ToBase64String(hashBytes);
        }

        public string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim().ToLowerInvariant();
            
            // Xóa dấu tiếng Việt đơn giản
            string[] vietStrs = new string[]
            {
                "aàảãáạăằẳẵắặâầẩẫấậ", "dđ", "eèẻẽéẹêềểễếệ",
                "iìỉĩíị", "oòỏõóọôồổỗốộơờởỡớợ",
                "uùủũúụưừửữứự", "yỳỷỹýỵ"
            };

            for (int i = 0; i < vietStrs.Length; i++)
            {
                char target = vietStrs[i][0];
                for (int j = 1; j < vietStrs[i].Length; j++)
                {
                    input = input.Replace(vietStrs[i][j], target);
                }
            }

            input = Regex.Replace(input, @"\s+", " ");
            return input;
        }

        public List<string> BuildBigrams(string name)
        {
            var normalized = NormalizeName(name);
            var bigrams = new List<string>();

            if (string.IsNullOrEmpty(normalized))
                return bigrams;

            if (normalized.Length < 2)
            {
                bigrams.Add(normalized);
                return bigrams;
            }

            for (int i = 0; i <= normalized.Length - 2; i++)
            {
                bigrams.Add(normalized.Substring(i, 2));
            }

            return bigrams.Distinct().ToList();
        }

        public string ComputeBitgramBucket(string bigram)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(_hmacKey + bigram));
            // Lấy 2 bytes đầu tiên -> 65536 buckets (BG_0000 -> BG_FFFF)
            ushort bucketValue = BitConverter.ToUInt16(bytes, 0);
            return $"BG_{bucketValue:X4}";
        }
    }
}
