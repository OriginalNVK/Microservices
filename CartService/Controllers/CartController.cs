using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CartService.DTOs;
using CartService.Services;

namespace CartService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Lấy giỏ hàng của khách hàng</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var cart = await _cartService.GetCartAsync(maKH);
        return Ok(cart);
    }

    /// <summary>Thêm sản phẩm vào giỏ hàng</summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var result = await _cartService.AddToCartAsync(maKH, dto);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    /// <summary>Cập nhật số lượng sản phẩm trong giỏ hàng</summary>
    [HttpPut("{maCart}")]
    public async Task<IActionResult> UpdateCart(int maCart, [FromBody] UpdateCartDto dto)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var updated = await _cartService.UpdateCartAsync(maKH, maCart, dto);
        if (!updated) return NotFound();

        return Ok(new { message = "Cập nhật giỏ hàng thành công" });
    }

    /// <summary>Xóa sản phẩm khỏi giỏ hàng</summary>
    [HttpDelete("{maCart}")]
    public async Task<IActionResult> RemoveFromCart(int maCart)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var removed = await _cartService.RemoveFromCartAsync(maKH, maCart);
        if (!removed) return NotFound();

        return Ok(new { message = "Đã xóa khỏi giỏ hàng" });
    }

    /// <summary>Xóa toàn bộ giỏ hàng</summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        await _cartService.ClearCartAsync(maKH);

        return Ok(new { message = "Đã xóa toàn bộ giỏ hàng" });
    }

    /// <summary>Lấy số lượng sản phẩm trong giỏ</summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCartCount()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var count = await _cartService.GetCartCountAsync(maKH);

        return Ok(new { count });
    }
}
