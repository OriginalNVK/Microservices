using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoaiController : ControllerBase
{
    private readonly IProductService _productService;

    public LoaiController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Lấy tất cả loại hàng hóa</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loais = await _productService.GetAllCategoriesAsync();

        return Ok(loais);
    }

    /// <summary>Lấy chi tiết loại hàng hóa</summary>
    [HttpGet("{maLoai}")]
    public async Task<IActionResult> GetById(int maLoai)
    {
        var loai = await _productService.GetCategoryByIdAsync(maLoai);

        if (loai == null) return NotFound();

        return Ok(loai);
    }

    /// <summary>Thêm loại hàng hóa mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Create([FromBody] CreateLoaiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var loai = await _productService.CreateCategoryAsync(dto);

        return CreatedAtAction(nameof(GetById), new { maLoai = loai.MaLoai }, loai);
    }

    /// <summary>Cập nhật loại hàng hóa (Admin)</summary>
    [HttpPut("{maLoai}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Update(int maLoai, [FromBody] UpdateLoaiDto dto)
    {
        var loai = await _productService.UpdateCategoryAsync(maLoai, dto);
        if (loai == null) return NotFound();

        return Ok(loai);
    }

    /// <summary>Xóa loại hàng hóa (Admin)</summary>
    [HttpDelete("{maLoai}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Delete(int maLoai)
    {
        var result = await _productService.DeleteCategoryAsync(maLoai);
        if (result.Success) return Ok(new { message = result.Message });
        if (result.Message == "Không tìm thấy loại hàng hóa") return NotFound();

        return BadRequest(new { message = result.Message });
    }
}
