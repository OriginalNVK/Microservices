using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Kafka;
using UserService.Models;
using BCrypt.Net;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "1")]
public class NhanVienController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;

    public NhanVienController(UserDbContext context, IKafkaProducer kafkaProducer)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
    }

    /// <summary>Lấy danh sách nhân viên</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _context.NhanViens.Include(nv => nv.NguoiDung).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(nv => nv.HoTen.Contains(search) || nv.Email.Contains(search));

        var items = await query.Select(nv => new
        {
            nv.MaNV,
            nv.HoTen,
            nv.Email,
            nv.DienThoai,
            TenDangNhap = nv.NguoiDung.TenDangNhap,
            HieuLuc = nv.NguoiDung.HieuLuc,
            NgayTao = nv.NguoiDung.NgayTao
        }).ToListAsync();

        return Ok(items);
    }

    /// <summary>Thêm nhân viên mới</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNhanVienDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == dto.TenDangNhap))
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });

        if (await _context.NhanViens.AnyAsync(nv => nv.MaNV == dto.MaNV))
            return BadRequest(new { message = "Mã nhân viên đã tồn tại" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var nguoiDung = new NguoiDung
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                VaiTro = 1,
                HieuLuc = true,
                NgayTao = DateTime.Now
            };
            _context.NguoiDungs.Add(nguoiDung);
            await _context.SaveChangesAsync();

            var nhanVien = new NhanVien
            {
                MaNV = dto.MaNV,
                UserId = nguoiDung.Id,
                HoTen = dto.HoTen,
                Email = dto.Email,
                DienThoai = dto.DienThoai
            };
            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _kafkaProducer.ProduceAsync("employee.created", new
            {
                MaNV = dto.MaNV,
                HoTen = dto.HoTen,
                Email = dto.Email
            });

            return CreatedAtAction(nameof(GetAll), new { message = "Tạo nhân viên thành công", maNV = dto.MaNV });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>Khóa/mở khóa tài khoản nhân viên</summary>
    [HttpPut("{maNV}/toggle-lock")]
    public async Task<IActionResult> ToggleLock(string maNV)
    {
        var nv = await _context.NhanViens
            .Include(n => n.NguoiDung)
            .FirstOrDefaultAsync(n => n.MaNV == maNV);

        if (nv == null) return NotFound();

        nv.NguoiDung.HieuLuc = !nv.NguoiDung.HieuLuc;
        await _context.SaveChangesAsync();

        return Ok(new { message = nv.NguoiDung.HieuLuc ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản" });
    }

    /// <summary>Xóa nhân viên</summary>
    [HttpDelete("{maNV}")]
    public async Task<IActionResult> Delete(string maNV)
    {
        var nv = await _context.NhanViens
            .Include(n => n.NguoiDung)
            .FirstOrDefaultAsync(n => n.MaNV == maNV);

        if (nv == null) return NotFound();

        _context.NguoiDungs.Remove(nv.NguoiDung);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Xóa nhân viên thành công" });
    }
}
