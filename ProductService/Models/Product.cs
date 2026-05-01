using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models;

[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ProductAlias { get; set; }

    public int CategoryId { get; set; }

    [MaxLength(50)]
    public string? DescriptionUnit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [MaxLength(255)]
    public string? Image { get; set; }

    public DateOnly CreatedDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Discount { get; set; } = 0;

    public int PurchaseCount { get; set; } = 0;

    public string? Description { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;
}
