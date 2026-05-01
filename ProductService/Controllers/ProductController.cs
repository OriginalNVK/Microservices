using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Get products with filters</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryFilter filter)
    {
        var result = await _productService.GetAllProductsAsync(filter);

        return Ok(result);
    }

    /// <summary>Get product detail</summary>
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetById(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);

        if (product == null) return NotFound();
        return Ok(product);
    }

    /// <summary>Create a new product (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _productService.CreateProductAsync(dto);
        if (!result.Success) return BadRequest(new { message = result.Message });

        return CreatedAtAction(nameof(GetById), new { productId = result.ProductId }, new { ProductId = result.ProductId, dto.ProductName });
    }

    /// <summary>Update a product (Admin)</summary>
    [HttpPut("{productId}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Update(int productId, [FromBody] UpdateProductDto dto)
    {
        var result = await _productService.UpdateProductAsync(productId, dto);
        if (result.Success) return Ok(new { message = result.Message });
        if (result.Message == "Product not found") return NotFound();
        return BadRequest(new { message = result.Message });
    }

    /// <summary>Delete a product (Admin)</summary>
    [HttpDelete("{productId}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Delete(int productId)
    {
        var deleted = await _productService.DeleteProductAsync(productId);
        if (!deleted) return NotFound();

        return Ok(new { message = "Product deleted successfully" });
    }

    /// <summary>Get best-selling products</summary>
    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellers([FromQuery] int top = 10)
    {
        var items = await _productService.GetBestSellersAsync(top);

        return Ok(items);
    }

    /// <summary>Get products on sale</summary>
    [HttpGet("on-sale")]
    public async Task<IActionResult> GetOnSale()
    {
        var items = await _productService.GetOnSaleAsync();

        return Ok(items);
    }
}
