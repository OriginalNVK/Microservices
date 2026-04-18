using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models;

[Table("HangHoa")]
public class HangHoa
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaHH { get; set; }

    [Required]
    [MaxLength(100)]
    public string TenHH { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TenAlias { get; set; }

    public int MaLoai { get; set; }

    [MaxLength(50)]
    public string? MoTaDonVi { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DonGia { get; set; }

    [MaxLength(255)]
    public string? Hinh { get; set; }

    public DateOnly NgaySX { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal GiamGia { get; set; } = 0;

    public int LuotMua { get; set; } = 0;

    public string? MoTa { get; set; }

    [ForeignKey(nameof(MaLoai))]
    public Loai Loai { get; set; } = null!;
}
