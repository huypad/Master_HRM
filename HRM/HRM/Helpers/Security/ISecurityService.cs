namespace HRM.Helpers.Security
{
    public interface ISecurityService
    {
        // Đầu vào là chuỗi nguyên bản -> đầu ra là giá trị để lưu vào cột Search_Index
        string GenerateSearchIndex(string rawData, string columnProfile);

        // Đầu vào là chuỗi nguyên bản -> đầu ra là chuỗi đã mã hóa
        string EncryptData(string rawData);

        // Đầu vào là chuỗi mã hóa -> đầu ra là dữ liệu thật
        string DecryptData(string encryptedData);
    }
}