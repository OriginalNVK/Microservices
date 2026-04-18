using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string MatKhau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    public bool GioiTinh { get; set; }

    [Required]
    public DateOnly NgaySinh { get; set; }

    [MaxLength(200)]
    public string? DiaChi { get; set; }

    [MaxLength(20)]
    public string? DienThoai { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public string MatKhau { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    [Required]
    public string MatKhauCu { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class UpdateKhachHangDto
{
    [MaxLength(100)]
    public string? HoTen { get; set; }

    public bool? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    [MaxLength(200)]
    public string? DiaChi { get; set; }

    [MaxLength(20)]
    public string? DienThoai { get; set; }

    [MaxLength(255)]
    public string? Hinh { get; set; }
}

public class CreateNhanVienDto
{
    [Required]
    [MaxLength(50)]
    public string MaNV { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string MatKhau { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DienThoai { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public int VaiTro { get; set; }
    public string? MaKH { get; set; }
    public string? MaNV { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
