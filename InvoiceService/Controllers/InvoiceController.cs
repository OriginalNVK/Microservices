using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceService.Data;
using InvoiceService.Services;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly InvoiceDbContext _context;
    private readonly IPdfExportService _pdfService;
    private readonly IExcelReportService _excelService;

    public InvoiceController(
        InvoiceDbContext context,
        IPdfExportService pdfService,
        IExcelReportService excelService)
    {
        _context = context;
        _pdfService = pdfService;
        _excelService = excelService;
    }

    /// <summary>Lấy danh sách hóa đơn của khách hàng</summary>
    [HttpGet("my-invoices")]
    public async Task<IActionResult> GetMyInvoices([FromQuery] int? trangThai)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        if (string.IsNullOrEmpty(maKH))
            return Forbid();

        var query = _context.HoaDons
            .Include(h => h.ChiTietHDs)
            .Where(h => h.MaKH == maKH);

        if (trangThai.HasValue)
            query = query.Where(h => h.TrangThai == trangThai);

        var invoices = await query.OrderByDescending(h => h.NgayDat).ToListAsync();

        return Ok(invoices.Select(h => new
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
        }));
    }

    /// <summary>Lấy chi tiết hóa đơn</summary>
    [HttpGet("{maHD}")]
    public async Task<IActionResult> GetById(int maHD)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var isAdmin = User.FindFirst("VaiTro")?.Value == "1";

        var query = _context.HoaDons.Include(h => h.ChiTietHDs).Where(h => h.MaHD == maHD);

        if (!isAdmin && !string.IsNullOrEmpty(maKH))
            query = query.Where(h => h.MaKH == maKH);

        var hd = await query.FirstOrDefaultAsync();
        if (hd == null) return NotFound();

        var tongTienHang = hd.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong);

        return Ok(new
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
            PhiVanChuyen = hd.PhiVanChuyen,
            TongCong = tongTienHang + hd.PhiVanChuyen
        });
    }

    /// <summary>Xuất hóa đơn PDF</summary>
    [HttpGet("{maHD}/export/pdf")]
    public async Task<IActionResult> ExportPdf(int maHD)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var isAdmin = User.FindFirst("VaiTro")?.Value == "1";

        var query = _context.HoaDons.Include(h => h.ChiTietHDs).Where(h => h.MaHD == maHD);

        if (!isAdmin && !string.IsNullOrEmpty(maKH))
            query = query.Where(h => h.MaKH == maKH);

        var hd = await query.FirstOrDefaultAsync();
        if (hd == null) return NotFound();

        var pdf = _pdfService.GenerateInvoicePdf(hd);
        return File(pdf, "application/pdf", $"HoaDon_{maHD}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    /// <summary>Lấy tất cả hóa đơn (Admin)</summary>
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
            .ToListAsync();

        return Ok(new
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
        });
    }

    /// <summary>Xuất báo cáo Excel danh sách đơn hàng (Admin)</summary>
    [HttpGet("report/excel")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> ExportSalesReportExcel(
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var from = tuNgay ?? DateTime.Now.AddMonths(-1);
        var to = denNgay ?? DateTime.Now;

        var orders = await _context.HoaDons
            .Include(h => h.ChiTietHDs)
            .Where(h => h.NgayDat >= from && h.NgayDat <= to)
            .OrderByDescending(h => h.NgayDat)
            .ToListAsync();

        var excel = _excelService.GenerateSalesReport(orders, from, to);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoDoanhThu_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
    }

    /// <summary>Báo cáo doanh thu theo ngày (Admin)</summary>
    [HttpGet("report/revenue")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var from = tuNgay ?? DateTime.Now.AddMonths(-1);
        var to = denNgay ?? DateTime.Now;

        var orders = await _context.HoaDons
            .Include(h => h.ChiTietHDs)
            .Where(h => h.NgayDat >= from && h.NgayDat <= to && h.TrangThai == 3)
            .ToListAsync();

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

        return Ok(new
        {
            TuNgay = from.ToString("dd/MM/yyyy"),
            DenNgay = to.ToString("dd/MM/yyyy"),
            TongDoanhThu = report.Sum(r => r.DoanhThu),
            TongDon = report.Sum(r => r.SoDon),
            ChiTiet = report
        });
    }

    /// <summary>Báo cáo sản phẩm bán chạy (Admin)</summary>
    [HttpGet("report/top-products")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> GetTopProductsReport(
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        [FromQuery] int top = 10)
    {
        var from = tuNgay ?? DateTime.Now.AddMonths(-1);
        var to = denNgay ?? DateTime.Now;

        var chiTiet = await _context.ChiTietHDs
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

        return Ok(report);
    }

    /// <summary>Xuất báo cáo doanh thu Excel (Admin)</summary>
    [HttpGet("report/revenue/excel")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> ExportRevenueReportExcel(
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var from = tuNgay ?? DateTime.Now.AddMonths(-1);
        var to = denNgay ?? DateTime.Now;

        var orders = await _context.HoaDons
            .Include(h => h.ChiTietHDs)
            .Where(h => h.NgayDat >= from && h.NgayDat <= to)
            .ToListAsync();

        var excel = _excelService.GenerateRevenueReport(orders, from, to);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoDoanhThuNgay_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
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
