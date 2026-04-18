using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceService.Models;

[Table("HoaDon")]
public class HoaDon
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaHD { get; set; }

    [Required]
    [MaxLength(20)]
    public string MaKH { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? HoTenKH { get; set; }

    [MaxLength(100)]
    public string? EmailKH { get; set; }

    [MaxLength(20)]
    public string? DienThoaiKH { get; set; }

    public DateTime NgayDat { get; set; }

    public DateOnly? NgayCan { get; set; }

    public DateOnly? NgayGiao { get; set; }

    [MaxLength(100)]
    public string? HoTen { get; set; }

    [Required]
    [MaxLength(200)]
    public string DiaChi { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CachThanhToan { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CachVanChuyen { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PhiVanChuyen { get; set; } = 0;

    [MaxLength(50)]
    public string? MaNV { get; set; }

    [MaxLength(500)]
    public string? GhiChu { get; set; }

    public int TrangThai { get; set; } = 0;

    public ICollection<ChiTietHD> ChiTietHDs { get; set; } = new List<ChiTietHD>();
}

[Table("ChiTietHD")]
public class ChiTietHD
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaCT { get; set; }

    public int MaHD { get; set; }

    public int MaHH { get; set; }

    [MaxLength(100)]
    public string TenHH { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }

    [Range(1, int.MaxValue)]
    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal GiamGia { get; set; } = 0;

    [ForeignKey(nameof(MaHD))]
    public HoaDon HoaDon { get; set; } = null!;
}
