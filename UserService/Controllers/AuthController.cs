using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Kafka;
using UserService.Models;
using UserService.Services;
using BCrypt.Net;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserDbContext context,
        IJwtService jwtService,
        IKafkaProducer kafkaProducer,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _kafkaProducer = kafkaProducer;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>Đăng ký tài khoản khách hàng mới</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == dto.TenDangNhap))
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });

        if (await _context.KhachHangs.AnyAsync(k => k.Email == dto.Email))
            return BadRequest(new { message = "Email đã được sử dụng" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var nguoiDung = new NguoiDung
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                VaiTro = 0,
                HieuLuc = true,
                NgayTao = DateTime.Now
            };
            _context.NguoiDungs.Add(nguoiDung);
            await _context.SaveChangesAsync();

            var maKH = $"KH{nguoiDung.Id:D5}";
            var khachHang = new KhachHang
            {
                MaKH = maKH,
                UserId = nguoiDung.Id,
                HoTen = dto.HoTen,
                GioiTinh = dto.GioiTinh,
                NgaySinh = dto.NgaySinh,
                DiaChi = dto.DiaChi,
                DienThoai = dto.DienThoai,
                Email = dto.Email
            };
            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _kafkaProducer.ProduceAsync("user.registered", new
            {
                UserId = nguoiDung.Id,
                TenDangNhap = nguoiDung.TenDangNhap,
                VaiTro = nguoiDung.VaiTro,
                NgayTao = nguoiDung.NgayTao
            });

            await _kafkaProducer.ProduceAsync("customer.created", new
            {
                MaKH = maKH,
                UserId = nguoiDung.Id,
                HoTen = dto.HoTen,
                Email = dto.Email,
                DienThoai = dto.DienThoai,
                DiaChi = dto.DiaChi,
                CreatedAt = DateTime.UtcNow
            });

            var token = _jwtService.GenerateToken(nguoiDung, maKH, null, dto.HoTen);

            return Ok(new AuthResponseDto
            {
                Token = token,
                TenDangNhap = nguoiDung.TenDangNhap,
                VaiTro = nguoiDung.VaiTro,
                MaKH = maKH,
                HoTen = dto.HoTen,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>Đăng nhập</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.NguoiDungs
            .Include(u => u.KhachHang)
            .Include(u => u.NhanVien)
            .FirstOrDefaultAsync(u => u.TenDangNhap == dto.TenDangNhap);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau))
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng" });

        if (!user.HieuLuc)
            return Unauthorized(new { message = "Tài khoản đã bị khóa" });

        var hoTen = user.KhachHang?.HoTen ?? user.NhanVien?.HoTen ?? user.TenDangNhap;
        var token = _jwtService.GenerateToken(user, user.KhachHang?.MaKH, user.NhanVien?.MaNV, hoTen);

        return Ok(new AuthResponseDto
        {
            Token = token,
            TenDangNhap = user.TenDangNhap,
            VaiTro = user.VaiTro,
            MaKH = user.KhachHang?.MaKH,
            MaNV = user.NhanVien?.MaNV,
            HoTen = hoTen,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
    }

    /// <summary>Quên mật khẩu - gửi email reset</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var khachHang = await _context.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.Email == dto.Email);

        if (khachHang == null)
            return Ok(new { message = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu" });

        var resetToken = Guid.NewGuid().ToString("N");
        khachHang.NguoiDung.RandomKey = resetToken;
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordResetEmailAsync(khachHang.Email, resetToken, khachHang.HoTen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
        }

        return Ok(new { message = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu" });
    }

    /// <summary>Đặt lại mật khẩu với token</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _context.NguoiDungs
            .FirstOrDefaultAsync(u => u.RandomKey == dto.Token);

        if (user == null)
            return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn" });

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
        user.RandomKey = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Mật khẩu đã được đặt lại thành công" });
    }

    /// <summary>Đổi mật khẩu (cần đăng nhập)</summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.NguoiDungs.FindAsync(userId);

        if (user == null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.MatKhauCu, user.MatKhau))
            return BadRequest(new { message = "Mật khẩu cũ không đúng" });

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công" });
    }
}
