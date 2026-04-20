using CartService.DTOs;
using CartService.Kafka;
using CartService.Models;
using CartService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CartService.Services;

public interface ICartService
{
	Task<CartSummaryDto> GetCartAsync(string maKH);
	Task<(bool Success, string Message)> AddToCartAsync(string maKH, AddToCartDto dto);
	Task<bool> UpdateCartAsync(string maKH, int maCart, UpdateCartDto dto);
	Task<bool> RemoveFromCartAsync(string maKH, int maCart);
	Task ClearCartAsync(string maKH);
	Task<int> GetCartCountAsync(string maKH);
}

public class CartService : ICartService
{
	private readonly ICartRepository _repository;
	private readonly IKafkaProducer _kafkaProducer;

	public CartService(ICartRepository repository, IKafkaProducer kafkaProducer)
	{
		_repository = repository;
		_kafkaProducer = kafkaProducer;
	}

	public async Task<CartSummaryDto> GetCartAsync(string maKH)
	{
		var cartItems = await _repository.Carts
			.Where(c => c.MaKH == maKH)
			.OrderByDescending(c => c.NgayThem)
			.ToListAsync();

		var maHHList = cartItems.Select(c => c.MaHH).ToList();
		var hangHoaCache = await _repository.HangHoaCaches
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

		return new CartSummaryDto
		{
			MaKH = maKH,
			Items = items
		};
	}

	public async Task<(bool Success, string Message)> AddToCartAsync(string maKH, AddToCartDto dto)
	{
		var hangHoa = await _repository.FindHangHoaCacheAsync(dto.MaHH);
		if (hangHoa == null)
			return (false, "Sản phẩm không tồn tại trong hệ thống");

		var existing = await _repository.FindCartByCustomerAndProductAsync(maKH, dto.MaHH);

		if (existing != null)
		{
			existing.SoLuong += dto.SoLuong;
			existing.DonGia = hangHoa.DonGia;
		}
		else
		{
			await _repository.AddCartAsync(new Cart
			{
				MaKH = maKH,
				MaHH = dto.MaHH,
				SoLuong = dto.SoLuong,
				DonGia = hangHoa.DonGia,
				NgayThem = DateTime.Now
			});
		}

		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("cart.updated", new
		{
			MaKH = maKH,
			MaHH = dto.MaHH,
			Action = "add",
			SoLuong = dto.SoLuong
		});

		return (true, "Đã thêm vào giỏ hàng");
	}

	public async Task<bool> UpdateCartAsync(string maKH, int maCart, UpdateCartDto dto)
	{
		var cartItem = await _repository.FindCartAsync(maCart, maKH);
		if (cartItem == null) return false;

		cartItem.SoLuong = dto.SoLuong;
		await _repository.SaveChangesAsync();
		return true;
	}

	public async Task<bool> RemoveFromCartAsync(string maKH, int maCart)
	{
		var cartItem = await _repository.FindCartAsync(maCart, maKH);
		if (cartItem == null) return false;

		_repository.RemoveCart(cartItem);
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("cart.updated", new
		{
			MaKH = maKH,
			MaHH = cartItem.MaHH,
			Action = "remove"
		});

		return true;
	}

	public async Task ClearCartAsync(string maKH)
	{
		var items = await _repository.Carts.Where(c => c.MaKH == maKH).ToListAsync();
		_repository.RemoveCarts(items);
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("cart.cleared", new { MaKH = maKH });
	}

	public Task<int> GetCartCountAsync(string maKH) =>
		_repository.Carts.Where(c => c.MaKH == maKH).SumAsync(c => c.SoLuong);
}
