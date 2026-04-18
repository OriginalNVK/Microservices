using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CartService.Data;
using CartService.DTOs;
using CartService.Kafka;
using CartService.Models;

namespace CartService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly CartDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;

    public CartController(CartDbContext context, IKafkaProducer kafkaProducer)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
    }

    /// <summary>Lấy giỏ hàng của khách hàng</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var cartItems = await _context.Carts
            .Where(c => c.MaKH == maKH)
            .OrderByDescending(c => c.NgayThem)
            .ToListAsync();

        var maHHList = cartItems.Select(c => c.MaHH).ToList();
        var hangHoaCache = await _context.HangHoaCaches
            .Where(h => maHHList.Contains(h.MaHH))
            .ToDictionaryAsync(h => h.MaHH);

        var items = cartItems.Select(c =>
        {
            hangHoaCache.TryGetValue(c.MaHH, out var hh);
            return new CartItemResponseDto
            {
                MaCart = c.MaCart,
                MaHH = c.MaHH,
                TenHH = hh?.TenHH ?? $"Sản phẩm #{c.MaHH}",
                Hinh = hh?.Hinh,
                SoLuong = c.SoLuong,
                DonGia = c.DonGia,
                GiamGia = hh?.GiamGia ?? 0,
                NgayThem = c.NgayThem
            };
        }).ToList();

        return Ok(new CartSummaryDto
        {
            MaKH = maKH,
            Items = items
        });
    }

    /// <summary>Thêm sản phẩm vào giỏ hàng</summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var hangHoa = await _context.HangHoaCaches.FindAsync(dto.MaHH);
        if (hangHoa == null)
            return BadRequest(new { message = "Sản phẩm không tồn tại trong hệ thống" });

        var existing = await _context.Carts
            .FirstOrDefaultAsync(c => c.MaKH == maKH && c.MaHH == dto.MaHH);

        if (existing != null)
        {
            existing.SoLuong += dto.SoLuong;
            existing.DonGia = hangHoa.DonGia;
        }
        else
        {
            _context.Carts.Add(new Cart
            {
                MaKH = maKH,
                MaHH = dto.MaHH,
                SoLuong = dto.SoLuong,
                DonGia = hangHoa.DonGia,
                NgayThem = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("cart.updated", new
        {
            MaKH = maKH,
            MaHH = dto.MaHH,
            Action = "add",
            SoLuong = dto.SoLuong
        });

        return Ok(new { message = "Đã thêm vào giỏ hàng" });
    }

    /// <summary>Cập nhật số lượng sản phẩm trong giỏ hàng</summary>
    [HttpPut("{maCart}")]
    public async Task<IActionResult> UpdateCart(int maCart, [FromBody] UpdateCartDto dto)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var cartItem = await _context.Carts
            .FirstOrDefaultAsync(c => c.MaCart == maCart && c.MaKH == maKH);

        if (cartItem == null) return NotFound();

        cartItem.SoLuong = dto.SoLuong;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật giỏ hàng thành công" });
    }

    /// <summary>Xóa sản phẩm khỏi giỏ hàng</summary>
    [HttpDelete("{maCart}")]
    public async Task<IActionResult> RemoveFromCart(int maCart)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var cartItem = await _context.Carts
            .FirstOrDefaultAsync(c => c.MaCart == maCart && c.MaKH == maKH);

        if (cartItem == null) return NotFound();

        _context.Carts.Remove(cartItem);
        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("cart.updated", new
        {
            MaKH = maKH,
            MaHH = cartItem.MaHH,
            Action = "remove"
        });

        return Ok(new { message = "Đã xóa khỏi giỏ hàng" });
    }

    /// <summary>Xóa toàn bộ giỏ hàng</summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var items = await _context.Carts.Where(c => c.MaKH == maKH).ToListAsync();
        _context.Carts.RemoveRange(items);
        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("cart.cleared", new { MaKH = maKH });

        return Ok(new { message = "Đã xóa toàn bộ giỏ hàng" });
    }

    /// <summary>Lấy số lượng sản phẩm trong giỏ</summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCartCount()
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var count = await _context.Carts
            .Where(c => c.MaKH == maKH)
            .SumAsync(c => c.SoLuong);

        return Ok(new { count });
    }
}
