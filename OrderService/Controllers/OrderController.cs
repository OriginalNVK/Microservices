using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Kafka;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;

    public OrderController(OrderDbContext context, IKafkaProducer kafkaProducer)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
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

        var hoaDon = new HoaDon
        {
            MaKH = maKH,
            NgayDat = DateTime.Now,
            NgayCan = dto.NgayCan,
            HoTen = dto.HoTen ?? User.FindFirst("HoTen")?.Value,
            DiaChi = dto.DiaChi,
            CachThanhToan = dto.CachThanhToan,
            CachVanChuyen = dto.CachVanChuyen,
            PhiVanChuyen = CalculateShippingFee(dto.CachVanChuyen),
            GhiChu = dto.GhiChu,
            TrangThai = 0
        };

        _context.HoaDons.Add(hoaDon);
        await _context.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            _context.ChiTietHDs.Add(new ChiTietHD
            {
                MaHD = hoaDon.MaHD,
                MaHH = item.MaHH,
                TenHH = item.TenHH,
                DonGia = item.DonGia,
                SoLuong = item.SoLuong,
                GiamGia = item.GiamGia
            });
        }
        await _context.SaveChangesAsync();

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

        return CreatedAtAction(nameof(GetById), new { maHD = hoaDon.MaHD }, new
        {
            MaHD = hoaDon.MaHD,
            message = "Đặt hàng thành công"
        });
    }

    /// <summary>Lấy danh sách đơn hàng của khách hàng</summary>
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int? trangThai)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var query = _context.HoaDons
            .Include(h => h.ChiTietHDs)
            .Where(h => h.MaKH == maKH);

        if (trangThai.HasValue)
            query = query.Where(h => h.TrangThai == trangThai);

        var orders = await query
            .OrderByDescending(h => h.NgayDat)
            .Select(h => new OrderResponseDto
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
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>Lấy chi tiết đơn hàng</summary>
    [HttpGet("{maHD}")]
    public async Task<IActionResult> GetById(int maHD)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var vaiTro = User.FindFirst("VaiTro")?.Value;
        var isAdmin = vaiTro == "1";

        var query = _context.HoaDons
            .Include(h => h.ChiTietHDs)
            .Where(h => h.MaHD == maHD);

        if (!isAdmin && !string.IsNullOrEmpty(maKH))
            query = query.Where(h => h.MaKH == maKH);

        var hd = await query.FirstOrDefaultAsync();
        if (hd == null) return NotFound();

        return Ok(new OrderResponseDto
        {
            MaHD = hd.MaHD,
            MaKH = hd.MaKH,
            NgayDat = hd.NgayDat,
            NgayCan = hd.NgayCan,
            NgayGiao = hd.NgayGiao,
            HoTen = hd.HoTen,
            DiaChi = hd.DiaChi,
            CachThanhToan = hd.CachThanhToan,
            CachVanChuyen = hd.CachVanChuyen,
            PhiVanChuyen = hd.PhiVanChuyen,
            MaNV = hd.MaNV,
            GhiChu = hd.GhiChu,
            TrangThai = hd.TrangThai,
            ChiTiet = hd.ChiTietHDs.Select(ct => new ChiTietHDDto
            {
                MaCT = ct.MaCT,
                MaHH = ct.MaHH,
                TenHH = ct.TenHH,
                SoLuong = ct.SoLuong,
                DonGia = ct.DonGia,
                GiamGia = ct.GiamGia
            }).ToList()
        });
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
        var query = _context.HoaDons.Include(h => h.ChiTietHDs).AsQueryable();

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
            .Select(h => new OrderResponseDto
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
            })
            .ToListAsync();

        return Ok(new { total, page, size, items });
    }

    /// <summary>Cập nhật trạng thái đơn hàng (Admin/NV)</summary>
    [HttpPut("{maHD}/status")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateStatus(int maHD, [FromBody] UpdateOrderStatusDto dto)
    {
        var hd = await _context.HoaDons.FindAsync(maHD);
        if (hd == null) return NotFound();

        if (hd.TrangThai == 4)
            return BadRequest(new { message = "Không thể cập nhật đơn hàng đã hủy" });

        hd.TrangThai = dto.TrangThai;
        if (!string.IsNullOrEmpty(dto.MaNV)) hd.MaNV = dto.MaNV;
        if (dto.NgayGiao.HasValue) hd.NgayGiao = dto.NgayGiao;

        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("order.updated", new
        {
            MaHD = hd.MaHD,
            MaKH = hd.MaKH,
            TrangThai = hd.TrangThai,
            MaNV = hd.MaNV,
            NgayGiao = hd.NgayGiao,
            UpdatedAt = DateTime.UtcNow
        });

        return Ok(new { message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>Hủy đơn hàng (khách hàng chỉ hủy khi chờ xác nhận)</summary>
    [HttpPut("{maHD}/cancel")]
    public async Task<IActionResult> CancelOrder(int maHD, [FromBody] string? lyDo)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var isAdmin = User.FindFirst("VaiTro")?.Value == "1";

        var query = _context.HoaDons.Where(h => h.MaHD == maHD);
        if (!isAdmin && !string.IsNullOrEmpty(maKH))
            query = query.Where(h => h.MaKH == maKH);

        var hd = await query.FirstOrDefaultAsync();
        if (hd == null) return NotFound();

        if (!isAdmin && hd.TrangThai != 0)
            return BadRequest(new { message = "Chỉ có thể hủy đơn hàng đang chờ xác nhận" });

        hd.TrangThai = 4;
        hd.GhiChu = !string.IsNullOrEmpty(lyDo) ? $"Hủy đơn: {lyDo}" : "Đơn hàng đã bị hủy";
        await _context.SaveChangesAsync();

        await _kafkaProducer.ProduceAsync("order.cancelled", new
        {
            MaHD = hd.MaHD,
            MaKH = hd.MaKH,
            LyDo = lyDo,
            CancelledAt = DateTime.UtcNow
        });

        return Ok(new { message = "Hủy đơn hàng thành công" });
    }

    /// <summary>Thống kê đơn hàng (Admin)</summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        var query = _context.HoaDons.Include(h => h.ChiTietHDs).AsQueryable();

        if (tuNgay.HasValue) query = query.Where(h => h.NgayDat >= tuNgay);
        if (denNgay.HasValue) query = query.Where(h => h.NgayDat <= denNgay);

        var orders = await query.ToListAsync();

        return Ok(new
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
        });
    }

    private static decimal CalculateShippingFee(string cachVanChuyen) => cachVanChuyen switch
    {
        "Giao hàng nhanh" => 30000,
        "Giao hàng hỏa tốc" => 60000,
        "Giao hàng tiết kiệm" => 15000,
        _ => 20000
    };
}
