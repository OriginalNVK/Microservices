using System.ComponentModel.DataAnnotations;

namespace CartService.DTOs;

public class AddToCartDto
{
    [Required]
    public int MaHH { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
    public int SoLuong { get; set; }
}

public class UpdateCartDto
{
    [Required]
    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
    public int SoLuong { get; set; }
}

public class CartItemResponseDto
{
    public int MaCart { get; set; }
    public int MaHH { get; set; }
    public string TenHH { get; set; } = string.Empty;
    public string? Hinh { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiamGia { get; set; }
    public decimal GiaSauGiam => DonGia * (1 - GiamGia / 100);
    public decimal ThanhTien => GiaSauGiam * SoLuong;
    public DateTime NgayThem { get; set; }
}

public class CartSummaryDto
{
    public string MaKH { get; set; } = string.Empty;
    public List<CartItemResponseDto> Items { get; set; } = new();
    public int TongSoMat => Items.Sum(i => i.SoLuong);
    public decimal TongTien => Items.Sum(i => i.ThanhTien);
}
