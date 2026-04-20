using Microsoft.EntityFrameworkCore;
using UserService.DTOs;
using UserService.Kafka;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public interface IUserManagementService
{
    Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);

    Task<object?> GetCustomerProfileAsync(string maKH);
    Task<bool> UpdateCustomerProfileAsync(string maKH, UpdateKhachHangDto dto);
    Task<object> GetCustomersAsync(int page, int size, string? search);
    Task<object?> GetCustomerByIdAsync(string maKH);
    Task<bool?> ToggleCustomerLockAsync(string maKH);

    Task<List<object>> GetEmployeesAsync(string? search);
    Task<(bool Success, string Message)> CreateEmployeeAsync(CreateNhanVienDto dto);
    Task<bool?> ToggleEmployeeLockAsync(string maNV);
    Task<bool> DeleteEmployeeAsync(string maNV);
}

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _repository;
    private readonly IJwtService _jwtService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IUserRepository repository,
        IJwtService jwtService,
        IKafkaProducer kafkaProducer,
        IEmailService emailService,
        ILogger<UserManagementService> logger)
    {
        _repository = repository;
        _jwtService = jwtService;
        _kafkaProducer = kafkaProducer;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto)
    {
        if (await _repository.NguoiDungs.AnyAsync(u => u.TenDangNhap == dto.TenDangNhap))
            return (false, "Tên đăng nhập đã tồn tại", null);

        if (await _repository.KhachHangs.AnyAsync(k => k.Email == dto.Email))
            return (false, "Email đã được sử dụng", null);

        await using var transaction = await _repository.BeginTransactionAsync();
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
            await _repository.AddUserAsync(nguoiDung);
            await _repository.SaveChangesAsync();

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
            await _repository.AddCustomerAsync(khachHang);
            await _repository.SaveChangesAsync();

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

            return (true, "Đăng ký thành công", new AuthResponseDto
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

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _repository.NguoiDungs
            .Include(u => u.KhachHang)
            .Include(u => u.NhanVien)
            .FirstOrDefaultAsync(u => u.TenDangNhap == dto.TenDangNhap);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau) || !user.HieuLuc)
            return null;

        var hoTen = user.KhachHang?.HoTen ?? user.NhanVien?.HoTen ?? user.TenDangNhap;
        var token = _jwtService.GenerateToken(user, user.KhachHang?.MaKH, user.NhanVien?.MaNV, hoTen);

        return new AuthResponseDto
        {
            Token = token,
            TenDangNhap = user.TenDangNhap,
            VaiTro = user.VaiTro,
            MaKH = user.KhachHang?.MaKH,
            MaNV = user.NhanVien?.MaNV,
            HoTen = hoTen,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var khachHang = await _repository.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.Email == dto.Email);

        if (khachHang == null)
            return;

        var resetToken = Guid.NewGuid().ToString("N");
        khachHang.NguoiDung.RandomKey = resetToken;
        await _repository.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordResetEmailAsync(khachHang.Email, resetToken, khachHang.HoTen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _repository.NguoiDungs.FirstOrDefaultAsync(u => u.RandomKey == dto.Token);
        if (user == null) return false;

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
        user.RandomKey = null;
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _repository.FindUserByIdAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.MatKhauCu, user.MatKhau))
            return false;

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<object?> GetCustomerProfileAsync(string maKH)
    {
        var kh = await _repository.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.MaKH == maKH);

        if (kh == null) return null;

        return new
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
        };
    }

    public async Task<bool> UpdateCustomerProfileAsync(string maKH, UpdateKhachHangDto dto)
    {
        var kh = await _repository.KhachHangs.FirstOrDefaultAsync(k => k.MaKH == maKH);
        if (kh == null) return false;

        if (dto.HoTen != null) kh.HoTen = dto.HoTen;
        if (dto.GioiTinh.HasValue) kh.GioiTinh = dto.GioiTinh.Value;
        if (dto.NgaySinh.HasValue) kh.NgaySinh = dto.NgaySinh.Value;
        if (dto.DiaChi != null) kh.DiaChi = dto.DiaChi;
        if (dto.DienThoai != null) kh.DienThoai = dto.DienThoai;
        if (dto.Hinh != null) kh.Hinh = dto.Hinh;

        await _repository.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("customer.updated", new
        {
            MaKH = kh.MaKH,
            HoTen = kh.HoTen,
            DiaChi = kh.DiaChi,
            DienThoai = kh.DienThoai,
            UpdatedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<object> GetCustomersAsync(int page, int size, string? search)
    {
        var query = _repository.KhachHangs.AsQueryable();

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

        return new { total, page, size, items };
    }

    public async Task<object?> GetCustomerByIdAsync(string maKH)
    {
        var kh = await _repository.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.MaKH == maKH);

        if (kh == null) return null;

        return new
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
        };
    }

    public async Task<bool?> ToggleCustomerLockAsync(string maKH)
    {
        var kh = await _repository.KhachHangs
            .Include(k => k.NguoiDung)
            .FirstOrDefaultAsync(k => k.MaKH == maKH);

        if (kh == null) return null;

        kh.NguoiDung.HieuLuc = !kh.NguoiDung.HieuLuc;
        await _repository.SaveChangesAsync();
        return kh.NguoiDung.HieuLuc;
    }

    public async Task<List<object>> GetEmployeesAsync(string? search)
    {
        var query = _repository.NhanViens.Include(nv => nv.NguoiDung).AsQueryable();

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

        return items.Cast<object>().ToList();
    }

    public async Task<(bool Success, string Message)> CreateEmployeeAsync(CreateNhanVienDto dto)
    {
        if (await _repository.NguoiDungs.AnyAsync(u => u.TenDangNhap == dto.TenDangNhap))
            return (false, "Tên đăng nhập đã tồn tại");

        if (await _repository.NhanViens.AnyAsync(nv => nv.MaNV == dto.MaNV))
            return (false, "Mã nhân viên đã tồn tại");

        await using var transaction = await _repository.BeginTransactionAsync();
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
            await _repository.AddUserAsync(nguoiDung);
            await _repository.SaveChangesAsync();

            var nhanVien = new NhanVien
            {
                MaNV = dto.MaNV,
                UserId = nguoiDung.Id,
                HoTen = dto.HoTen,
                Email = dto.Email,
                DienThoai = dto.DienThoai
            };
            await _repository.AddEmployeeAsync(nhanVien);
            await _repository.SaveChangesAsync();

            await transaction.CommitAsync();

            await _kafkaProducer.ProduceAsync("employee.created", new
            {
                MaNV = dto.MaNV,
                HoTen = dto.HoTen,
                Email = dto.Email
            });

            return (true, "Tạo nhân viên thành công");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool?> ToggleEmployeeLockAsync(string maNV)
    {
        var nv = await _repository.NhanViens
            .Include(n => n.NguoiDung)
            .FirstOrDefaultAsync(n => n.MaNV == maNV);

        if (nv == null) return null;

        nv.NguoiDung.HieuLuc = !nv.NguoiDung.HieuLuc;
        await _repository.SaveChangesAsync();
        return nv.NguoiDung.HieuLuc;
    }

    public async Task<bool> DeleteEmployeeAsync(string maNV)
    {
        var nv = await _repository.NhanViens
            .Include(n => n.NguoiDung)
            .FirstOrDefaultAsync(n => n.MaNV == maNV);

        if (nv == null) return false;

        _repository.RemoveUser(nv.NguoiDung);
        await _repository.SaveChangesAsync();
        return true;
    }
}
