namespace HRM.Security
{

    // KHÔNG tự viết logic mã hóa riêng ngoài class này.
    public interface IHybridSecurityService
    {
        string EncryptData(string rawText);
        string DecryptData(string cipherText);
        string GenerateExactIndex(string rawText); 
        string GenerateFuzzyIndex(string rawText); 
    }
}
