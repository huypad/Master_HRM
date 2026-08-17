namespace HRM.Model.Healthcare
{
   
    /// DTO đại diện cho dữ liệu Bệnh nhân từ HealthcareDB sau khi đã được giải mã AES-256.
    /// Dùng làm kiểu dữ liệu T cho PagedResult<T>.
    
    public class PatientDto
    {
        public int PatientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CCCD { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? Blood_Type { get; set; }
        public string? Email { get; set; }
    }
}
