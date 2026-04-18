using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

[Table("KhachHang")]
public class KhachHang
{
    [Key]
    [MaxLength(20)]
    public string MaKH { get; set; } = string.Empty;

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    public bool GioiTinh { get; set; }

    public DateOnly NgaySinh { get; set; }

    [MaxLength(200)]
    public string? DiaChi { get; set; }

    [MaxLength(20)]
    public string? DienThoai { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Hinh { get; set; }

    [ForeignKey(nameof(UserId))]
    public NguoiDung NguoiDung { get; set; } = null!;
}
