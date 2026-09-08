using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Get all categories</summary>
    [HttpGet]
    public List<Category> GetAll()
    {
        return _categoryService.GetAllCategories();
    }

    /// <summary>Get category detail</summary>
    [HttpGet("{categoryId}")]
    public Category? GetById(int categoryId)
    {
        return _categoryService.GetCategoryById(categoryId);
    }

    /// <summary>Create a new category (Admin)</summary>
    [HttpPost]
    public Category Create([FromBody] string categoryName)
    {
        if (!ModelState.IsValid)
        {
            throw new ArgumentException("Invalid category name");
        }

        return _categoryService.CreateCategory(categoryName);
    }

    /// <summary>Update a category (Admin)</summary>
    [HttpPut("{categoryId}")]
    public Category Update(int categoryId, [FromBody] string categoryName)
    {
        return _categoryService.UpdateCategory(categoryId, categoryName);
    }

    /// <summary>Delete a category (Admin)</summary>
    [HttpDelete("{categoryId}")]
    public bool Delete(int categoryId)
    {
        return _categoryService.DeleteCategory(categoryId);
    }
}
