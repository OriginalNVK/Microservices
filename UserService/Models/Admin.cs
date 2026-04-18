using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

[Table("NhanVien")]
public class NhanVien
{
    [Key]
    [MaxLength(50)]
    public string MaNV { get; set; } = string.Empty;

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DienThoai { get; set; }

    [ForeignKey(nameof(UserId))]
    public NguoiDung NguoiDung { get; set; } = null!;
}
