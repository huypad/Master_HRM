namespace HRM.Security
{

    // chỉ được gọi các hàm này trong Controller/Service/Repository.
    // KHÔNG tự viết logic mã hóa riêng ngoài class này.
    public interface IHybridSecurityService
    {
        string EncryptData(string rawText);
        string DecryptData(string cipherText);
        string GenerateExactIndex(string rawText); // Dành cho CCCD, SĐT
        string GenerateFuzzyIndex(string rawText); // Dành cho Họ Tên
    }
}
