using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class CreateOrderDto
{
    [Required]
    [MaxLength(200)]
    public string DiaChi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? HoTen { get; set; }

    [Required]
    [MaxLength(50)]
    public string CachThanhToan { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CachVanChuyen { get; set; } = string.Empty;

    public DateOnly? NgayCan { get; set; }

    [MaxLength(500)]
    public string? GhiChu { get; set; }

    [Required]
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    [Required]
    public int MaHH { get; set; }

    [Required]
    [MaxLength(100)]
    public string TenHH { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int SoLuong { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal DonGia { get; set; }

    [Range(0, 100)]
    public decimal GiamGia { get; set; } = 0;
}

public class UpdateOrderStatusDto
{
    [Required]
    public int TrangThai { get; set; }

    [MaxLength(50)]
    public string? MaNV { get; set; }

    public DateOnly? NgayGiao { get; set; }
}

public class OrderResponseDto
{
    public int MaHD { get; set; }
    public string MaKH { get; set; } = string.Empty;
    public DateTime NgayDat { get; set; }
    public DateOnly? NgayCan { get; set; }
    public DateOnly? NgayGiao { get; set; }
    public string? HoTen { get; set; }
    public string DiaChi { get; set; } = string.Empty;
    public string CachThanhToan { get; set; } = string.Empty;
    public string CachVanChuyen { get; set; } = string.Empty;
    public decimal PhiVanChuyen { get; set; }
    public string? MaNV { get; set; }
    public string? GhiChu { get; set; }
    public int TrangThai { get; set; }
    public string TrangThaiText => TrangThai switch
    {
        0 => "Chờ xác nhận",
        1 => "Đã xác nhận",
        2 => "Đang giao hàng",
        3 => "Đã giao hàng",
        4 => "Đã hủy",
        _ => "Không xác định"
    };
    public List<ChiTietHDDto> ChiTiet { get; set; } = new();
    public decimal TongTien => ChiTiet.Sum(c => c.ThanhTien) + PhiVanChuyen;
}

public class ChiTietHDDto
{
    public int MaCT { get; set; }
    public int MaHH { get; set; }
    public string TenHH { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiamGia { get; set; }
    public decimal GiaSauGiam => DonGia * (1 - GiamGia / 100);
    public decimal ThanhTien => GiaSauGiam * SoLuong;
}
