using InvoiceService.Models;
using InvoiceService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public interface IInvoiceService
{
	Task<IEnumerable<object>> GetMyInvoicesAsync(string maKH, int? trangThai);
	Task<object?> GetByIdAsync(int maHD, string? maKH, bool isAdmin);
	Task<HoaDon?> GetInvoiceEntityAsync(int maHD, string? maKH, bool isAdmin);
	Task<object> GetAllAsync(string? maKH, int? trangThai, DateTime? tuNgay, DateTime? denNgay, int page, int size);
	Task<List<HoaDon>> GetOrdersInRangeAsync(DateTime from, DateTime to, bool onlyDelivered = false);
	Task<object> GetRevenueReportAsync(DateTime from, DateTime to);
	Task<List<object>> GetTopProductsReportAsync(DateTime from, DateTime to, int top);
}

public class InvoiceService : IInvoiceService
{
	private readonly IInvoiceRepository _repository;

	public InvoiceService(IInvoiceRepository repository)
	{
		_repository = repository;
	}

	public async Task<IEnumerable<object>> GetMyInvoicesAsync(string maKH, int? trangThai)
	{
		var query = _repository.HoaDons
			.Include(h => h.ChiTietHDs)
			.Where(h => h.MaKH == maKH);

		if (trangThai.HasValue)
			query = query.Where(h => h.TrangThai == trangThai);

		var invoices = await query.OrderByDescending(h => h.NgayDat).ToListAsync();

		return invoices.Select(h => (object)new
		{
			h.MaHD,
			h.MaKH,
			h.NgayDat,
			h.HoTen,
			h.DiaChi,
			h.CachThanhToan,
			h.CachVanChuyen,
			h.PhiVanChuyen,
			h.TrangThai,
			TrangThaiText = GetTrangThaiText(h.TrangThai),
			SoMat = h.ChiTietHDs.Sum(ct => ct.SoLuong),
			TongTien = h.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong) + h.PhiVanChuyen
		});
	}

	public async Task<object?> GetByIdAsync(int maHD, string? maKH, bool isAdmin)
	{
		var hd = await GetInvoiceEntityAsync(maHD, maKH, isAdmin);
		if (hd == null) return null;

		var tongTienHang = hd.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong);

		return new
		{
			hd.MaHD,
			hd.MaKH,
			hd.HoTenKH,
			hd.EmailKH,
			hd.DienThoaiKH,
			hd.NgayDat,
			hd.NgayCan,
			hd.NgayGiao,
			hd.HoTen,
			hd.DiaChi,
			hd.CachThanhToan,
			hd.CachVanChuyen,
			hd.PhiVanChuyen,
			hd.MaNV,
			hd.GhiChu,
			hd.TrangThai,
			TrangThaiText = GetTrangThaiText(hd.TrangThai),
			ChiTiet = hd.ChiTietHDs.Select(ct => new
			{
				ct.MaCT,
				ct.MaHH,
				ct.TenHH,
				ct.DonGia,
				ct.SoLuong,
				ct.GiamGia,
				GiaSauGiam = ct.DonGia * (1 - ct.GiamGia / 100),
				ThanhTien = ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong
			}),
			TongTienHang = tongTienHang,
			TongCong = tongTienHang + hd.PhiVanChuyen
		};
	}

	public async Task<HoaDon?> GetInvoiceEntityAsync(int maHD, string? maKH, bool isAdmin)
	{
		var query = _repository.HoaDons.Include(h => h.ChiTietHDs).Where(h => h.MaHD == maHD);

		if (!isAdmin && !string.IsNullOrEmpty(maKH))
			query = query.Where(h => h.MaKH == maKH);

		return await query.FirstOrDefaultAsync();
	}

	public async Task<object> GetAllAsync(string? maKH, int? trangThai, DateTime? tuNgay, DateTime? denNgay, int page, int size)
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
		var items = await query
			.OrderByDescending(h => h.NgayDat)
			.Skip((page - 1) * size)
			.Take(size)
			.ToListAsync();

		return new
		{
			total,
			page,
			size,
			items = items.Select(h => new
			{
				h.MaHD,
				h.MaKH,
				h.NgayDat,
				h.HoTen,
				h.DiaChi,
				h.CachThanhToan,
				h.CachVanChuyen,
				h.PhiVanChuyen,
				h.TrangThai,
				TrangThaiText = GetTrangThaiText(h.TrangThai),
				TongTien = h.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong) + h.PhiVanChuyen
			})
		};
	}

	public Task<List<HoaDon>> GetOrdersInRangeAsync(DateTime from, DateTime to, bool onlyDelivered = false)
	{
		var query = _repository.HoaDons
			.Include(h => h.ChiTietHDs)
			.Where(h => h.NgayDat >= from && h.NgayDat <= to);

		if (onlyDelivered)
			query = query.Where(h => h.TrangThai == 3);

		return query.OrderByDescending(h => h.NgayDat).ToListAsync();
	}

	public async Task<object> GetRevenueReportAsync(DateTime from, DateTime to)
	{
		var orders = await GetOrdersInRangeAsync(from, to, onlyDelivered: true);

		var report = orders
			.GroupBy(o => o.NgayDat.Date)
			.Select(g => new
			{
				Ngay = g.Key.ToString("dd/MM/yyyy"),
				SoDon = g.Count(),
				TongTienHang = g.Sum(o => o.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong)),
				PhiVanChuyen = g.Sum(o => o.PhiVanChuyen),
				DoanhThu = g.Sum(o => o.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong) + o.PhiVanChuyen)
			})
			.OrderBy(r => r.Ngay)
			.ToList();

		return new
		{
			TuNgay = from.ToString("dd/MM/yyyy"),
			DenNgay = to.ToString("dd/MM/yyyy"),
			TongDoanhThu = report.Sum(r => r.DoanhThu),
			TongDon = report.Sum(r => r.SoDon),
			ChiTiet = report
		};
	}

	public async Task<List<object>> GetTopProductsReportAsync(DateTime from, DateTime to, int top)
	{
		var chiTiet = await _repository.ChiTietHDs
			.Include(ct => ct.HoaDon)
			.Where(ct => ct.HoaDon.NgayDat >= from && ct.HoaDon.NgayDat <= to && ct.HoaDon.TrangThai == 3)
			.ToListAsync();

		var report = chiTiet
			.GroupBy(ct => new { ct.MaHH, ct.TenHH })
			.Select(g => new
			{
				MaHH = g.Key.MaHH,
				TenHH = g.Key.TenHH,
				TongSoLuong = g.Sum(ct => ct.SoLuong),
				DoanhThu = g.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong)
			})
			.OrderByDescending(r => r.TongSoLuong)
			.Take(top)
			.ToList();

		return report.Cast<object>().ToList();
	}

	private static string GetTrangThaiText(int trangThai) => trangThai switch
	{
		0 => "Chờ xác nhận",
		1 => "Đã xác nhận",
		2 => "Đang giao hàng",
		3 => "Đã giao hàng",
		4 => "Đã hủy",
		_ => "Không xác định"
	};
}
