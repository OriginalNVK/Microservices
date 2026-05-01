using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Services;

/// <summary>
/// Defines product and category business operations.
/// </summary>
public interface IProductService
{
	Task<object> GetAllProductsAsync(ProductQueryFilter filter);
	Task<ProductResponseDto?> GetProductByIdAsync(int productId);
	Task<(bool Success, string Message, int ProductId)> CreateProductAsync(CreateProductDto dto);
	Task<(bool Success, string Message)> UpdateProductAsync(int productId, UpdateProductDto dto);
	Task<bool> DeleteProductAsync(int productId);
	Task<List<ProductResponseDto>> GetBestSellersAsync(int top);
	Task<List<ProductResponseDto>> GetOnSaleAsync();
	Task<List<object>> GetAllCategoriesAsync();
	Task<object?> GetCategoryByIdAsync(int categoryId);
	Task<Category> CreateCategoryAsync(CreateCategoryDto dto);
	Task<Category?> UpdateCategoryAsync(int categoryId, UpdateCategoryDto dto);
	Task<(bool Success, string Message)> DeleteCategoryAsync(int categoryId);
}
