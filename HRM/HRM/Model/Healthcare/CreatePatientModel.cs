namespace HRM.Model.Healthcare
{
    /// <summary>
    /// Model input khi tạo hoặc cập nhật thông tin bệnh nhân.
    /// </summary>
    public class CreatePatientModel
    {
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? CCCD { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
        public string? InsuranceId { get; set; }
    }
}
