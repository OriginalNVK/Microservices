using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HangHoaController : ControllerBase
{
    private readonly IProductService _productService;

    public HangHoaController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Lấy danh sách hàng hóa với bộ lọc</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryFilter filter)
    {
        var result = await _productService.GetAllProductsAsync(filter);

        return Ok(result);
    }

    /// <summary>Lấy chi tiết hàng hóa</summary>
    [HttpGet("{maHH}")]
    public async Task<IActionResult> GetById(int maHH)
    {
        var hh = await _productService.GetProductByIdAsync(maHH);

        if (hh == null) return NotFound();
        return Ok(hh);
    }

    /// <summary>Thêm hàng hóa mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Create([FromBody] CreateHangHoaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _productService.CreateProductAsync(dto);
        if (!result.Success) return BadRequest(new { message = result.Message });

        return CreatedAtAction(nameof(GetById), new { maHH = result.MaHH }, new { MaHH = result.MaHH, dto.TenHH });
    }

    /// <summary>Sửa hàng hóa (Admin)</summary>
    [HttpPut("{maHH}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Update(int maHH, [FromBody] UpdateHangHoaDto dto)
    {
        var result = await _productService.UpdateProductAsync(maHH, dto);
        if (result.Success) return Ok(new { message = result.Message });
        if (result.Message == "Không tìm thấy hàng hóa") return NotFound();
        return BadRequest(new { message = result.Message });
    }

    /// <summary>Xóa hàng hóa (Admin)</summary>
    [HttpDelete("{maHH}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Delete(int maHH)
    {
        var deleted = await _productService.DeleteProductAsync(maHH);
        if (!deleted) return NotFound();

        return Ok(new { message = "Xóa hàng hóa thành công" });
    }

    /// <summary>Lấy hàng hóa bán chạy nhất</summary>
    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellers([FromQuery] int top = 10)
    {
        var items = await _productService.GetBestSellersAsync(top);

        return Ok(items);
    }

    /// <summary>Lấy hàng hóa đang giảm giá</summary>
    [HttpGet("on-sale")]
    public async Task<IActionResult> GetOnSale()
    {
        var items = await _productService.GetOnSaleAsync();

        return Ok(items);
    }
}
