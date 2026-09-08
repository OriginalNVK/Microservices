using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProductService.Models;

[Table("Product")]
[Index(nameof(Name), IsUnique = true)]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public int CategoryId { get; set; }

    [Required]
    public int Stock { get; set; }

    [MaxLength(50)]
    public string? UnitDescription { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitPrice { get; set; }

    [MaxLength(255)]
    public string? Image { get; set; }

    [Required]
    public DateOnly CreatedDate { get; set; }

    public string? Description { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;
}
