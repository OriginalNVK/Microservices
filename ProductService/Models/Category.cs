using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models;

[Table("Categories")]
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CategoryAlias { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
