using System.ComponentModel.DataAnnotations;

namespace HRM.Model.NhanVien
{
    public class NhanVienModel
    {
        [Required]
        public string MaNV { get; set; } = null!;

        public string? Holot { get; set; }

        [Required]
        public string Ten { get; set; } = null!;

        public DateTime? Ngaysinh { get; set; }
        public string? CMND { get; set; }
        public string? Mobile { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
    }
}
