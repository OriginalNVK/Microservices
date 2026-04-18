using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoaiController : ControllerBase
{
    private readonly ProductDbContext _context;

    public LoaiController(ProductDbContext context)
    {
        _context = context;
    }

    /// <summary>Lấy tất cả loại hàng hóa</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loais = await _context.Loais
            .Select(l => new
            {
                l.MaLoai,
                l.TenLoai,
                l.TenLoaiAlias,
                l.MoTa,
                l.Hinh,
                SoHangHoa = l.HangHoas.Count
            })
            .ToListAsync();

        return Ok(loais);
    }

    /// <summary>Lấy chi tiết loại hàng hóa</summary>
    [HttpGet("{maLoai}")]
    public async Task<IActionResult> GetById(int maLoai)
    {
        var loai = await _context.Loais
            .Include(l => l.HangHoas)
            .FirstOrDefaultAsync(l => l.MaLoai == maLoai);

        if (loai == null) return NotFound();

        return Ok(new
        {
            loai.MaLoai,
            loai.TenLoai,
            loai.TenLoaiAlias,
            loai.MoTa,
            loai.Hinh,
            SoHangHoa = loai.HangHoas.Count
        });
    }

    /// <summary>Thêm loại hàng hóa mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Create([FromBody] CreateLoaiDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var loai = new Loai
        {
            TenLoai = dto.TenLoai,
            TenLoaiAlias = dto.TenLoaiAlias ?? dto.TenLoai.ToLower().Replace(" ", "-"),
            MoTa = dto.MoTa,
            Hinh = dto.Hinh
        };
        _context.Loais.Add(loai);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { maLoai = loai.MaLoai }, loai);
    }

    /// <summary>Cập nhật loại hàng hóa (Admin)</summary>
    [HttpPut("{maLoai}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Update(int maLoai, [FromBody] UpdateLoaiDto dto)
    {
        var loai = await _context.Loais.FindAsync(maLoai);
        if (loai == null) return NotFound();

        if (dto.TenLoai != null) loai.TenLoai = dto.TenLoai;
        if (dto.TenLoaiAlias != null) loai.TenLoaiAlias = dto.TenLoaiAlias;
        if (dto.MoTa != null) loai.MoTa = dto.MoTa;
        if (dto.Hinh != null) loai.Hinh = dto.Hinh;

        await _context.SaveChangesAsync();
        return Ok(loai);
    }

    /// <summary>Xóa loại hàng hóa (Admin)</summary>
    [HttpDelete("{maLoai}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Delete(int maLoai)
    {
        var loai = await _context.Loais.Include(l => l.HangHoas).FirstOrDefaultAsync(l => l.MaLoai == maLoai);
        if (loai == null) return NotFound();

        if (loai.HangHoas.Any())
            return BadRequest(new { message = "Không thể xóa loại đang có hàng hóa" });

        _context.Loais.Remove(loai);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Xóa loại thành công" });
    }
}
