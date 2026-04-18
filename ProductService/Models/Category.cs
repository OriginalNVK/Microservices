using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models;

[Table("Loai")]
public class Loai
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaLoai { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenLoai { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TenLoaiAlias { get; set; }

    public string? MoTa { get; set; }

    public string? Hinh { get; set; }

    public ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
