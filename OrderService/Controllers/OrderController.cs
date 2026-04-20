using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Tạo đơn hàng mới</summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        if (!dto.Items.Any())
            return BadRequest(new { message = "Đơn hàng phải có ít nhất 1 sản phẩm" });

        var result = await _orderService.CreateOrderAsync(maKH, User.FindFirst("HoTen")?.Value, dto);

        return CreatedAtAction(nameof(GetById), new { maHD = result.MaHD }, new
        {
            MaHD = result.MaHD,
            message = result.Message
        });
    }

    /// <summary>Lấy danh sách đơn hàng của khách hàng</summary>
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int? trangThai)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var orders = await _orderService.GetMyOrdersAsync(maKH, trangThai);

        return Ok(orders);
    }

    /// <summary>Lấy chi tiết đơn hàng</summary>
    [HttpGet("{maHD}")]
    public async Task<IActionResult> GetById(int maHD)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var vaiTro = User.FindFirst("VaiTro")?.Value;
        var isAdmin = vaiTro == "1";

        var hd = await _orderService.GetByIdAsync(maHD, maKH, isAdmin);
        if (hd == null) return NotFound();
        return Ok(hd);
    }

    /// <summary>Lấy tất cả đơn hàng (Admin/NV)</summary>
    [HttpGet]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? maKH,
        [FromQuery] int? trangThai,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var result = await _orderService.GetAllAsync(maKH, trangThai, tuNgay, denNgay, page, size);

        return Ok(new { total = result.Total, page = result.Page, size = result.Size, items = result.Items });
    }

    /// <summary>Cập nhật trạng thái đơn hàng (Admin/NV)</summary>
    [HttpPut("{maHD}/status")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateStatus(int maHD, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _orderService.UpdateStatusAsync(maHD, dto);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    /// <summary>Hủy đơn hàng (khách hàng chỉ hủy khi chờ xác nhận)</summary>
    [HttpPut("{maHD}/cancel")]
    public async Task<IActionResult> CancelOrder(int maHD, [FromBody] string? lyDo)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var isAdmin = User.FindFirst("VaiTro")?.Value == "1";

        var result = await _orderService.CancelOrderAsync(maHD, maKH, isAdmin, lyDo);
        if (result.Success) return Ok(new { message = result.Message });
        if (result.Message == "Không tìm thấy đơn hàng") return NotFound();
        return BadRequest(new { message = result.Message });
    }

    /// <summary>Thống kê đơn hàng (Admin)</summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        var statistics = await _orderService.GetStatisticsAsync(tuNgay, denNgay);
        return Ok(statistics);
    }
}
