
namespace ProductService.DTOs;
public class ProductResDto
{
    public string Name { get; set; }
    public string CategoryName { get; set; }
    public string? UnitDescription { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Image { get; set; }
    public DateOnly CreatedDate { get; set; }
    public string? Description { get; set; }
}