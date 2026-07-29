namespace HRM.Model.Healthcare
{
    /// <summary>
    /// DTO trả về frontend – dữ liệu bệnh nhân đã giải mã.
    /// </summary>
    public class PatientDto
    {
        public int PatientId { get; set; }
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? CCCD { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
    }
}
