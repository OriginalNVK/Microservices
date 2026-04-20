using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "1")]
public class NhanVienController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public NhanVienController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>Lấy danh sách nhân viên</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var items = await _userManagementService.GetEmployeesAsync(search);

        return Ok(items);
    }

    /// <summary>Thêm nhân viên mới</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNhanVienDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userManagementService.CreateEmployeeAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return CreatedAtAction(nameof(GetAll), new { message = result.Message, maNV = dto.MaNV });
    }

    /// <summary>Khóa/mở khóa tài khoản nhân viên</summary>
    [HttpPut("{maNV}/toggle-lock")]
    public async Task<IActionResult> ToggleLock(string maNV)
    {
        var enabled = await _userManagementService.ToggleEmployeeLockAsync(maNV);
        if (!enabled.HasValue) return NotFound();

        return Ok(new { message = enabled.Value ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản" });
    }

    /// <summary>Xóa nhân viên</summary>
    [HttpDelete("{maNV}")]
    public async Task<IActionResult> Delete(string maNV)
    {
        var deleted = await _userManagementService.DeleteEmployeeAsync(maNV);
        if (!deleted) return NotFound();

        return Ok(new { message = "Xóa nhân viên thành công" });
    }
}
