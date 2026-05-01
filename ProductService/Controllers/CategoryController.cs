using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly IProductService _productService;

    public CategoryController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Get all categories</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _productService.GetAllCategoriesAsync();

        return Ok(categories);
    }

    /// <summary>Get category detail</summary>
    [HttpGet("{categoryId}")]
    public async Task<IActionResult> GetById(int categoryId)
    {
        var category = await _productService.GetCategoryByIdAsync(categoryId);

        if (category == null) return NotFound();

        return Ok(category);
    }

    /// <summary>Create a new category (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = await _productService.CreateCategoryAsync(dto);

        return CreatedAtAction(nameof(GetById), new { categoryId = category.CategoryId }, category);
    }

    /// <summary>Update a category (Admin)</summary>
    [HttpPut("{categoryId}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Update(int categoryId, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _productService.UpdateCategoryAsync(categoryId, dto);
        if (category == null) return NotFound();

        return Ok(category);
    }

    /// <summary>Delete a category (Admin)</summary>
    [HttpDelete("{categoryId}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var result = await _productService.DeleteCategoryAsync(categoryId);
        if (result.Success) return Ok(new { message = result.Message });
        if (result.Message == "Category not found") return NotFound();

        return BadRequest(new { message = result.Message });
    }
}
