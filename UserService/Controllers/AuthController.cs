using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public AuthController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>Đăng ký tài khoản khách hàng mới</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userManagementService.RegisterAsync(dto);
        if (!result.Success || result.Data == null)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>Đăng nhập</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var data = await _userManagementService.LoginAsync(dto);
        if (data == null)
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng" });

        return Ok(data);
    }

    /// <summary>Quên mật khẩu - gửi email reset</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _userManagementService.ForgotPasswordAsync(dto);

        return Ok(new { message = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu" });
    }

    /// <summary>Đặt lại mật khẩu với token</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var ok = await _userManagementService.ResetPasswordAsync(dto);
        if (!ok)
            return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn" });

        return Ok(new { message = "Mật khẩu đã được đặt lại thành công" });
    }

    /// <summary>Đổi mật khẩu (cần đăng nhập)</summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var ok = await _userManagementService.ChangePasswordAsync(userId, dto);
        if (!ok)
            return BadRequest(new { message = "Mật khẩu cũ không đúng" });

        return Ok(new { message = "Đổi mật khẩu thành công" });
    }
}
