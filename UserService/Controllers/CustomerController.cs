using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Kafka;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;

    public KhachHangController(UserDbContext context, IKafkaProducer kafkaProducer)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
    }

    /// <summary>Lấy thông tin khách hàng hiện tại</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var kh = await _context.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.MaKH == maKH);

        if (kh == null) return NotFound();

        return Ok(new
        {
            kh.MaKH,
            kh.HoTen,
            kh.GioiTinh,
            kh.NgaySinh,
            kh.DiaChi,
            kh.DienThoai,
            kh.Email,
            kh.Hinh,
            TenDangNhap = kh.NguoiDung.TenDangNhap,
            NgayTao = kh.NguoiDung.NgayTao
        });
    }

    /// <summary>Cập nhật thông tin khách hàng</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateKhachHangDto dto)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKH == maKH);
        if (kh == null) return NotFound();

        if (dto.HoTen != null) kh.HoTen = dto.HoTen;
        if (dto.GioiTinh.HasValue) kh.GioiTinh = dto.GioiTinh.Value;
        if (dto.NgaySinh.HasValue) kh.NgaySinh = dto.NgaySinh.Value;
        if (dto.DiaChi != null) kh.DiaChi = dto.DiaChi;
        if (dto.DienThoai != null) kh.DienThoai = dto.DienThoai;
        if (dto.Hinh != null) kh.Hinh = dto.Hinh;

        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("customer.updated", new
        {
            MaKH = kh.MaKH,
            HoTen = kh.HoTen,
            DiaChi = kh.DiaChi,
            DienThoai = kh.DienThoai,
            UpdatedAt = DateTime.UtcNow
        });

        return Ok(new { message = "Cập nhật thông tin thành công" });
    }

    /// <summary>Lấy danh sách tất cả khách hàng (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? search = null)
    {
        var query = _context.KhachHangs.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(k => k.HoTen.Contains(search) || k.Email.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(k => new
            {
                k.MaKH,
                k.HoTen,
                k.GioiTinh,
                k.NgaySinh,
                k.DiaChi,
                k.DienThoai,
                k.Email,
                k.Hinh
            })
            .ToListAsync();

        return Ok(new { total, page, size, items });
    }

    /// <summary>Lấy thông tin khách hàng theo mã (Admin)</summary>
    [HttpGet("{maKH}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetById(string maKH)
    {
        var kh = await _context.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.MaKH == maKH);

        if (kh == null) return NotFound();

        return Ok(new
        {
            kh.MaKH,
            kh.HoTen,
            kh.GioiTinh,
            kh.NgaySinh,
            kh.DiaChi,
            kh.DienThoai,
            kh.Email,
            kh.Hinh,
            TenDangNhap = kh.NguoiDung.TenDangNhap,
            HieuLuc = kh.NguoiDung.HieuLuc
        });
    }

    /// <summary>Khóa/mở khóa tài khoản (Admin)</summary>
    [HttpPut("{maKH}/toggle-lock")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> ToggleLock(string maKH)
    {
        var kh = await _context.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.MaKH == maKH);

        if (kh == null) return NotFound();

        kh.NguoiDung.HieuLuc = !kh.NguoiDung.HieuLuc;
        await _context.SaveChangesAsync();

        return Ok(new { message = kh.NguoiDung.HieuLuc ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản" });
    }
}
