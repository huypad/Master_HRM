using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Entities
{
    [Table("Tbl_Nhanvien")]
    public class NhanVien
    {
        [Key]
        public decimal Id_NV { get; set; }

        public string? MaNV { get; set; }

        public string? Holot { get; set; }

        public string? Ten { get; set; }

        public DateTime? Ngaysinh { get; set; }

        public string? CMND { get; set; }

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        public string? Sotaikhoan { get; set; }

        public bool? Disable { get; set; }

        public byte[]? I_Holot { get; set; }
        public byte[]? I_Ten { get; set; }
        public byte[]? I_CMND { get; set; }
        public byte[]? I_Sotaikhoan { get; set; }

        public byte[]? CMNDHash { get; set; }
        public byte[]? SotaikhoanHash { get; set; }
    }
}
