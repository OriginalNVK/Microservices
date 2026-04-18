using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs;

public class CreateLoaiDto
{
    [Required(ErrorMessage = "Tên loại là bắt buộc")]
    [MaxLength(50)]
    public string TenLoai { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TenLoaiAlias { get; set; }

    public string? MoTa { get; set; }

    public string? Hinh { get; set; }
}

public class UpdateLoaiDto
{
    [MaxLength(50)]
    public string? TenLoai { get; set; }

    [MaxLength(50)]
    public string? TenLoaiAlias { get; set; }

    public string? MoTa { get; set; }

    public string? Hinh { get; set; }
}

public class CreateHangHoaDto
{
    [Required(ErrorMessage = "Tên hàng hóa là bắt buộc")]
    [MaxLength(100)]
    public string TenHH { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TenAlias { get; set; }

    [Required]
    public int MaLoai { get; set; }

    [MaxLength(50)]
    public string? MoTaDonVi { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DonGia { get; set; }

    public string? Hinh { get; set; }

    [Required]
    public DateOnly NgaySX { get; set; }

    [Range(0, 100)]
    public decimal GiamGia { get; set; } = 0;

    public string? MoTa { get; set; }
}

public class UpdateHangHoaDto
{
    [MaxLength(100)]
    public string? TenHH { get; set; }

    [MaxLength(100)]
    public string? TenAlias { get; set; }

    public int? MaLoai { get; set; }

    [MaxLength(50)]
    public string? MoTaDonVi { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DonGia { get; set; }

    public string? Hinh { get; set; }

    public DateOnly? NgaySX { get; set; }

    [Range(0, 100)]
    public decimal? GiamGia { get; set; }

    public string? MoTa { get; set; }
}

public class HangHoaResponseDto
{
    public int MaHH { get; set; }
    public string TenHH { get; set; } = string.Empty;
    public string? TenAlias { get; set; }
    public int MaLoai { get; set; }
    public string TenLoai { get; set; } = string.Empty;
    public string? MoTaDonVi { get; set; }
    public decimal? DonGia { get; set; }
    public decimal GiaSauGiam => DonGia.HasValue ? DonGia.Value * (1 - GiamGia / 100) : 0;
    public string? Hinh { get; set; }
    public DateOnly NgaySX { get; set; }
    public decimal GiamGia { get; set; }
    public int LuotMua { get; set; }
    public string? MoTa { get; set; }
}
