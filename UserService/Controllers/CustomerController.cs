using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public KhachHangController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>Lấy thông tin khách hàng hiện tại</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var kh = await _userManagementService.GetCustomerProfileAsync(maKH);

        if (kh == null) return NotFound();

        return Ok(kh);
    }

    /// <summary>Cập nhật thông tin khách hàng</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateKhachHangDto dto)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var updated = await _userManagementService.UpdateCustomerProfileAsync(maKH, dto);
        if (!updated) return NotFound();

        return Ok(new { message = "Cập nhật thông tin thành công" });
    }

    /// <summary>Lấy danh sách tất cả khách hàng (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? search = null)
    {
        var data = await _userManagementService.GetCustomersAsync(page, size, search);
        return Ok(data);
    }

    /// <summary>Lấy thông tin khách hàng theo mã (Admin)</summary>
    [HttpGet("{maKH}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetById(string maKH)
    {
        var kh = await _userManagementService.GetCustomerByIdAsync(maKH);

        if (kh == null) return NotFound();

        return Ok(kh);
    }

    /// <summary>Khóa/mở khóa tài khoản (Admin)</summary>
    [HttpPut("{maKH}/toggle-lock")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> ToggleLock(string maKH)
    {
        var enabled = await _userManagementService.ToggleCustomerLockAsync(maKH);
        if (!enabled.HasValue) return NotFound();

        return Ok(new { message = enabled.Value ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản" });
    }
}
