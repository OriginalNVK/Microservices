using Microsoft.EntityFrameworkCore;
using ProductService.DTOs;
using ProductService.Kafka;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Services;

public class ProductQueryFilter
{
	public int? MaLoai { get; set; }
	public string? Search { get; set; }
	public decimal? MinGia { get; set; }
	public decimal? MaxGia { get; set; }
	public string? SortBy { get; set; } = "tenHH";
	public bool Ascending { get; set; } = true;
	public int Page { get; set; } = 1;
	public int Size { get; set; } = 12;
}

public interface IProductService
{
	Task<object> GetAllProductsAsync(ProductQueryFilter filter);
	Task<HangHoaResponseDto?> GetProductByIdAsync(int maHH);
	Task<(bool Success, string Message, int MaHH)> CreateProductAsync(CreateHangHoaDto dto);
	Task<(bool Success, string Message)> UpdateProductAsync(int maHH, UpdateHangHoaDto dto);
	Task<bool> DeleteProductAsync(int maHH);
	Task<List<HangHoaResponseDto>> GetBestSellersAsync(int top);
	Task<List<HangHoaResponseDto>> GetOnSaleAsync();
	Task<List<object>> GetAllCategoriesAsync();
	Task<object?> GetCategoryByIdAsync(int maLoai);
	Task<Loai> CreateCategoryAsync(CreateLoaiDto dto);
	Task<Loai?> UpdateCategoryAsync(int maLoai, UpdateLoaiDto dto);
	Task<(bool Success, string Message)> DeleteCategoryAsync(int maLoai);
}

public class ProductService : IProductService
{
	private readonly IProductRepository _repository;
	private readonly IKafkaProducer _kafkaProducer;

	public ProductService(IProductRepository repository, IKafkaProducer kafkaProducer)
	{
		_repository = repository;
		_kafkaProducer = kafkaProducer;
	}

	public async Task<object> GetAllProductsAsync(ProductQueryFilter filter)
	{
		var query = _repository.HangHoas.Include(h => h.Loai).AsQueryable();

		if (filter.MaLoai.HasValue)
			query = query.Where(h => h.MaLoai == filter.MaLoai);
		if (!string.IsNullOrEmpty(filter.Search))
			query = query.Where(h => h.TenHH.Contains(filter.Search) || (h.MoTa != null && h.MoTa.Contains(filter.Search)));
		if (filter.MinGia.HasValue)
			query = query.Where(h => h.DonGia >= filter.MinGia);
		if (filter.MaxGia.HasValue)
			query = query.Where(h => h.DonGia <= filter.MaxGia);

		query = filter.SortBy?.ToLower() switch
		{
			"dongia" => filter.Ascending ? query.OrderBy(h => h.DonGia) : query.OrderByDescending(h => h.DonGia),
			"luotmua" => filter.Ascending ? query.OrderBy(h => h.LuotMua) : query.OrderByDescending(h => h.LuotMua),
			"giamgia" => filter.Ascending ? query.OrderBy(h => h.GiamGia) : query.OrderByDescending(h => h.GiamGia),
			_ => filter.Ascending ? query.OrderBy(h => h.TenHH) : query.OrderByDescending(h => h.TenHH)
		};

		var total = await query.CountAsync();
		var entities = await query
			.Skip((filter.Page - 1) * filter.Size)
			.Take(filter.Size)
			.ToListAsync();

		var items = entities.Select(MapHangHoaResponse).ToList();

		return new { total, page = filter.Page, size = filter.Size, items };
	}

	public Task<HangHoaResponseDto?> GetProductByIdAsync(int maHH) =>
		_repository.HangHoas
			.Include(h => h.Loai)
			.Where(h => h.MaHH == maHH)
			.Select(h => new HangHoaResponseDto
			{
				MaHH = h.MaHH,
				TenHH = h.TenHH,
				TenAlias = h.TenAlias,
				MaLoai = h.MaLoai,
				TenLoai = h.Loai.TenLoai,
				MoTaDonVi = h.MoTaDonVi,
				DonGia = h.DonGia,
				Hinh = h.Hinh,
				NgaySX = h.NgaySX,
				GiamGia = h.GiamGia,
				LuotMua = h.LuotMua,
				MoTa = h.MoTa
			})
			.FirstOrDefaultAsync();

	public async Task<(bool Success, string Message, int MaHH)> CreateProductAsync(CreateHangHoaDto dto)
	{
		if (!await _repository.Loais.AnyAsync(l => l.MaLoai == dto.MaLoai))
			return (false, "Loại hàng hóa không tồn tại", 0);

		var hh = new HangHoa
		{
			TenHH = dto.TenHH,
			TenAlias = dto.TenAlias ?? dto.TenHH.ToLower().Replace(" ", "-"),
			MaLoai = dto.MaLoai,
			MoTaDonVi = dto.MoTaDonVi,
			DonGia = dto.DonGia,
			Hinh = dto.Hinh,
			NgaySX = dto.NgaySX,
			GiamGia = dto.GiamGia,
			LuotMua = 0,
			MoTa = dto.MoTa
		};

		await _repository.AddHangHoaAsync(hh);
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("product.created", new
		{
			MaHH = hh.MaHH,
			TenHH = hh.TenHH,
			DonGia = hh.DonGia,
			GiamGia = hh.GiamGia,
			Hinh = hh.Hinh,
			CreatedAt = DateTime.UtcNow
		});

		return (true, "Tạo hàng hóa thành công", hh.MaHH);
	}

	public async Task<(bool Success, string Message)> UpdateProductAsync(int maHH, UpdateHangHoaDto dto)
	{
		var hh = await _repository.FindHangHoaByIdAsync(maHH);
		if (hh == null) return (false, "Không tìm thấy hàng hóa");

		if (dto.MaLoai.HasValue && !await _repository.Loais.AnyAsync(l => l.MaLoai == dto.MaLoai))
			return (false, "Loại hàng hóa không tồn tại");

		if (dto.TenHH != null) hh.TenHH = dto.TenHH;
		if (dto.TenAlias != null) hh.TenAlias = dto.TenAlias;
		if (dto.MaLoai.HasValue) hh.MaLoai = dto.MaLoai.Value;
		if (dto.MoTaDonVi != null) hh.MoTaDonVi = dto.MoTaDonVi;
		if (dto.DonGia.HasValue) hh.DonGia = dto.DonGia;
		if (dto.Hinh != null) hh.Hinh = dto.Hinh;
		if (dto.NgaySX.HasValue) hh.NgaySX = dto.NgaySX.Value;
		if (dto.GiamGia.HasValue) hh.GiamGia = dto.GiamGia.Value;
		if (dto.MoTa != null) hh.MoTa = dto.MoTa;

		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("product.updated", new
		{
			MaHH = hh.MaHH,
			TenHH = hh.TenHH,
			DonGia = hh.DonGia,
			GiamGia = hh.GiamGia,
			Hinh = hh.Hinh,
			UpdatedAt = DateTime.UtcNow
		});

		return (true, "Cập nhật thành công");
	}

	public async Task<bool> DeleteProductAsync(int maHH)
	{
		var hh = await _repository.FindHangHoaByIdAsync(maHH);
		if (hh == null) return false;

		_repository.RemoveHangHoa(hh);
		await _repository.SaveChangesAsync();

		await _kafkaProducer.ProduceAsync("product.deleted", new
		{
			MaHH = maHH,
			DeletedAt = DateTime.UtcNow
		});

		return true;
	}

	public Task<List<HangHoaResponseDto>> GetBestSellersAsync(int top) =>
		_repository.HangHoas
			.Include(h => h.Loai)
			.OrderByDescending(h => h.LuotMua)
			.Take(top)
			.Select(h => new HangHoaResponseDto
			{
				MaHH = h.MaHH,
				TenHH = h.TenHH,
				TenAlias = h.TenAlias,
				MaLoai = h.MaLoai,
				TenLoai = h.Loai.TenLoai,
				MoTaDonVi = h.MoTaDonVi,
				DonGia = h.DonGia,
				Hinh = h.Hinh,
				NgaySX = h.NgaySX,
				GiamGia = h.GiamGia,
				LuotMua = h.LuotMua,
				MoTa = h.MoTa
			})
			.ToListAsync();

	public Task<List<HangHoaResponseDto>> GetOnSaleAsync() =>
		_repository.HangHoas
			.Include(h => h.Loai)
			.Where(h => h.GiamGia > 0)
			.OrderByDescending(h => h.GiamGia)
			.Select(h => new HangHoaResponseDto
			{
				MaHH = h.MaHH,
				TenHH = h.TenHH,
				TenAlias = h.TenAlias,
				MaLoai = h.MaLoai,
				TenLoai = h.Loai.TenLoai,
				MoTaDonVi = h.MoTaDonVi,
				DonGia = h.DonGia,
				Hinh = h.Hinh,
				NgaySX = h.NgaySX,
				GiamGia = h.GiamGia,
				LuotMua = h.LuotMua,
				MoTa = h.MoTa
			})
			.ToListAsync();

	public async Task<List<object>> GetAllCategoriesAsync()
	{
		var loais = await _repository.Loais
			.Select(l => new
			{
				l.MaLoai,
				l.TenLoai,
				l.TenLoaiAlias,
				l.MoTa,
				l.Hinh,
				SoHangHoa = l.HangHoas.Count
			})
			.ToListAsync();

		return loais.Cast<object>().ToList();
	}

	public async Task<object?> GetCategoryByIdAsync(int maLoai)
	{
		var loai = await _repository.Loais
			.Include(l => l.HangHoas)
			.FirstOrDefaultAsync(l => l.MaLoai == maLoai);

		if (loai == null) return null;

		return new
		{
			loai.MaLoai,
			loai.TenLoai,
			loai.TenLoaiAlias,
			loai.MoTa,
			loai.Hinh,
			SoHangHoa = loai.HangHoas.Count
		};
	}

	public async Task<Loai> CreateCategoryAsync(CreateLoaiDto dto)
	{
		var loai = new Loai
		{
			TenLoai = dto.TenLoai,
			TenLoaiAlias = dto.TenLoaiAlias ?? dto.TenLoai.ToLower().Replace(" ", "-"),
			MoTa = dto.MoTa,
			Hinh = dto.Hinh
		};

		await _repository.AddLoaiAsync(loai);
		await _repository.SaveChangesAsync();
		return loai;
	}

	public async Task<Loai?> UpdateCategoryAsync(int maLoai, UpdateLoaiDto dto)
	{
		var loai = await _repository.FindLoaiByIdAsync(maLoai);
		if (loai == null) return null;

		if (dto.TenLoai != null) loai.TenLoai = dto.TenLoai;
		if (dto.TenLoaiAlias != null) loai.TenLoaiAlias = dto.TenLoaiAlias;
		if (dto.MoTa != null) loai.MoTa = dto.MoTa;
		if (dto.Hinh != null) loai.Hinh = dto.Hinh;

		await _repository.SaveChangesAsync();
		return loai;
	}

	public async Task<(bool Success, string Message)> DeleteCategoryAsync(int maLoai)
	{
		var loai = await _repository.Loais.Include(l => l.HangHoas).FirstOrDefaultAsync(l => l.MaLoai == maLoai);
		if (loai == null) return (false, "Không tìm thấy loại hàng hóa");

		if (loai.HangHoas.Any())
			return (false, "Không thể xóa loại đang có hàng hóa");

		_repository.RemoveLoai(loai);
		await _repository.SaveChangesAsync();

		return (true, "Xóa loại thành công");
	}

	private static HangHoaResponseDto MapHangHoaResponse(HangHoa h) => new()
	{
		MaHH = h.MaHH,
		TenHH = h.TenHH,
		TenAlias = h.TenAlias,
		MaLoai = h.MaLoai,
		TenLoai = h.Loai.TenLoai,
		MoTaDonVi = h.MoTaDonVi,
		DonGia = h.DonGia,
		Hinh = h.Hinh,
		NgaySX = h.NgaySX,
		GiamGia = h.GiamGia,
		LuotMua = h.LuotMua,
		MoTa = h.MoTa
	};
}
