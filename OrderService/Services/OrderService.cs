using Microsoft.EntityFrameworkCore;
using OrderService.DTOs;
using OrderService.Kafka;
using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services;

public interface IOrderService
{
	Task<(int MaHD, string Message)> CreateOrderAsync(string maKH, string? hoTenClaim, CreateOrderDto dto);
	Task<List<OrderResponseDto>> GetMyOrdersAsync(string maKH, int? trangThai);
	Task<OrderResponseDto?> GetByIdAsync(int maHD, string? maKH, bool isAdmin);
	Task<(int Total, int Page, int Size, List<OrderResponseDto> Items)> GetAllAsync(string? maKH, int? trangThai, DateTime? tuNgay, DateTime? denNgay, int page, int size);
	Task<(bool Success, string Message)> UpdateStatusAsync(int maHD, UpdateOrderStatusDto dto);
	Task<(bool Success, string Message)> CancelOrderAsync(int maHD, string? maKH, bool isAdmin, string? lyDo);
	Task<object> GetStatisticsAsync(DateTime? tuNgay, DateTime? denNgay);
}

public class OrderService : IOrderService
{
	private readonly IOrderRepository _repository;
	private readonly IKafkaProducer _kafkaProducer;

	public OrderService(IOrderRepository repository, IKafkaProducer kafkaProducer)
	{
		_repository = repository;
		_kafkaProducer = kafkaProducer;
	}

	public async Task<(int MaHD, string Message)> CreateOrderAsync(string maKH, string? hoTenClaim, CreateOrderDto dto)
	{
		var hoaDon = new HoaDon
		{
			MaKH = maKH,
			NgayDat = DateTime.Now,
			NgayCan = dto.NgayCan,
			HoTen = dto.HoTen ?? hoTenClaim,
			DiaChi = dto.DiaChi,
			CachThanhToan = dto.CachThanhToan,
			CachVanChuyen = dto.CachVanChuyen,
			PhiVanChuyen = CalculateShippingFee(dto.CachVanChuyen),
			GhiChu = dto.GhiChu,
			TrangThai = 0
		};

		await _repository.AddHoaDonAsync(hoaDon);
		await _repository.SaveChangesAsync();

		foreach (var item in dto.Items)
		{
			await _repository.AddChiTietAsync(new ChiTietHD
			{
				MaHD = hoaDon.MaHD,
				MaHH = item.MaHH,
				TenHH = item.TenHH,
				DonGia = item.DonGia,
				SoLuong = item.SoLuong,
				GiamGia = item.GiamGia
			});
		}
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("order.created", new
		{
			MaHD = hoaDon.MaHD,
			MaKH = maKH,
			HoTen = hoaDon.HoTen,
			DiaChi = hoaDon.DiaChi,
			CachThanhToan = hoaDon.CachThanhToan,
			CachVanChuyen = hoaDon.CachVanChuyen,
			PhiVanChuyen = hoaDon.PhiVanChuyen,
			TongTien = dto.Items.Sum(i => i.DonGia * (1 - i.GiamGia / 100) * i.SoLuong) + hoaDon.PhiVanChuyen,
			Items = dto.Items.Select(i => new
			{
				i.MaHH,
				i.TenHH,
				i.SoLuong,
				i.DonGia,
				i.GiamGia
			}),
			NgayDat = hoaDon.NgayDat
		});

		return (hoaDon.MaHD, "Đặt hàng thành công");
	}

	public async Task<List<OrderResponseDto>> GetMyOrdersAsync(string maKH, int? trangThai)
	{
		var query = _repository.HoaDons
			.Include(h => h.ChiTietHDs)
			.Where(h => h.MaKH == maKH);

		if (trangThai.HasValue)
			query = query.Where(h => h.TrangThai == trangThai);

		var entities = await query
			.OrderByDescending(h => h.NgayDat)
			.ToListAsync();

		return entities.Select(MapOrderResponse).ToList();
	}

	public async Task<OrderResponseDto?> GetByIdAsync(int maHD, string? maKH, bool isAdmin)
	{
		var query = _repository.HoaDons
			.Include(h => h.ChiTietHDs)
			.Where(h => h.MaHD == maHD);

		if (!isAdmin && !string.IsNullOrEmpty(maKH))
			query = query.Where(h => h.MaKH == maKH);

		var hd = await query.FirstOrDefaultAsync();
		return hd == null ? null : MapOrderResponse(hd);
	}

	public async Task<(int Total, int Page, int Size, List<OrderResponseDto> Items)> GetAllAsync(
		string? maKH,
		int? trangThai,
		DateTime? tuNgay,
		DateTime? denNgay,
		int page,
		int size)
	{
		var query = _repository.HoaDons.Include(h => h.ChiTietHDs).AsQueryable();

		if (!string.IsNullOrEmpty(maKH))
			query = query.Where(h => h.MaKH == maKH);
		if (trangThai.HasValue)
			query = query.Where(h => h.TrangThai == trangThai);
		if (tuNgay.HasValue)
			query = query.Where(h => h.NgayDat >= tuNgay);
		if (denNgay.HasValue)
			query = query.Where(h => h.NgayDat <= denNgay);

		var total = await query.CountAsync();
		var entities = await query
			.OrderByDescending(h => h.NgayDat)
			.Skip((page - 1) * size)
			.Take(size)
			.ToListAsync();

		var items = entities.Select(MapOrderResponse).ToList();

		return (total, page, size, items);
	}

	public async Task<(bool Success, string Message)> UpdateStatusAsync(int maHD, UpdateOrderStatusDto dto)
	{
		var hd = await _repository.FindHoaDonByIdAsync(maHD);
		if (hd == null) return (false, "Không tìm thấy đơn hàng");

		if (hd.TrangThai == 4)
			return (false, "Không thể cập nhật đơn hàng đã hủy");

		hd.TrangThai = dto.TrangThai;
		if (!string.IsNullOrEmpty(dto.MaNV)) hd.MaNV = dto.MaNV;
		if (dto.NgayGiao.HasValue) hd.NgayGiao = dto.NgayGiao;

		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("order.updated", new
		{
			MaHD = hd.MaHD,
			MaKH = hd.MaKH,
			TrangThai = hd.TrangThai,
			MaNV = hd.MaNV,
			NgayGiao = hd.NgayGiao,
			UpdatedAt = DateTime.UtcNow
		});

		return (true, "Cập nhật trạng thái thành công");
	}

	public async Task<(bool Success, string Message)> CancelOrderAsync(int maHD, string? maKH, bool isAdmin, string? lyDo)
	{
		var query = _repository.HoaDons.Where(h => h.MaHD == maHD);
		if (!isAdmin && !string.IsNullOrEmpty(maKH))
			query = query.Where(h => h.MaKH == maKH);

		var hd = await query.FirstOrDefaultAsync();
		if (hd == null) return (false, "Không tìm thấy đơn hàng");

		if (!isAdmin && hd.TrangThai != 0)
			return (false, "Chỉ có thể hủy đơn hàng đang chờ xác nhận");

		hd.TrangThai = 4;
		hd.GhiChu = !string.IsNullOrEmpty(lyDo) ? $"Hủy đơn: {lyDo}" : "Đơn hàng đã bị hủy";
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("order.cancelled", new
		{
			MaHD = hd.MaHD,
			MaKH = hd.MaKH,
			LyDo = lyDo,
			CancelledAt = DateTime.UtcNow
		});

		return (true, "Hủy đơn hàng thành công");
	}

	public async Task<object> GetStatisticsAsync(DateTime? tuNgay, DateTime? denNgay)
	{
		var query = _repository.HoaDons.Include(h => h.ChiTietHDs).AsQueryable();

		if (tuNgay.HasValue) query = query.Where(h => h.NgayDat >= tuNgay);
		if (denNgay.HasValue) query = query.Where(h => h.NgayDat <= denNgay);

		var orders = await query.ToListAsync();

		return new
		{
			TongDonHang = orders.Count,
			DonChoXacNhan = orders.Count(h => h.TrangThai == 0),
			DonDaXacNhan = orders.Count(h => h.TrangThai == 1),
			DangGiaoHang = orders.Count(h => h.TrangThai == 2),
			DaGiaoHang = orders.Count(h => h.TrangThai == 3),
			DaHuy = orders.Count(h => h.TrangThai == 4),
			DoanhThu = orders
				.Where(h => h.TrangThai == 3)
				.Sum(h => h.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong) + h.PhiVanChuyen)
		};
	}

	private static OrderResponseDto MapOrderResponse(HoaDon h) => new()
	{
		MaHD = h.MaHD,
		MaKH = h.MaKH,
		NgayDat = h.NgayDat,
		NgayCan = h.NgayCan,
		NgayGiao = h.NgayGiao,
		HoTen = h.HoTen,
		DiaChi = h.DiaChi,
		CachThanhToan = h.CachThanhToan,
		CachVanChuyen = h.CachVanChuyen,
		PhiVanChuyen = h.PhiVanChuyen,
		MaNV = h.MaNV,
		GhiChu = h.GhiChu,
		TrangThai = h.TrangThai,
		ChiTiet = h.ChiTietHDs.Select(ct => new ChiTietHDDto
		{
			MaCT = ct.MaCT,
			MaHH = ct.MaHH,
			TenHH = ct.TenHH,
			SoLuong = ct.SoLuong,
			DonGia = ct.DonGia,
			GiamGia = ct.GiamGia
		}).ToList()
	};

	private static decimal CalculateShippingFee(string cachVanChuyen) => cachVanChuyen switch
	{
		"Giao hàng nhanh" => 30000,
		"Giao hàng hỏa tốc" => 60000,
		"Giao hàng tiết kiệm" => 15000,
		_ => 20000
	};
}
