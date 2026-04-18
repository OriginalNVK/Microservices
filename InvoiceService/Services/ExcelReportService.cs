using ClosedXML.Excel;
using InvoiceService.Models;

namespace InvoiceService.Services;

public interface IExcelReportService
{
    byte[] GenerateSalesReport(List<HoaDon> orders, DateTime tuNgay, DateTime denNgay);
    byte[] GenerateRevenueReport(List<HoaDon> orders, DateTime tuNgay, DateTime denNgay);
}

public class ExcelReportService : IExcelReportService
{
    public byte[] GenerateSalesReport(List<HoaDon> orders, DateTime tuNgay, DateTime denNgay)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Báo cáo đơn hàng");

        ws.Range("A1:H1").Merge();
        ws.Cell("A1").Value = "BÁO CÁO ĐƠN HÀNG - HKSHOP";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell("A2").Value = $"Từ ngày: {tuNgay:dd/MM/yyyy} - Đến ngày: {denNgay:dd/MM/yyyy}";
        ws.Range("A2:H2").Merge();
        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var headers = new[] { "Mã HD", "Mã KH", "Ngày đặt", "Địa chỉ", "Thanh toán", "Vận chuyển", "Tổng tiền", "Trạng thái" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
            ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            ws.Cell(4, i + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(4, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var row = 5;
        foreach (var order in orders)
        {
            var tongTien = order.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong) + order.PhiVanChuyen;
            ws.Cell(row, 1).Value = order.MaHD;
            ws.Cell(row, 2).Value = order.MaKH;
            ws.Cell(row, 3).Value = order.NgayDat.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 4).Value = order.DiaChi;
            ws.Cell(row, 5).Value = order.CachThanhToan;
            ws.Cell(row, 6).Value = order.CachVanChuyen;
            ws.Cell(row, 7).Value = tongTien;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 8).Value = GetTrangThaiText(order.TrangThai);

            if (row % 2 == 0)
                ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
        }

        var tongDoanhThu = orders.Where(o => o.TrangThai == 3)
            .Sum(o => o.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong) + o.PhiVanChuyen);

        ws.Cell(row + 1, 6).Value = "Tổng doanh thu:";
        ws.Cell(row + 1, 6).Style.Font.Bold = true;
        ws.Cell(row + 1, 7).Value = tongDoanhThu;
        ws.Cell(row + 1, 7).Style.Font.Bold = true;
        ws.Cell(row + 1, 7).Style.NumberFormat.Format = "#,##0";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenerateRevenueReport(List<HoaDon> orders, DateTime tuNgay, DateTime denNgay)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Báo cáo doanh thu");

        ws.Range("A1:E1").Merge();
        ws.Cell("A1").Value = "BÁO CÁO DOANH THU THEO NGÀY - HKSHOP";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell("A2").Value = $"Từ ngày: {tuNgay:dd/MM/yyyy} - Đến ngày: {denNgay:dd/MM/yyyy}";
        ws.Range("A2:E2").Merge();
        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var headers = new[] { "Ngày", "Số đơn", "Tổng tiền hàng", "Phí vận chuyển", "Tổng doanh thu" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
            ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            ws.Cell(4, i + 1).Style.Font.FontColor = XLColor.White;
        }

        var groupedByDate = orders
            .Where(o => o.TrangThai == 3)
            .GroupBy(o => o.NgayDat.Date)
            .OrderBy(g => g.Key);

        var row = 5;
        foreach (var group in groupedByDate)
        {
            var sodon = group.Count();
            var tongHang = group.Sum(o => o.ChiTietHDs.Sum(ct => ct.DonGia * (1 - ct.GiamGia / 100) * ct.SoLuong));
            var phiVC = group.Sum(o => o.PhiVanChuyen);

            ws.Cell(row, 1).Value = group.Key.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = sodon;
            ws.Cell(row, 3).Value = tongHang;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 4).Value = phiVC;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Value = tongHang + phiVC;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

            if (row % 2 == 0)
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
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
