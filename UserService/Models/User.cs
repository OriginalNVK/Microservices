using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

[Table("NguoiDung")]
public class NguoiDung
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string MatKhau { get; set; } = string.Empty;

    public int VaiTro { get; set; } = 0;

    public bool HieuLuc { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? RandomKey { get; set; }

    public KhachHang? KhachHang { get; set; }
    public NhanVien? NhanVien { get; set; }
}
