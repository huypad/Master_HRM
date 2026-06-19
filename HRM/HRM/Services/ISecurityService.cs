namespace HRM.Services
{
    public interface ISecurityService
    {
        string GenerateSearchIndex(string rawData, string columnProfile);
        string EncryptData(string rawData);
        string DecryptData(string encryptedData);
    }
}
