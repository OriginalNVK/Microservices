using ProductService.Models;

namespace ProductService.Repositories;

/// <summary>
/// Defines data access operations for products and categories.
/// </summary>
public interface IProductRepository
{
	IQueryable<Category> Categories { get; }
	IQueryable<Product> Products { get; }
	Task<Category?> FindCategoryByIdAsync(int categoryId);
	Task<Product?> FindProductByIdAsync(int productId);
	Task AddCategoryAsync(Category category);
	Task AddProductAsync(Product product);
	void RemoveCategory(Category category);
	void RemoveProduct(Product product);
	Task<int> SaveChangesAsync();
}
