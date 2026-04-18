using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CartService.Models;

[Table("Cart")]
public class Cart
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaCart { get; set; }

    [Required]
    [MaxLength(20)]
    public string MaKH { get; set; } = string.Empty;

    public int MaHH { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }

    public DateTime NgayThem { get; set; } = DateTime.Now;
}

[Table("HangHoaCache")]
public class HangHoaCache
{
    [Key]
    public int MaHH { get; set; }

    [Required]
    [MaxLength(100)]
    public string TenHH { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal GiamGia { get; set; }

    [MaxLength(255)]
    public string? Hinh { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
