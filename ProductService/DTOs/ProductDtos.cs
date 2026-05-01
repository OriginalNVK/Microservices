using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CategoryAlias { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }
}

public class UpdateCategoryDto
{
    [MaxLength(50)]
    public string? CategoryName { get; set; }

    [MaxLength(50)]
    public string? CategoryAlias { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }
}

public class CreateProductDto
{
    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(100)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ProductAlias { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [MaxLength(50)]
    public string? DescriptionUnit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    public string? Image { get; set; }

    [Required]
    public DateOnly CreatedDate { get; set; }

    [Range(0, 100)]
    public decimal Discount { get; set; } = 0;

    public string? Description { get; set; }
}

public class UpdateProductDto
{
    [MaxLength(100)]
    public string? ProductName { get; set; }

    [MaxLength(100)]
    public string? ProductAlias { get; set; }

    public int? CategoryId { get; set; }

    [MaxLength(50)]
    public string? DescriptionUnit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    public string? Image { get; set; }

    public DateOnly? CreatedDate { get; set; }

    [Range(0, 100)]
    public decimal? Discount { get; set; }

    public string? Description { get; set; }
}

public class ProductResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductAlias { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? DescriptionUnit { get; set; }
    public decimal? Price { get; set; }
    public decimal DiscountedPrice => Price.HasValue ? Price.Value * (1 - Discount / 100) : 0;
    public string? Image { get; set; }
    public DateOnly CreatedDate { get; set; }
    public decimal Discount { get; set; }
    public int PurchaseCount { get; set; }
    public string? Description { get; set; }
}
