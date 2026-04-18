using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Kafka;
using ProductService.Models;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HangHoaController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;

    public HangHoaController(ProductDbContext context, IKafkaProducer kafkaProducer)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
    }

    /// <summary>Lấy danh sách hàng hóa với bộ lọc</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? maLoai,
        [FromQuery] string? search,
        [FromQuery] decimal? minGia,
        [FromQuery] decimal? maxGia,
        [FromQuery] string? sortBy = "tenHH",
        [FromQuery] bool ascending = true,
        [FromQuery] int page = 1,
        [FromQuery] int size = 12)
    {
        var query = _context.HangHoas.Include(h => h.Loai).AsQueryable();

        if (maLoai.HasValue)
            query = query.Where(h => h.MaLoai == maLoai);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(h => h.TenHH.Contains(search) || (h.MoTa != null && h.MoTa.Contains(search)));

        if (minGia.HasValue)
            query = query.Where(h => h.DonGia >= minGia);

        if (maxGia.HasValue)
            query = query.Where(h => h.DonGia <= maxGia);

        query = sortBy?.ToLower() switch
        {
            "dongia" => ascending ? query.OrderBy(h => h.DonGia) : query.OrderByDescending(h => h.DonGia),
            "luotmua" => ascending ? query.OrderBy(h => h.LuotMua) : query.OrderByDescending(h => h.LuotMua),
            "giamgia" => ascending ? query.OrderBy(h => h.GiamGia) : query.OrderByDescending(h => h.GiamGia),
            _ => ascending ? query.OrderBy(h => h.TenHH) : query.OrderByDescending(h => h.TenHH)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(h => new HangHoaResponseDto
            {
                MaHH = h.MaHH,
                TenHH = h.TenHH,
                TenAlias = h.TenAlias,
                MaLoai = h.MaLoai,
                TenLoai = h.Loai.TenLoai,
                MoTaDonVi = h.MoTaDonVi,
                DonGia = h.DonGia,
                Hinh = h.Hinh,
                NgaySX = h.NgaySX,
                GiamGia = h.GiamGia,
                LuotMua = h.LuotMua,
                MoTa = h.MoTa
            })
            .ToListAsync();

        return Ok(new { total, page, size, items });
    }

    /// <summary>Lấy chi tiết hàng hóa</summary>
    [HttpGet("{maHH}")]
    public async Task<IActionResult> GetById(int maHH)
    {
        var hh = await _context.HangHoas
            .Include(h => h.Loai)
            .FirstOrDefaultAsync(h => h.MaHH == maHH);

        if (hh == null) return NotFound();

        return Ok(new HangHoaResponseDto
        {
            MaHH = hh.MaHH,
            TenHH = hh.TenHH,
            TenAlias = hh.TenAlias,
            MaLoai = hh.MaLoai,
            TenLoai = hh.Loai.TenLoai,
            MoTaDonVi = hh.MoTaDonVi,
            DonGia = hh.DonGia,
            Hinh = hh.Hinh,
            NgaySX = hh.NgaySX,
            GiamGia = hh.GiamGia,
            LuotMua = hh.LuotMua,
            MoTa = hh.MoTa
        });
    }

    /// <summary>Thêm hàng hóa mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Create([FromBody] CreateHangHoaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!await _context.Loais.AnyAsync(l => l.MaLoai == dto.MaLoai))
            return BadRequest(new { message = "Loại hàng hóa không tồn tại" });

        var hh = new HangHoa
        {
            TenHH = dto.TenHH,
            TenAlias = dto.TenAlias ?? dto.TenHH.ToLower().Replace(" ", "-"),
            MaLoai = dto.MaLoai,
            MoTaDonVi = dto.MoTaDonVi,
            DonGia = dto.DonGia,
            Hinh = dto.Hinh,
            NgaySX = dto.NgaySX,
            GiamGia = dto.GiamGia,
            LuotMua = 0,
            MoTa = dto.MoTa
        };

        _context.HangHoas.Add(hh);
        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("product.created", new
        {
            MaHH = hh.MaHH,
            TenHH = hh.TenHH,
            DonGia = hh.DonGia,
            GiamGia = hh.GiamGia,
            Hinh = hh.Hinh,
            CreatedAt = DateTime.UtcNow
        });

        return CreatedAtAction(nameof(GetById), new { maHH = hh.MaHH }, new { hh.MaHH, hh.TenHH });
    }

    /// <summary>Sửa hàng hóa (Admin)</summary>
    [HttpPut("{maHH}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Update(int maHH, [FromBody] UpdateHangHoaDto dto)
    {
        var hh = await _context.HangHoas.FindAsync(maHH);
        if (hh == null) return NotFound();

        if (dto.MaLoai.HasValue && !await _context.Loais.AnyAsync(l => l.MaLoai == dto.MaLoai))
            return BadRequest(new { message = "Loại hàng hóa không tồn tại" });

        if (dto.TenHH != null) hh.TenHH = dto.TenHH;
        if (dto.TenAlias != null) hh.TenAlias = dto.TenAlias;
        if (dto.MaLoai.HasValue) hh.MaLoai = dto.MaLoai.Value;
        if (dto.MoTaDonVi != null) hh.MoTaDonVi = dto.MoTaDonVi;
        if (dto.DonGia.HasValue) hh.DonGia = dto.DonGia;
        if (dto.Hinh != null) hh.Hinh = dto.Hinh;
        if (dto.NgaySX.HasValue) hh.NgaySX = dto.NgaySX.Value;
        if (dto.GiamGia.HasValue) hh.GiamGia = dto.GiamGia.Value;
        if (dto.MoTa != null) hh.MoTa = dto.MoTa;

        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("product.updated", new
        {
            MaHH = hh.MaHH,
            TenHH = hh.TenHH,
            DonGia = hh.DonGia,
            GiamGia = hh.GiamGia,
            Hinh = hh.Hinh,
            UpdatedAt = DateTime.UtcNow
        });

        return Ok(new { message = "Cập nhật thành công" });
    }

    /// <summary>Xóa hàng hóa (Admin)</summary>
    [HttpDelete("{maHH}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Delete(int maHH)
    {
        var hh = await _context.HangHoas.FindAsync(maHH);
        if (hh == null) return NotFound();

        _context.HangHoas.Remove(hh);
        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("product.deleted", new
        {
            MaHH = maHH,
            DeletedAt = DateTime.UtcNow
        });

        return Ok(new { message = "Xóa hàng hóa thành công" });
    }

    /// <summary>Lấy hàng hóa bán chạy nhất</summary>
    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellers([FromQuery] int top = 10)
    {
        var items = await _context.HangHoas
            .Include(h => h.Loai)
            .OrderByDescending(h => h.LuotMua)
            .Take(top)
            .Select(h => new HangHoaResponseDto
            {
                MaHH = h.MaHH,
                TenHH = h.TenHH,
                MaLoai = h.MaLoai,
                TenLoai = h.Loai.TenLoai,
                DonGia = h.DonGia,
                GiamGia = h.GiamGia,
                Hinh = h.Hinh,
                LuotMua = h.LuotMua,
                NgaySX = h.NgaySX
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Lấy hàng hóa đang giảm giá</summary>
    [HttpGet("on-sale")]
    public async Task<IActionResult> GetOnSale()
    {
        var items = await _context.HangHoas
            .Include(h => h.Loai)
            .Where(h => h.GiamGia > 0)
            .OrderByDescending(h => h.GiamGia)
            .Select(h => new HangHoaResponseDto
            {
                MaHH = h.MaHH,
                TenHH = h.TenHH,
                MaLoai = h.MaLoai,
                TenLoai = h.Loai.TenLoai,
                DonGia = h.DonGia,
                GiamGia = h.GiamGia,
                Hinh = h.Hinh,
                LuotMua = h.LuotMua,
                NgaySX = h.NgaySX
            })
            .ToListAsync();

        return Ok(items);
    }
}
