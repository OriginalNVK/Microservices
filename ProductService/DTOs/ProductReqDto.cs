using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs;
public class ProductReqDto
{
    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [MaxLength(50)]
    public string? UnitDescription { get; set; }
    
    public decimal? UnitPrice { get; set; }

    public string? Image { get; set; }

    [Required]
    public DateOnly CreatedDate { get; set; }

    public string? Description { get; set; }
}




