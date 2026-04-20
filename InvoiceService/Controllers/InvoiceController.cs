using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InvoiceService.Services;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPdfExportService _pdfService;
    private readonly IExcelReportService _excelService;

    public InvoiceController(
        IInvoiceService invoiceService,
        IPdfExportService pdfService,
        IExcelReportService excelService)
    {
        _invoiceService = invoiceService;
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

        var invoices = await _invoiceService.GetMyInvoicesAsync(maKH, trangThai);
        return Ok(invoices);
    }

    /// <summary>Lấy chi tiết hóa đơn</summary>
    [HttpGet("{maHD}")]
    public async Task<IActionResult> GetById(int maHD)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var isAdmin = User.FindFirst("VaiTro")?.Value == "1";

        var hd = await _invoiceService.GetByIdAsync(maHD, maKH, isAdmin);
        if (hd == null) return NotFound();
        return Ok(hd);
    }

    /// <summary>Xuất hóa đơn PDF</summary>
    [HttpGet("{maHD}/export/pdf")]
    public async Task<IActionResult> ExportPdf(int maHD)
    {
        var maKH = User.FindFirst("MaKH")?.Value;
        var isAdmin = User.FindFirst("VaiTro")?.Value == "1";

        var hd = await _invoiceService.GetInvoiceEntityAsync(maHD, maKH, isAdmin);
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
        var data = await _invoiceService.GetAllAsync(maKH, trangThai, tuNgay, denNgay, page, size);
        return Ok(data);
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

        var orders = await _invoiceService.GetOrdersInRangeAsync(from, to);

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

        var report = await _invoiceService.GetRevenueReportAsync(from, to);
        return Ok(report);
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

        var report = await _invoiceService.GetTopProductsReportAsync(from, to, top);

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

        var orders = await _invoiceService.GetOrdersInRangeAsync(from, to);

        var excel = _excelService.GenerateRevenueReport(orders, from, to);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoDoanhThuNgay_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
    }
}
