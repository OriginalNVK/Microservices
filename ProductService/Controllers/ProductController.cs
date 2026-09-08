using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Models;
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
    public List<ProductResDto> GetAll()
    {
        return _productService.GetAllProducts();
    }

    /// <summary>Get product detail</summary>
    [HttpGet("{productId}")]
    public ProductResDto? GetById(int productId)
    {
        return _productService.GetProductById(productId);
    }

    /// <summary>Create a new product (Admin)</summary>
    [HttpPost]
    public ProductResDto Create([FromBody] ProductReqDto productDto)
    {
        if (!ModelState.IsValid)
        {
            string errorMessage = string.Join("; \n", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage));
            throw new ArgumentException(errorMessage);
        }

        return _productService.CreateProduct(productDto);
    }

    /// <summary>Update a product (Admin)</summary>
    [HttpPut("{productId}")]
    public ProductResDto Update(int productId, [FromBody] ProductReqDto productDto)
    {
        if (!ModelState.IsValid)
        {
            string errorMessage = string.Join("; \n", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage));
            throw new ArgumentException(errorMessage);
        }

        return _productService.UpdateProduct(productId, productDto);
    }

    /// <summary>Delete a product (Admin)</summary>
    [HttpDelete("{productId}")]
    public bool Delete(int productId)
    {
        return _productService.DeleteProduct(productId);
    }
}
