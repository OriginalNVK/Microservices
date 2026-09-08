using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Services;

public interface ICategoryService
{
	List<Category> GetAllCategories();
	Category? GetCategoryById(int categoryId);
	Category CreateCategory(string categoryName);
	Category UpdateCategory(int categoryId, string categoryName);
	bool DeleteCategory(int categoryId);
}
