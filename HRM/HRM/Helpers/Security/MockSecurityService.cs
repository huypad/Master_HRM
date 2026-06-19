using HRM.Common;

namespace HRM.Helpers.Security
{
    public class MockSecurityService : ISecurityService
    {
        public string GenerateSearchIndex(string rawData, string columnProfile)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return string.Empty;
            }

            // columnProfile giúp phân biệt hash của CMND, Số tài khoản, Họ tên...
            var input = $"{columnProfile}:{rawData}";

            // Tạm dùng SecurityIndexHelper hiện có trong project
            var hashBytes = SecurityIndexHelper.ComputeHashWithSalt(input);

            // Interface trả về string nên đổi byte[] hash thành chuỗi HEX
            return Convert.ToHexString(hashBytes);
        }

        public string EncryptData(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return string.Empty;
            }

            // Tạm dùng EncryptionHelper hiện có trong project
            // Đây là phần thuật toán nằm bên trong Black-box
            var encryptedBytes = EncryptionHelper.EncryptString(rawData);

            if (encryptedBytes == null || encryptedBytes.Length == 0)
            {
                return string.Empty;
            }

            // Interface yêu cầu trả string nên đổi byte[] mã hóa thành Base64
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptData(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData))
            {
                return string.Empty;
            }

            try
            {
                // encryptedData là chuỗi Base64 của byte[] mã hóa
                var encryptedBytes = Convert.FromBase64String(encryptedData);

                return EncryptionHelper.DecryptString(encryptedBytes) ?? string.Empty;
            }
            catch
            {
                // Tránh lỗi khi gặp dữ liệu cũ hoặc dữ liệu không đúng định dạng
                return string.Empty;
            }
        }
    }
}