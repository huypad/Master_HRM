using System;
using System.Text;

namespace HRM.Security
{
    // THUẬT TOÁN THẾ THÂN (MOCK)
    // Dùng tạm để test UI và luồng Database
    // Sau này leader sẽ viết class RealSecurityService đè lên class này.
    public class MockSecurityService : IHybridSecurityService
    {
        public string EncryptData(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return rawText;
            // Giả lập mã hóa: Biến thành Base64 để nhìn giống data đã bị mã hóa
            return Convert.ToBase64String(Encoding.UTF8.GetBytes("MOCK_AES_" + rawText));
        }

        public string DecryptData(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            // Giả lập giải mã
            var base64EncodedBytes = Convert.FromBase64String(cipherText);
            var decoded = Encoding.UTF8.GetString(base64EncodedBytes);
            return decoded.Replace("MOCK_AES_", "");
        }

        public string GenerateExactIndex(string rawText)
        {
            // Giả lập hàm băm HMAC: Thêm tiền tố để Database biết đây là mã băm
            return "HMAC_INDEX_" + rawText;
        }

        public string GenerateFuzzyIndex(string rawText)
        {
            // Giả lập băm LSH: Tạm thời băm giống hệt nhau để test database
            return "LSH_BUCKET_" + rawText.Length.ToString();
        }
    }
}
